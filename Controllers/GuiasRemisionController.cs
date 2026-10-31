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
/// Guías de remisión: sustentan el traslado físico de mercadería. Al
/// EMITIRSE, si no están vinculadas a una venta ya facturada (el stock de la
/// venta ya se descontó al emitir el comprobante), descuentan el stock del
/// almacén de origen y generan el movimiento de Kardex (costeado por el
/// método promedio ponderado, igual que el resto de salidas de almacén).
/// </summary>
[Authorize]
[MenuPermiso("guias-remision")]
[ApiController]
[Route("guias-remision")]
public class GuiasRemisionController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly ProductosService _productosService;
    private readonly PdfService _pdfService;
    private readonly EmailService _emailService;

    public GuiasRemisionController(SiserpDbContext db, ProductosService productosService, PdfService pdfService, EmailService emailService)
    {
        _db = db;
        _productosService = productosService;
        _pdfService = pdfService;
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateGuiaRemisionDto dto)
    {
        var empresaId = User.GetEmpresaId();
        if (dto.Detalles.Count == 0) return BadRequest(new { message = "La guía debe tener al menos un ítem" });

        var almacenOrigen = await _db.Almacenes.FirstOrDefaultAsync(a => a.Id == dto.AlmacenOrigenId && a.EmpresaId == empresaId);
        if (almacenOrigen is null) return BadRequest(new { message = "Almacén de origen no encontrado" });

        if (dto.AlmacenDestinoId is not null)
        {
            var almacenDestino = await _db.Almacenes.FirstOrDefaultAsync(a => a.Id == dto.AlmacenDestinoId && a.EmpresaId == empresaId);
            if (almacenDestino is null) return BadRequest(new { message = "Almacén de destino no encontrado" });
        }

        if (dto.ClienteId is not null)
        {
            var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == dto.ClienteId && c.EmpresaId == empresaId);
            if (cliente is null) return BadRequest(new { message = "Cliente no encontrado" });
        }

        if (dto.ComprobanteId is not null)
        {
            var comprobante = await _db.Comprobantes.FirstOrDefaultAsync(c => c.Id == dto.ComprobanteId && c.EmpresaId == empresaId);
            if (comprobante is null) return BadRequest(new { message = "El comprobante indicado no existe" });
        }

        var correlativo = await _db.GuiasRemision.CountAsync(g => g.EmpresaId == empresaId) + 1;

        var guia = new GuiaRemision
        {
            EmpresaId = empresaId,
            Numero = $"GR-{correlativo:D6}",
            AlmacenOrigenId = dto.AlmacenOrigenId,
            AlmacenDestinoId = dto.AlmacenDestinoId,
            ClienteId = dto.ClienteId,
            ComprobanteId = dto.ComprobanteId,
            DireccionPartida = dto.DireccionPartida,
            DireccionLlegada = dto.DireccionLlegada,
            Transportista = dto.Transportista,
            PlacaVehiculo = dto.PlacaVehiculo,
            MotivoTraslado = dto.MotivoTraslado ?? "VENTA",
            Observaciones = dto.Observaciones,
            Estado = EstadoGuiaRemision.BORRADOR,
            Detalles = dto.Detalles.Select(d => new GuiaRemisionDetalle
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
            }).ToList(),
        };
        _db.GuiasRemision.Add(guia);
        await _db.SaveChangesAsync();

        return Ok(await Obtener(guia.Id, empresaId));
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        var guias = await _db.GuiasRemision
            .Where(g => g.EmpresaId == empresaId)
            .Include(g => g.AlmacenOrigen)
            .Include(g => g.AlmacenDestino)
            .Include(g => g.Cliente)
            .Include(g => g.Comprobante)
            .Include(g => g.Detalles)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
        return Ok(guias);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var guia = await Obtener(id, empresaId);
        if (guia is null) return NotFound(new { message = "Guía de remisión no encontrada" });
        return Ok(guia);
    }

    /// <summary>
    /// Emite la guía: si no está vinculada a un comprobante (venta ya
    /// facturada, que ya descontó stock), descuenta el stock del almacén de
    /// origen por cada ítem y registra el movimiento en el Kardex.
    /// </summary>
    [HttpPost("{id:guid}/emitir")]
    public async Task<IActionResult> Emitir(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var guia = await _db.GuiasRemision
            .Include(g => g.Detalles)
            .FirstOrDefaultAsync(g => g.Id == id && g.EmpresaId == empresaId);
        if (guia is null) return NotFound(new { message = "Guía de remisión no encontrada" });
        if (guia.Estado != EstadoGuiaRemision.BORRADOR) return BadRequest(new { message = "Solo se puede emitir una guía en estado BORRADOR" });

        await using var transaction = await _db.Database.BeginTransactionAsync();

        if (guia.ComprobanteId is null)
        {
            foreach (var detalle in guia.Detalles)
            {
                var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.ProductoId == detalle.ProductoId && s.AlmacenId == guia.AlmacenOrigenId);
                if (stock is null || stock.Cantidad < detalle.Cantidad)
                    return BadRequest(new { message = $"Stock insuficiente para el producto {detalle.ProductoId}" });
            }

            foreach (var detalle in guia.Detalles)
            {
                await _productosService.AjustarStockAsync(
                    detalle.ProductoId, guia.AlmacenOrigenId, detalle.Cantidad, TipoMovimiento.SALIDA,
                    $"Guía de remisión {guia.Numero}");
            }
        }

        guia.Estado = EstadoGuiaRemision.EMITIDA;
        guia.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(await Obtener(guia.Id, empresaId));
    }

    [HttpPost("{id:guid}/anular")]
    public async Task<IActionResult> Anular(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var guia = await _db.GuiasRemision
            .Include(g => g.Detalles)
            .FirstOrDefaultAsync(g => g.Id == id && g.EmpresaId == empresaId);
        if (guia is null) return NotFound(new { message = "Guía de remisión no encontrada" });
        if (guia.Estado == EstadoGuiaRemision.ANULADA) return BadRequest(new { message = "La guía ya está anulada" });

        // Si ya se había emitido y descontado stock propio (sin comprobante), se revierte el stock.
        if (guia.Estado == EstadoGuiaRemision.EMITIDA && guia.ComprobanteId is null)
        {
            foreach (var detalle in guia.Detalles)
            {
                await _productosService.AjustarStockAsync(
                    detalle.ProductoId, guia.AlmacenOrigenId, detalle.Cantidad, TipoMovimiento.ENTRADA,
                    $"Anulación guía de remisión {guia.Numero}");
            }
        }

        guia.Estado = EstadoGuiaRemision.ANULADA;
        guia.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(guia);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var guia = await Obtener(id, empresaId);
        if (guia is null) return NotFound(new { message = "Guía de remisión no encontrada" });
        var empresa = await _db.Empresas.FirstAsync(e => e.Id == empresaId);
        var pdf = _pdfService.GenerarGuiaRemision(guia, empresa);
        return File(pdf, "application/pdf", $"{guia.Numero}.pdf");
    }

    [HttpPost("{id:guid}/enviar-correo")]
    public async Task<IActionResult> EnviarCorreo(Guid id, EnviarCorreoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var guia = await Obtener(id, empresaId);
        if (guia is null) return NotFound(new { message = "Guía de remisión no encontrada" });

        var destino = dto.Para ?? guia.Cliente?.Email;
        if (string.IsNullOrWhiteSpace(destino)) return BadRequest(new { message = "No se indicó un correo destino y el cliente no tiene uno registrado" });

        var empresa = await _db.Empresas.FirstAsync(e => e.Id == empresaId);
        var pdf = _pdfService.GenerarGuiaRemision(guia, empresa);
        try
        {
            await _emailService.SendAsync(
                destino, dto.Asunto ?? $"Guía de remisión {guia.Numero}",
                dto.Mensaje ?? $"Adjuntamos la guía de remisión {guia.Numero}.", pdf, $"{guia.Numero}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        return Ok(new { message = "Correo enviado" });
    }

    private async Task<GuiaRemision?> Obtener(Guid id, Guid empresaId) =>
        await _db.GuiasRemision
            .Where(g => g.Id == id && g.EmpresaId == empresaId)
            .Include(g => g.AlmacenOrigen)
            .Include(g => g.AlmacenDestino)
            .Include(g => g.Cliente)
            .Include(g => g.Comprobante)
            .Include(g => g.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync();
}
