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
/// Endpoints pensados para el dispositivo móvil de almacén: buscar un producto
/// escaneando su código de barras y registrar el conteo físico (toma de
/// inventario), generando el ajuste correspondiente en el Kardex.
/// </summary>
[Authorize]
[MenuPermiso("inventario-movil")]
[ApiController]
[Route("inventario")]
public class InventarioController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly ProductosService _productosService;

    public InventarioController(SiserpDbContext db, ProductosService productosService)
    {
        _db = db;
        _productosService = productosService;
    }

    /// <summary>Busca un producto por su código (el mismo valor codificado en el código de barras).</summary>
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string codigo)
    {
        var empresaId = User.GetEmpresaId();
        var producto = await _db.Productos
            .Where(p => p.EmpresaId == empresaId && p.Codigo == codigo)
            .Include(p => p.Stocks).ThenInclude(s => s.Almacen)
            .FirstOrDefaultAsync();
        if (producto is null) return NotFound(new { message = "Producto no encontrado para el código escaneado" });

        return Ok(new
        {
            producto.Id,
            producto.Codigo,
            producto.Descripcion,
            producto.UnidadMedida,
            producto.StockMinimo,
            stockTotal = producto.Stocks.Sum(s => s.Cantidad),
            stocksPorAlmacen = producto.Stocks.Select(s => new { s.AlmacenId, almacen = s.Almacen.Nombre, s.Cantidad }),
        });
    }

    /// <summary>
    /// Registra el conteo físico (toma de inventario) de un producto en un almacén: calcula la
    /// diferencia contra el stock del sistema y genera un movimiento de AJUSTE en el Kardex.
    /// </summary>
    [HttpPost("ajustar")]
    public async Task<IActionResult> Ajustar(AjusteInventarioDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == dto.ProductoId && p.EmpresaId == empresaId);
        if (producto is null) return NotFound(new { message = "Producto no encontrado" });
        var almacen = await _db.Almacenes.FirstOrDefaultAsync(a => a.Id == dto.AlmacenId && a.EmpresaId == empresaId);
        if (almacen is null) return NotFound(new { message = "Almacén no encontrado" });

        var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.ProductoId == dto.ProductoId && s.AlmacenId == dto.AlmacenId);
        var cantidadSistema = stock?.Cantidad ?? 0;
        var diferencia = dto.CantidadContada - cantidadSistema;

        if (diferencia == 0)
        {
            return Ok(new { message = "Sin diferencias, no se genera ajuste", cantidadSistema, cantidadContada = dto.CantidadContada });
        }

        var referencia = $"Toma de inventario móvil{(string.IsNullOrWhiteSpace(dto.Observacion) ? "" : $" - {dto.Observacion}")}";
        var movimiento = await _productosService.AjustarStockAsync(
            dto.ProductoId, dto.AlmacenId, diferencia, TipoMovimiento.AJUSTE, referencia);

        return Ok(new
        {
            movimiento.Id,
            cantidadAnterior = cantidadSistema,
            cantidadContada = dto.CantidadContada,
            diferencia,
        });
    }
}
