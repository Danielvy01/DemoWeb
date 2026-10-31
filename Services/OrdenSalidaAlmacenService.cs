using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

/// <summary>
/// Lógica compartida de órdenes de salida de almacén: usada tanto por el
/// controlador de órdenes de salida como por las órdenes de trabajo, que
/// generan y confirman una orden de salida para consumir sus insumos (en
/// vez de descontar el stock directamente).
/// </summary>
public class OrdenSalidaAlmacenService
{
    private readonly SiserpDbContext _db;
    private readonly ProductosService _productosService;

    public OrdenSalidaAlmacenService(SiserpDbContext db, ProductosService productosService)
    {
        _db = db;
        _productosService = productosService;
    }

    /// <summary>Crea la orden (en BORRADOR) sin afectar el stock todavía.</summary>
    public async Task<OrdenSalidaAlmacen> CrearAsync(
        Guid empresaId, Guid almacenId, string motivo, string? observaciones, Guid? ordenTrabajoId,
        IEnumerable<(Guid ProductoId, decimal Cantidad)> detalles)
    {
        var correlativo = await _db.OrdenesSalidaAlmacen.CountAsync(o => o.EmpresaId == empresaId) + 1;
        var orden = new OrdenSalidaAlmacen
        {
            EmpresaId = empresaId,
            Numero = $"OSA-{correlativo:D6}",
            AlmacenId = almacenId,
            Motivo = motivo,
            Observaciones = observaciones,
            OrdenTrabajoId = ordenTrabajoId,
            Estado = EstadoOrdenSalidaAlmacen.BORRADOR,
            Detalles = detalles.Select(d => new OrdenSalidaAlmacenDetalle { ProductoId = d.ProductoId, Cantidad = d.Cantidad }).ToList(),
        };
        _db.OrdenesSalidaAlmacen.Add(orden);
        await _db.SaveChangesAsync();
        return orden;
    }

    /// <summary>Confirma la orden: valida stock suficiente y lo descuenta (actualiza Kardex por promedio).</summary>
    public async Task ConfirmarAsync(OrdenSalidaAlmacen orden)
    {
        foreach (var detalle in orden.Detalles)
        {
            var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.ProductoId == detalle.ProductoId && s.AlmacenId == orden.AlmacenId);
            if (stock is null || stock.Cantidad < detalle.Cantidad)
                throw new InvalidOperationException($"Stock insuficiente para el producto {detalle.ProductoId}");
        }

        foreach (var detalle in orden.Detalles)
        {
            await _productosService.AjustarStockAsync(
                detalle.ProductoId, orden.AlmacenId, detalle.Cantidad, TipoMovimiento.SALIDA,
                $"Orden de salida de almacén {orden.Numero}");
        }

        orden.Estado = EstadoOrdenSalidaAlmacen.CONFIRMADA;
        orden.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
