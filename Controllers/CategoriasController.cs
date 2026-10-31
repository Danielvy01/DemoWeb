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

/// <summary>Categorías de familia usadas para clasificar productos (p. ej. "Bebidas", "Abarrotes").</summary>
[Authorize]
[MenuPermiso("productos")]
[ApiController]
[Route("categorias")]
public class CategoriasController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly ICategoriaRepository _categoriaRepository;

    public CategoriasController(SiserpDbContext db, ICategoriaRepository categoriaRepository)
    {
        _db = db;
        _categoriaRepository = categoriaRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoriaDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var categoria = new Categoria { EmpresaId = empresaId, Nombre = dto.Nombre };
        // Alta a través del stored procedure sp_Categoria_Registrar.
        var creada = await _categoriaRepository.RegistrarAsync(categoria);
        return Ok(creada);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll([FromQuery] bool incluirInactivos = false)
    {
        var empresaId = User.GetEmpresaId();
        var categorias = await _db.Categorias
            .Where(c => c.EmpresaId == empresaId && (incluirInactivos || c.Activo))
            .OrderBy(c => c.Nombre)
            .ToListAsync();
        return Ok(categorias);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (categoria is null) return NotFound(new { message = "Categoría no encontrada" });
        return Ok(categoria);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCategoriaDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var categoria = await _db.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (categoria is null) return NotFound(new { message = "Categoría no encontrada" });

        if (dto.Nombre is not null) categoria.Nombre = dto.Nombre;

        // Modificación a través del stored procedure sp_Categoria_Modificar.
        var actualizada = await _categoriaRepository.ModificarAsync(categoria);
        if (actualizada is null) return NotFound(new { message = "Categoría no encontrada" });
        return Ok(actualizada);
    }

    /// <summary>Baja lógica; se conserva el histórico de productos ya asociados a la categoría.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        // Baja lógica a través del stored procedure sp_Categoria_EliminarLogico.
        var categoria = await _categoriaRepository.EliminarLogicoAsync(id, empresaId);
        if (categoria is null) return NotFound(new { message = "Categoría no encontrada" });
        return Ok(categoria);
    }

    [HttpPut("{id:guid}/reactivar")]
    public async Task<IActionResult> Reactivar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var categoria = await _db.Categorias.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (categoria is null) return NotFound(new { message = "Categoría no encontrada" });
        categoria.Activo = true;
        categoria.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(categoria);
    }
}
