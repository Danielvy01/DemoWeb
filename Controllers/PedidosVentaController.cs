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
/// Pedidos de venta: compromiso comercial con el cliente, ya sea generado a
/// partir de una <see cref="Cotizacion"/> aprobada o creado directamente.
/// No descuenta stock ni afecta contabilidad hasta que se factura
/// (<see cref="ConvertirAVenta"/>), momento en el que se genera el
/// <see cref="Comprobante"/> (factura/boleta) y se encola su envío a SUNAT.
/// </summary>
[Authorize]
[MenuPermiso("pedidos-venta")]
[ApiController]
[Route("pedidos-venta")]
public class PedidosVentaController : ControllerBase
{
    private const decimal IgvTasa = 0.18m;
    private static readonly Dictionary<TipoComprobante, string> SeriePorTipo = new()
    {
        [TipoComprobante.FACTURA] = "F001",
        [TipoComprobante.BOLETA] = "B001",
    };

    private readonly SiserpDbContext _db;
    private readonly ProductosService _productosService;

    public PedidosVentaController(SiserpDbContext db, ProductosService productosService)
    {
        _db = db;
        _productosService = productosService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePedidoVentaDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == dto.ClienteId && c.EmpresaId == empresaId);
        if (cliente is null) return BadRequest(new { message = "Cliente no encontrado" });
        if (dto.Detalles.Count == 0) return BadRequest(new { message = "El pedido debe tener al menos un ítem" });

        var productoIds = dto.Detalles.Select(d => d.ProductoId).ToList();
        var productos = await _db.Productos.Where(p => productoIds.Contains(p.Id) && p.EmpresaId == empresaId).ToListAsync();
        if (productos.Count != dto.Detalles.Select(d => d.ProductoId).Distinct().Count())
            return BadRequest(new { message = "Uno o más productos no existen para esta empresa" });

        var correlativo = await _db.PedidosVenta.CountAsync(p => p.EmpresaId == empresaId) + 1;
        var total = dto.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);

        var pedido = new PedidoVenta
        {
            EmpresaId = empresaId,
            ClienteId = dto.ClienteId,
            Numero = $"PED-{correlativo:D6}",
            FechaEntrega = dto.FechaEntrega,
            Moneda = dto.Moneda ?? "PEN",
            Total = Math.Round(total, 2),
            Observaciones = dto.Observaciones,
            Estado = EstadoPedidoVenta.BORRADOR,
            Detalles = dto.Detalles.Select(d => new PedidoVentaDetalle
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Total = Math.Round(d.Cantidad * d.PrecioUnitario, 2),
            }).ToList(),
        };
        _db.PedidosVenta.Add(pedido);
        await _db.SaveChangesAsync();

        return Ok(await Obtener(pedido.Id, empresaId));
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        var pedidos = await _db.PedidosVenta
            .Where(p => p.EmpresaId == empresaId)
            .Include(p => p.Cliente)
            .Include(p => p.Cotizacion)
            .Include(p => p.Detalles)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Ok(pedidos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var pedido = await Obtener(id, empresaId);
        if (pedido is null) return NotFound(new { message = "Pedido de venta no encontrado" });
        return Ok(pedido);
    }

    [HttpPost("{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var pedido = await _db.PedidosVenta.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (pedido is null) return NotFound(new { message = "Pedido de venta no encontrado" });
        if (pedido.Estado != EstadoPedidoVenta.BORRADOR) return BadRequest(new { message = "Solo se puede confirmar un pedido en estado BORRADOR" });
        pedido.Estado = EstadoPedidoVenta.CONFIRMADO;
        pedido.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(pedido);
    }

    [HttpPost("{id:guid}/anular")]
    public async Task<IActionResult> Anular(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var pedido = await _db.PedidosVenta.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (pedido is null) return NotFound(new { message = "Pedido de venta no encontrado" });
        if (pedido.Estado == EstadoPedidoVenta.FACTURADO) return BadRequest(new { message = "No se puede anular un pedido ya facturado" });
        pedido.Estado = EstadoPedidoVenta.ANULADO;
        pedido.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(pedido);
    }

    /// <summary>Factura un pedido confirmado: genera el comprobante (factura/boleta), descuenta stock y lo encola a SUNAT.</summary>
    [HttpPost("{id:guid}/convertir-a-venta")]
    public async Task<IActionResult> ConvertirAVenta(Guid id, ConvertirPedidoVentaDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var pedido = await _db.PedidosVenta
            .Include(p => p.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (pedido is null) return NotFound(new { message = "Pedido de venta no encontrado" });
        if (pedido.Estado != EstadoPedidoVenta.CONFIRMADO) return BadRequest(new { message = "Solo se puede facturar un pedido CONFIRMADO" });

        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == pedido.ClienteId && c.EmpresaId == empresaId);
        if (cliente is null) return BadRequest(new { message = "Cliente no encontrado" });
        if (cliente.Bloqueado) return BadRequest(new { message = "El cliente está bloqueado y no admite nuevas ventas" });
        if (!cliente.Activo) return BadRequest(new { message = "El cliente está inactivo" });

        var tipoComprobante = Enum.Parse<TipoComprobante>(dto.TipoComprobante);
        if (!SeriePorTipo.TryGetValue(tipoComprobante, out var serieNombre))
            return BadRequest(new { message = "Tipo de comprobante inválido para facturar un pedido de venta" });

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
        foreach (var d in pedido.Detalles)
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
            ClienteId = pedido.ClienteId,
            SerieId = serie.Id,
            TipoComprobante = tipoComprobante,
            Serie = serieNombre,
            Correlativo = serie.CorrelativoActual,
            Moneda = pedido.Moneda,
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
                $"{serieNombre}-{serie.CorrelativoActual} (pedido {pedido.Numero})");
        }

        // Encola el envío a SUNAT igual que las ventas directas y las cotizaciones convertidas.
        _db.SunatSyncQueue.Add(new SunatSyncQueue { ComprobanteId = comprobante.Id });

        pedido.Estado = EstadoPedidoVenta.FACTURADO;
        pedido.ComprobanteId = comprobante.Id;
        pedido.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(await Obtener(pedido.Id, empresaId));
    }

    private async Task<PedidoVenta?> Obtener(Guid id, Guid empresaId) =>
        await _db.PedidosVenta
            .Where(p => p.Id == id && p.EmpresaId == empresaId)
            .Include(p => p.Cliente)
            .Include(p => p.Cotizacion)
            .Include(p => p.Comprobante)
            .Include(p => p.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync();
}
