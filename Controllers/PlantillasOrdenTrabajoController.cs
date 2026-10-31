using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Dtos;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;

namespace Siserp.Api.Controllers;

/// <summary>
/// Plantillas (receta/BOM) de orden de trabajo: predefinen qué producto se
/// fabrica y qué insumos (por unidad a producir) se requieren, para no tener
/// que especificarlos manualmente cada vez que se crea una orden de trabajo.
/// </summary>
[Authorize]
[MenuPermiso("plantillas-orden-trabajo")]
[ApiController]
[Route("plantillas-orden-trabajo")]
public class PlantillasOrdenTrabajoController : ControllerBase
{
    private readonly SiserpDbContext _db;

    public PlantillasOrdenTrabajoController(SiserpDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePlantillaOrdenTrabajoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == dto.ProductoId && p.EmpresaId == empresaId);
        if (producto is null) return BadRequest(new { message = "Producto a fabricar no encontrado" });
        if (dto.Insumos.Count == 0) return BadRequest(new { message = "La plantilla debe tener al menos un insumo" });

        var plantilla = new OrdenTrabajoPlantilla
        {
            EmpresaId = empresaId,
            Nombre = dto.Nombre,
            ProductoId = dto.ProductoId,
            Observaciones = dto.Observaciones,
            Insumos = dto.Insumos.Select(i => new OrdenTrabajoPlantillaInsumo
            {
                ProductoId = i.ProductoId,
                CantidadPorUnidad = i.CantidadPorUnidad,
            }).ToList(),
        };
        _db.PlantillasOrdenTrabajo.Add(plantilla);
        await _db.SaveChangesAsync();

        return Ok(await Obtener(plantilla.Id, empresaId));
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        var plantillas = await _db.PlantillasOrdenTrabajo
            .Where(p => p.EmpresaId == empresaId)
            .Include(p => p.Producto)
            .Include(p => p.Insumos).ThenInclude(i => i.Producto)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
        return Ok(plantillas);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var plantilla = await Obtener(id, empresaId);
        if (plantilla is null) return NotFound(new { message = "Plantilla no encontrada" });
        return Ok(plantilla);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePlantillaOrdenTrabajoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var plantilla = await _db.PlantillasOrdenTrabajo
            .Include(p => p.Insumos)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (plantilla is null) return NotFound(new { message = "Plantilla no encontrada" });

        if (dto.Nombre is not null) plantilla.Nombre = dto.Nombre;
        if (dto.Observaciones is not null) plantilla.Observaciones = dto.Observaciones;
        if (dto.Activo is not null) plantilla.Activo = dto.Activo.Value;
        if (dto.Insumos is not null)
        {
            _db.PlantillaOrdenTrabajoInsumos.RemoveRange(plantilla.Insumos);
            plantilla.Insumos = dto.Insumos.Select(i => new OrdenTrabajoPlantillaInsumo
            {
                ProductoId = i.ProductoId,
                CantidadPorUnidad = i.CantidadPorUnidad,
            }).ToList();
        }
        plantilla.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await Obtener(plantilla.Id, empresaId));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var plantilla = await _db.PlantillasOrdenTrabajo.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (plantilla is null) return NotFound(new { message = "Plantilla no encontrada" });
        plantilla.Activo = false;
        plantilla.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Plantilla desactivada" });
    }

    private async Task<OrdenTrabajoPlantilla?> Obtener(Guid id, Guid empresaId) =>
        await _db.PlantillasOrdenTrabajo
            .Where(p => p.Id == id && p.EmpresaId == empresaId)
            .Include(p => p.Producto)
            .Include(p => p.Insumos).ThenInclude(i => i.Producto)
            .FirstOrDefaultAsync();
}
