using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;

namespace Siserp.Api.Controllers;

/// <summary>
/// Kardex de inventario: historial de movimientos por producto con saldo corriente
/// (cantidad y valorizado, método promedio ponderado) y alertas de stock por debajo
/// del umbral mínimo configurado en cada producto.
/// </summary>
[Authorize]
[MenuPermiso("kardex")]
[ApiController]
[Route("kardex")]
public class KardexController : ControllerBase
{
    private readonly SiserpDbContext _db;

    public KardexController(SiserpDbContext db)
    {
        _db = db;
    }

    /// <summary>Detalle de kardex de un producto (opcionalmente filtrado por almacén y rango de fechas).</summary>
    [HttpGet("{productoId:guid}")]
    public async Task<IActionResult> Detalle(Guid productoId, [FromQuery] Guid? almacenId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == productoId && p.EmpresaId == empresaId);
        if (producto is null) return NotFound(new { message = "Producto no encontrado" });

        var query = _db.MovimientosInventario
            .Where(m => m.ProductoId == productoId)
            .Include(m => m.Almacen)
            .AsQueryable();

        if (almacenId is not null) query = query.Where(m => m.AlmacenId == almacenId);
        if (desde is not null) query = query.Where(m => m.CreatedAt >= desde);
        if (hasta is not null) query = query.Where(m => m.CreatedAt <= hasta);

        var movimientos = await query.OrderBy(m => m.CreatedAt).ToListAsync();

        decimal saldoCantidad = 0;
        decimal saldoValor = 0;
        var filas = new List<object>();

        foreach (var m in movimientos)
        {
            decimal entradaCant = 0, salidaCant = 0;
            decimal costoUnitarioUsado;

            if (m.Tipo == TipoMovimiento.ENTRADA)
            {
                entradaCant = m.Cantidad;
                costoUnitarioUsado = m.CostoUnitario ?? (saldoCantidad > 0 ? saldoValor / saldoCantidad : producto.PrecioVenta);
                saldoValor += entradaCant * costoUnitarioUsado;
                saldoCantidad += entradaCant;
            }
            else if (m.Tipo == TipoMovimiento.SALIDA)
            {
                salidaCant = m.Cantidad;
                costoUnitarioUsado = saldoCantidad > 0 ? saldoValor / saldoCantidad : 0;
                saldoValor -= salidaCant * costoUnitarioUsado;
                saldoCantidad -= salidaCant;
            }
            else
            {
                // AJUSTE: la cantidad puede representar un incremento o decremento neto (toma de inventario).
                costoUnitarioUsado = saldoCantidad > 0 ? saldoValor / saldoCantidad : producto.PrecioVenta;
                if (m.Cantidad >= 0) entradaCant = m.Cantidad; else salidaCant = -m.Cantidad;
                saldoCantidad += m.Cantidad;
                saldoValor = saldoCantidad > 0 ? saldoCantidad * costoUnitarioUsado : 0;
            }

            if (saldoCantidad < 0) saldoCantidad = 0;
            if (saldoValor < 0) saldoValor = 0;

            filas.Add(new
            {
                m.Id,
                fecha = m.CreatedAt,
                almacen = m.Almacen.Nombre,
                tipo = m.Tipo.ToString(),
                referencia = m.Referencia,
                entrada = entradaCant,
                salida = salidaCant,
                costoUnitario = Math.Round(costoUnitarioUsado, 4),
                saldoCantidad = Math.Round(saldoCantidad, 2),
                saldoValorizado = Math.Round(saldoValor, 2),
            });
        }

        return Ok(new
        {
            producto = new { producto.Id, producto.Codigo, producto.Descripcion, producto.UnidadMedida, producto.StockMinimo },
            movimientos = filas,
            saldoActual = Math.Round(saldoCantidad, 2),
            valorizadoActual = Math.Round(saldoValor, 2),
        });
    }

    /// <summary>Productos cuyo stock total (todos los almacenes) está por debajo de su stock mínimo.</summary>
    [HttpGet("alertas")]
    public async Task<IActionResult> Alertas()
    {
        var empresaId = User.GetEmpresaId();
        var productos = await _db.Productos
            .Where(p => p.EmpresaId == empresaId && p.Activo && p.StockMinimo > 0)
            .Include(p => p.Stocks)
            .ToListAsync();

        var alertas = productos
            .Select(p => new
            {
                p.Id,
                p.Codigo,
                p.Descripcion,
                p.StockMinimo,
                stockActual = p.Stocks.Sum(s => s.Cantidad),
            })
            .Where(x => x.stockActual < x.StockMinimo)
            .OrderBy(x => x.stockActual)
            .ToList();

        return Ok(alertas);
    }
}
