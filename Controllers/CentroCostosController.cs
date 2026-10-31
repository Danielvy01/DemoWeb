using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Dtos;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;

namespace Siserp.Api.Controllers;

/// <summary>Mantenimiento de Centros de Costo (áreas de la empresa: Operaciones, Ventas, Sistemas, etc.) usados para clasificar al personal (RRHH).</summary>
[Authorize]
[MenuPermiso("centros-costo")]
[ApiController]
[Route("centros-costo")]
public class CentroCostosController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly ICentroCostoRepository _centroCostoRepository;

    public CentroCostosController(SiserpDbContext db, ICentroCostoRepository centroCostoRepository)
    {
        _db = db;
        _centroCostoRepository = centroCostoRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCentroCostoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var centroCosto = new CentroCosto { EmpresaId = empresaId, Nombre = dto.Nombre, Descripcion = dto.Descripcion };
        // Alta a través del stored procedure sp_CentroCosto_Registrar.
        var creado = await _centroCostoRepository.RegistrarAsync(centroCosto);
        return Ok(creado);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll([FromQuery] bool incluirInactivos = false)
    {
        var empresaId = User.GetEmpresaId();
        var centrosCosto = await _db.CentrosCosto
            .Where(c => c.EmpresaId == empresaId && (incluirInactivos || c.Activo))
            .OrderBy(c => c.Nombre)
            .ToListAsync();
        return Ok(centrosCosto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var centroCosto = await _db.CentrosCosto.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (centroCosto is null) return NotFound(new { message = "Centro de costo no encontrado" });
        return Ok(centroCosto);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCentroCostoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var centroCosto = await _db.CentrosCosto.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (centroCosto is null) return NotFound(new { message = "Centro de costo no encontrado" });

        if (dto.Nombre is not null) centroCosto.Nombre = dto.Nombre;
        if (dto.Descripcion is not null) centroCosto.Descripcion = dto.Descripcion;

        // Modificación a través del stored procedure sp_CentroCosto_Modificar.
        var actualizado = await _centroCostoRepository.ModificarAsync(centroCosto);
        if (actualizado is null) return NotFound(new { message = "Centro de costo no encontrado" });
        return Ok(actualizado);
    }

    /// <summary>Baja lógica; se conserva el histórico del personal ya asociado al centro de costo.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var tienePersonal = await _db.Empleados.AnyAsync(e => e.CentroCostoId == id && e.EmpresaId == empresaId && e.Activo);
        if (tienePersonal) return BadRequest(new { message = "No se puede desactivar: hay personal activo asignado a este centro de costo" });

        // Baja lógica a través del stored procedure sp_CentroCosto_EliminarLogico.
        var centroCosto = await _centroCostoRepository.EliminarLogicoAsync(id, empresaId);
        if (centroCosto is null) return NotFound(new { message = "Centro de costo no encontrado" });
        return Ok(centroCosto);
    }

    [HttpPut("{id:guid}/reactivar")]
    public async Task<IActionResult> Reactivar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var centroCosto = await _db.CentrosCosto.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (centroCosto is null) return NotFound(new { message = "Centro de costo no encontrado" });
        centroCosto.Activo = true;
        centroCosto.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(centroCosto);
    }
}
