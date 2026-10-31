using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Dtos;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;
using Siserp.Api.Services;

namespace Siserp.Api.Controllers;

/// <summary>
/// Cotizaciones: propuesta comercial previa a la venta. No afecta stock ni
/// contabilidad. Al ser aprobada por el cliente puede convertirse en una
/// venta (<see cref="Comprobante"/>) sin volver a digitar los ítems.
/// </summary>
[Authorize]
[MenuPermiso("cotizaciones")]
[ApiController]
[Route("cotizaciones")]
public class CotizacionesController : ControllerBase
{
    private const decimal IgvTasa = 0.18m;
    private static readonly Dictionary<TipoComprobante, string> SeriePorTipo = new()
    {
        [TipoComprobante.FACTURA] = "F001",
        [TipoComprobante.BOLETA] = "B001",
    };

    private readonly SiserpDbContext _db;
    private readonly ProductosService _productosService;
    private readonly PdfService _pdfService;
    private readonly EmailService _emailService;

    public CotizacionesController(SiserpDbContext db, ProductosService productosService, PdfService pdfService, EmailService emailService)
    {
        _db = db;
        _productosService = productosService;
        _pdfService = pdfService;
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCotizacionDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == dto.ClienteId && c.EmpresaId == empresaId);
        if (cliente is null) return BadRequest(new { message = "Cliente no encontrado" });
        if (dto.Detalles.Count == 0) return BadRequest(new { message = "La cotización debe tener al menos un ítem" });

        var productoIds = dto.Detalles.Select(d => d.ProductoId).ToList();
        var productos = await _db.Productos.Where(p => productoIds.Contains(p.Id) && p.EmpresaId == empresaId).ToListAsync();
        if (productos.Count != dto.Detalles.Select(d => d.ProductoId).Distinct().Count())
            return BadRequest(new { message = "Uno o más productos no existen para esta empresa" });

        var correlativo = await _db.Cotizaciones.CountAsync(c => c.EmpresaId == empresaId) + 1;
        var total = dto.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);

        var cotizacion = new Cotizacion
        {
            EmpresaId = empresaId,
            ClienteId = dto.ClienteId,
            Numero = $"COT-{correlativo:D6}",
            FechaValidez = dto.FechaValidez,
            Moneda = dto.Moneda ?? "PEN",
            Total = Math.Round(total, 2),
            Observaciones = dto.Observaciones,
            Estado = EstadoCotizacion.BORRADOR,
            Detalles = dto.Detalles.Select(d => new CotizacionDetalle
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Total = Math.Round(d.Cantidad * d.PrecioUnitario, 2),
            }).ToList(),
        };
        _db.Cotizaciones.Add(cotizacion);
        await _db.SaveChangesAsync();

        return Ok(await Obtener(cotizacion.Id, empresaId));
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        var cotizaciones = await _db.Cotizaciones
            .Where(c => c.EmpresaId == empresaId)
            .Include(c => c.Cliente)
            .Include(c => c.PedidoVenta)
            .Include(c => c.Detalles)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(cotizaciones);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var cotizacion = await Obtener(id, empresaId);
        if (cotizacion is null) return NotFound(new { message = "Cotización no encontrada" });
        return Ok(cotizacion);
    }

    [HttpPost("{id:guid}/enviar")]
    public async Task<IActionResult> Enviar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var cotizacion = await _db.Cotizaciones.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (cotizacion is null) return NotFound(new { message = "Cotización no encontrada" });
        if (cotizacion.Estado != EstadoCotizacion.BORRADOR) return BadRequest(new { message = "Solo se puede enviar una cotización en estado BORRADOR" });
        cotizacion.Estado = EstadoCotizacion.ENVIADA;
        cotizacion.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(cotizacion);
    }

    [HttpPost("{id:guid}/aprobar")]
    public async Task<IActionResult> Aprobar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var cotizacion = await _db.Cotizaciones.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (cotizacion is null) return NotFound(new { message = "Cotización no encontrada" });
        if (cotizacion.Estado != EstadoCotizacion.ENVIADA) return BadRequest(new { message = "Solo se puede aprobar una cotización ENVIADA" });
        cotizacion.Estado = EstadoCotizacion.APROBADA;
        cotizacion.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(cotizacion);
    }

    [HttpPost("{id:guid}/rechazar")]
    public async Task<IActionResult> Rechazar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var cotizacion = await _db.Cotizaciones.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (cotizacion is null) return NotFound(new { message = "Cotización no encontrada" });
        if (cotizacion.Estado is EstadoCotizacion.CONVERTIDA or EstadoCotizacion.ANULADA)
            return BadRequest(new { message = "La cotización ya no admite cambios de estado" });
        cotizacion.Estado = EstadoCotizacion.RECHAZADA;
        cotizacion.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(cotizacion);
    }

    [HttpPost("{id:guid}/anular")]
    public async Task<IActionResult> Anular(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var cotizacion = await _db.Cotizaciones.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (cotizacion is null) return NotFound(new { message = "Cotización no encontrada" });
        if (cotizacion.Estado == EstadoCotizacion.CONVERTIDA) return BadRequest(new { message = "No se puede anular una cotización ya convertida en venta" });
        cotizacion.Estado = EstadoCotizacion.ANULADA;
        cotizacion.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(cotizacion);
    }

    /// <summary>
    /// Convierte una cotización aprobada en un pedido de venta (no descuenta stock ni
    /// factura todavía). El pedido queda CONFIRMADO y luego se factura desde
    /// <see cref="PedidosVentaController.ConvertirAVenta"/>.
    /// </summary>
    [HttpPost("{id:guid}/convertir-a-pedido")]
    public async Task<IActionResult> ConvertirAPedido(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var cotizacion = await _db.Cotizaciones
            .Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (cotizacion is null) return NotFound(new { message = "Cotización no encontrada" });
        if (cotizacion.Estado != EstadoCotizacion.APROBADA) return BadRequest(new { message = "Solo se puede convertir una cotización APROBADA" });

        var correlativo = await _db.PedidosVenta.CountAsync(p => p.EmpresaId == empresaId) + 1;
        var pedido = new PedidoVenta
        {
            EmpresaId = empresaId,
            ClienteId = cotizacion.ClienteId,
            Numero = $"PED-{correlativo:D6}",
            Moneda = cotizacion.Moneda,
            Total = cotizacion.Total,
            Estado = EstadoPedidoVenta.CONFIRMADO,
            Observaciones = cotizacion.Observaciones,
            CotizacionId = cotizacion.Id,
            Detalles = cotizacion.Detalles.Select(d => new PedidoVentaDetalle
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Total = d.Total,
            }).ToList(),
        };
        _db.PedidosVenta.Add(pedido);

        cotizacion.Estado = EstadoCotizacion.CONVERTIDA;
        cotizacion.PedidoVentaId = pedido.Id;
        cotizacion.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(pedido);
    }

    /// <summary>Convierte una cotización aprobada directamente en una venta (comprobante), descontando stock del almacén indicado.</summary>
    [HttpPost("{id:guid}/convertir-a-venta")]
    public async Task<IActionResult> ConvertirAVenta(Guid id, ConvertirCotizacionDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var cotizacion = await _db.Cotizaciones
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (cotizacion is null) return NotFound(new { message = "Cotización no encontrada" });
        if (cotizacion.Estado != EstadoCotizacion.APROBADA) return BadRequest(new { message = "Solo se puede convertir una cotización APROBADA" });

        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == cotizacion.ClienteId && c.EmpresaId == empresaId);
        if (cliente is null) return BadRequest(new { message = "Cliente no encontrado" });
        if (cliente.Bloqueado) return BadRequest(new { message = "El cliente está bloqueado y no admite nuevas ventas" });
        if (!cliente.Activo) return BadRequest(new { message = "El cliente está inactivo" });

        var tipoComprobante = Enum.Parse<TipoComprobante>(dto.TipoComprobante);
        if (!SeriePorTipo.TryGetValue(tipoComprobante, out var serieNombre))
            return BadRequest(new { message = "Tipo de comprobante inválido para conversión de cotización" });

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var serie = await _db.SeriesComprobante.FirstOrDefaultAsync(s =>
            s.EmpresaId == empresaId && s.TipoComprobante == tipoComprobante && s.Serie == serieNombre);
        if (serie is null)
        {
            serie = new SerieComprobante { EmpresaId = empresaId, TipoComprobante = tipoComprobante, Serie = serieNombre, CorrelativoActual = 0 };
            _db.SeriesComprobante.Add(serie);
            await _db.SaveChangesAsync();
        }
        serie.CorrelativoActual += 1;
        await _db.SaveChangesAsync();

        decimal opGravada = 0;
        decimal igv = 0;
        var detalles = new List<ComprobanteDetalle>();
        foreach (var d in cotizacion.Detalles)
        {
            var totalLinea = d.Cantidad * d.PrecioUnitario;
            var afecto = d.Producto.AfectoIGV;
            var valorVentaLinea = afecto ? totalLinea / (1 + IgvTasa) : totalLinea;
            var igvLinea = afecto ? totalLinea - valorVentaLinea : 0;
            opGravada += valorVentaLinea;
            igv += igvLinea;
            detalles.Add(new ComprobanteDetalle
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                ValorVenta = Math.Round(valorVentaLinea, 2),
                Igv = Math.Round(igvLinea, 2),
                Total = Math.Round(totalLinea, 2),
            });
        }

        var comprobante = new Comprobante
        {
            EmpresaId = empresaId,
            ClienteId = cotizacion.ClienteId,
            SerieId = serie.Id,
            TipoComprobante = tipoComprobante,
            Serie = serieNombre,
            Correlativo = serie.CorrelativoActual,
            Moneda = cotizacion.Moneda,
            OpGravada = Math.Round(opGravada, 2),
            Igv = Math.Round(igv, 2),
            Total = Math.Round(opGravada + igv, 2),
            Estado = EstadoComprobante.PENDIENTE_ENVIO,
            FechaVencimiento = cliente.DiasCredito > 0 ? DateTime.UtcNow.AddDays(cliente.DiasCredito) : null,
            Detalles = detalles,
        };
        _db.Comprobantes.Add(comprobante);
        await _db.SaveChangesAsync();

        foreach (var detalle in detalles)
        {
            await _productosService.AjustarStockAsync(
                detalle.ProductoId, dto.AlmacenId, detalle.Cantidad, TipoMovimiento.SALIDA,
                $"{serieNombre}-{serie.CorrelativoActual} (cotización {cotizacion.Numero})");
        }

        _db.SunatSyncQueue.Add(new SunatSyncQueue { ComprobanteId = comprobante.Id });

        cotizacion.Estado = EstadoCotizacion.CONVERTIDA;
        cotizacion.ComprobanteId = comprobante.Id;
        cotizacion.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(await Obtener(cotizacion.Id, empresaId));
    }

    private async Task<Cotizacion?> Obtener(Guid id, Guid empresaId) =>
        await _db.Cotizaciones
            .Where(c => c.Id == id && c.EmpresaId == empresaId)
            .Include(c => c.Cliente)
            .Include(c => c.Comprobante)
            .Include(c => c.PedidoVenta)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync();

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var cotizacion = await Obtener(id, empresaId);
        if (cotizacion is null) return NotFound(new { message = "Cotización no encontrada" });
        var empresa = await _db.Empresas.FirstAsync(e => e.Id == empresaId);
        var pdf = _pdfService.GenerarCotizacion(cotizacion, empresa);
        return File(pdf, "application/pdf", $"{cotizacion.Numero}.pdf");
    }

    [HttpPost("{id:guid}/enviar-correo")]
    public async Task<IActionResult> EnviarCorreo(Guid id, EnviarCorreoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var cotizacion = await Obtener(id, empresaId);
        if (cotizacion is null) return NotFound(new { message = "Cotización no encontrada" });

        var destino = dto.Para ?? cotizacion.Cliente.Email;
        if (string.IsNullOrWhiteSpace(destino)) return BadRequest(new { message = "No se indicó un correo destino y el cliente no tiene uno registrado" });

        var empresa = await _db.Empresas.FirstAsync(e => e.Id == empresaId);
        var pdf = _pdfService.GenerarCotizacion(cotizacion, empresa);
        try
        {
            await _emailService.SendAsync(
                destino, dto.Asunto ?? $"Cotización {cotizacion.Numero}",
                dto.Mensaje ?? $"Adjuntamos la cotización {cotizacion.Numero}.", pdf, $"{cotizacion.Numero}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        return Ok(new { message = "Correo enviado" });
    }
}
