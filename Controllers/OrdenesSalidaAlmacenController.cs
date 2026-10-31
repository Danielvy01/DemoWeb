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
/// Órdenes de salida de almacén: sustentan y ejecutan salidas de mercadería
/// distintas a la venta (consumo de insumos de una orden de trabajo, merma,
/// consumo interno, traslados). Al CONFIRMARSE descuentan el stock del
/// almacén y generan el movimiento de Kardex (costeado por promedio).
/// </summary>
[Authorize]
[MenuPermiso("ordenes-salida-almacen")]
[ApiController]
[Route("ordenes-salida-almacen")]
public class OrdenesSalidaAlmacenController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly OrdenSalidaAlmacenService _ordenSalidaService;

    public OrdenesSalidaAlmacenController(SiserpDbContext db, OrdenSalidaAlmacenService ordenSalidaService)
    {
        _db = db;
        _ordenSalidaService = ordenSalidaService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrdenSalidaAlmacenDto dto)
    {
        var empresaId = User.GetEmpresaId();
        if (dto.Detalles.Count == 0) return BadRequest(new { message = "La orden debe tener al menos un ítem" });

        var almacen = await _db.Almacenes.FirstOrDefaultAsync(a => a.Id == dto.AlmacenId && a.EmpresaId == empresaId);
        if (almacen is null) return BadRequest(new { message = "Almacén no encontrado" });

        var orden = await _ordenSalidaService.CrearAsync(empresaId, dto.AlmacenId, dto.Motivo, dto.Observaciones, null,
            dto.Detalles.Select(d => (d.ProductoId, d.Cantidad)));

        return Ok(await Obtener(orden.Id, empresaId));
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        var ordenes = await _db.OrdenesSalidaAlmacen
            .Where(o => o.EmpresaId == empresaId)
            .Include(o => o.Almacen)
            .Include(o => o.OrdenTrabajo)
            .Include(o => o.Detalles)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return Ok(ordenes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await Obtener(id, empresaId);
        if (orden is null) return NotFound(new { message = "Orden de salida de almacén no encontrada" });
        return Ok(orden);
    }

    [HttpPost("{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await _db.OrdenesSalidaAlmacen
            .Include(o => o.Detalles)
            .FirstOrDefaultAsync(o => o.Id == id && o.EmpresaId == empresaId);
        if (orden is null) return NotFound(new { message = "Orden de salida de almacén no encontrada" });
        if (orden.Estado != EstadoOrdenSalidaAlmacen.BORRADOR) return BadRequest(new { message = "Solo se puede confirmar una orden en estado BORRADOR" });

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            await _ordenSalidaService.ConfirmarAsync(orden);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        await transaction.CommitAsync();

        return Ok(await Obtener(orden.Id, empresaId));
    }

    [HttpPost("{id:guid}/anular")]
    public async Task<IActionResult> Anular(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await _db.OrdenesSalidaAlmacen.FirstOrDefaultAsync(o => o.Id == id && o.EmpresaId == empresaId);
        if (orden is null) return NotFound(new { message = "Orden de salida de almacén no encontrada" });
        if (orden.Estado == EstadoOrdenSalidaAlmacen.CONFIRMADA) return BadRequest(new { message = "No se puede anular una orden ya confirmada" });
        orden.Estado = EstadoOrdenSalidaAlmacen.ANULADA;
        orden.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(orden);
    }

    private async Task<OrdenSalidaAlmacen?> Obtener(Guid id, Guid empresaId) =>
        await _db.OrdenesSalidaAlmacen
            .Where(o => o.Id == id && o.EmpresaId == empresaId)
            .Include(o => o.Almacen)
            .Include(o => o.OrdenTrabajo)
            .Include(o => o.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync();
}
