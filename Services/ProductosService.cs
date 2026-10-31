using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

public class ProductosService
{
    private readonly SiserpDbContext _db;
    private readonly WebPushNotificationService _webPush;

    public ProductosService(SiserpDbContext db, WebPushNotificationService webPush)
    {
        _db = db;
        _webPush = webPush;
    }

    /// <summary>
    /// Porcentaje (0-100) de descuento del producto que corresponde según la clasificación
    /// comercial del cliente (Normal no tiene descuento; Mayorista/Super Mayorista usan el
    /// porcentaje configurado en el propio producto).
    /// </summary>
    public static decimal ObtenerPorcentajeDescuento(Producto producto, Cliente cliente) => cliente.Clasificacion switch
    {
        ClasificacionCliente.MAYORISTA => producto.DescuentoMayoristaPct,
        ClasificacionCliente.SUPER_MAYORISTA => producto.DescuentoSuperMayoristaPct,
        _ => 0m,
    };

    /// <summary>Precio unitario de venta ya aplicado el descuento por clasificación del cliente.</summary>
    public static decimal CalcularPrecioUnitario(Producto producto, Cliente cliente)
    {
        var porcentaje = ObtenerPorcentajeDescuento(producto, cliente);
        return Math.Round(producto.PrecioVenta * (1 - porcentaje / 100m), 2);
    }

    /// <summary>Ajusta el stock de un producto en un almacen (usado por ventas, compras y ajustes manuales).</summary>
    public async Task<MovimientoInventario> AjustarStockAsync(
        Guid productoId, Guid almacenId, decimal cantidad, TipoMovimiento tipo, string? referencia = null, decimal? costoUnitario = null)
    {
        var esEntrada = tipo == TipoMovimiento.ENTRADA;
        var delta = tipo switch
        {
            TipoMovimiento.SALIDA => -Math.Abs(cantidad),
            TipoMovimiento.ENTRADA => Math.Abs(cantidad),
            _ => cantidad, // AJUSTE: el signo lo define quien llama (puede ser positivo o negativo)
        };

        var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.ProductoId == productoId && s.AlmacenId == almacenId);
        if (stock is null)
        {
            stock = new Stock { ProductoId = productoId, AlmacenId = almacenId, Cantidad = delta };
            _db.Stocks.Add(stock);
        }
        else
        {
            stock.Cantidad += delta;
            stock.UpdatedAt = DateTime.UtcNow;
        }

        var movimiento = new MovimientoInventario
        {
            ProductoId = productoId,
            AlmacenId = almacenId,
            Tipo = tipo,
            Cantidad = Math.Abs(cantidad),
            CostoUnitario = costoUnitario,
            Referencia = referencia,
        };
        _db.MovimientosInventario.Add(movimiento);
        await _db.SaveChangesAsync();

        await VerificarUmbralStockAsync(productoId);

        return movimiento;
    }

    /// <summary>
    /// Revisa si el stock total (todos los almacenes) de un producto cayó por debajo
    /// del umbral mínimo configurado y, de ser así, envía una notificación push a la
    /// empresa (con throttling de 1 alerta cada 24h por producto para no saturar).
    /// </summary>
    public async Task VerificarUmbralStockAsync(Guid productoId)
    {
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == productoId);
        if (producto is null || producto.StockMinimo <= 0) return;

        var stockTotal = await _db.Stocks.Where(s => s.ProductoId == productoId).SumAsync(s => (decimal?)s.Cantidad) ?? 0;

        if (stockTotal < producto.StockMinimo)
        {
            var yaAlertadoRecientemente = producto.UltimaAlertaStockBajo is not null &&
                                          producto.UltimaAlertaStockBajo.Value > DateTime.UtcNow.AddHours(-24);
            if (!yaAlertadoRecientemente)
            {
                producto.UltimaAlertaStockBajo = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await _webPush.EnviarATodaLaEmpresaAsync(
                    producto.EmpresaId,
                    "Stock bajo umbral",
                    $"{producto.Descripcion} ({producto.Codigo}): stock actual {stockTotal}, mínimo {producto.StockMinimo}.",
                    new { tipo = "STOCK_BAJO", productoId = producto.Id });
            }
        }
        else if (producto.UltimaAlertaStockBajo is not null)
        {
            // El stock se recuperó por encima del umbral: se resetea para que la próxima
            // caída genere una alerta nueva.
            producto.UltimaAlertaStockBajo = null;
            await _db.SaveChangesAsync();
        }
    }
}
