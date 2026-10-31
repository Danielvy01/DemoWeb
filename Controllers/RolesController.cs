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
/// Maestro de Roles: cada rol otorga, por cada menú del catálogo, un nivel de acceso
/// (Lectura / Escritura / Control Total). Un usuario puede tener asignados varios roles
/// a la vez (ver <see cref="UsuariosController"/>); su acceso efectivo a cada menú es el
/// máximo nivel entre todos ellos.
/// </summary>
[Authorize]
[MenuPermiso("roles")]
[ApiController]
[Route("roles")]
public class RolesController : ControllerBase
{
    private readonly SiserpDbContext _db;

    public RolesController(SiserpDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> FindAll([FromQuery] bool incluirInactivos = false)
    {
        var empresaId = User.GetEmpresaId();
        var roles = await _db.Roles
            .Where(r => r.EmpresaId == empresaId && (incluirInactivos || r.Activo))
            .OrderByDescending(r => r.EsSistema).ThenBy(r => r.Nombre)
            .Select(r => new
            {
                r.Id,
                r.Nombre,
                r.Descripcion,
                r.EsSistema,
                r.Activo,
                CantidadUsuarios = r.UsuarioRoles.Count,
            })
            .ToListAsync();
        return Ok(roles);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.EmpresaId == empresaId);
        if (rol is null) return NotFound(new { message = "Rol no encontrado" });
        return Ok(rol);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRolDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var existeNombre = await _db.Roles.AnyAsync(r => r.EmpresaId == empresaId && r.Nombre == dto.Nombre);
        if (existeNombre) return Conflict(new { message = "Ya existe un rol con ese nombre" });

        var rol = new Rol { EmpresaId = empresaId, Nombre = dto.Nombre, Descripcion = dto.Descripcion };
        _db.Roles.Add(rol);
        await _db.SaveChangesAsync();
        return Ok(rol);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateRolDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.EmpresaId == empresaId);
        if (rol is null) return NotFound(new { message = "Rol no encontrado" });
        if (rol.EsSistema) return BadRequest(new { message = "El rol Administrador no puede modificarse" });

        if (dto.Nombre is not null) rol.Nombre = dto.Nombre;
        if (dto.Descripcion is not null) rol.Descripcion = dto.Descripcion;
        if (dto.Activo is not null) rol.Activo = dto.Activo.Value;
        rol.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(rol);
    }

    /// <summary>Baja lógica del rol; no está permitido para el rol de sistema "Administrador".</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.EmpresaId == empresaId);
        if (rol is null) return NotFound(new { message = "Rol no encontrado" });
        if (rol.EsSistema) return BadRequest(new { message = "El rol Administrador no puede eliminarse" });

        rol.Activo = false;
        rol.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(rol);
    }

    /// <summary>Matriz de permisos del rol: todos los menús del catálogo con el nivel actual (o "sin acceso" si no se ha configurado).</summary>
    [HttpGet("{id:guid}/permisos")]
    public async Task<IActionResult> GetPermisos(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.EmpresaId == empresaId);
        if (rol is null) return NotFound(new { message = "Rol no encontrado" });

        var permisosActuales = await _db.RolMenuPermisos
            .Where(p => p.RolId == id)
            .ToDictionaryAsync(p => p.MenuId);

        var matriz = await _db.Menus
            .Where(m => m.Activo)
            .OrderBy(m => m.Orden)
            .Select(m => new
            {
                m.Id,
                m.Codigo,
                m.Nombre,
                m.Grupo,
                Lectura = permisosActuales.ContainsKey(m.Id) && permisosActuales[m.Id].Lectura,
                Escritura = permisosActuales.ContainsKey(m.Id) && permisosActuales[m.Id].Escritura,
                ControlTotal = permisosActuales.ContainsKey(m.Id) && permisosActuales[m.Id].ControlTotal,
            })
            .ToListAsync();

        return Ok(matriz);
    }

    /// <summary>Reemplaza la matriz de permisos del rol (uno por menú). No aplica al rol "Administrador".</summary>
    [HttpPut("{id:guid}/permisos")]
    public async Task<IActionResult> SetPermisos(Guid id, SetPermisosRolDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.EmpresaId == empresaId);
        if (rol is null) return NotFound(new { message = "Rol no encontrado" });
        if (rol.EsSistema) return BadRequest(new { message = "El rol Administrador siempre tiene control total; no se puede modificar" });

        var actuales = await _db.RolMenuPermisos.Where(p => p.RolId == id).ToListAsync();
        _db.RolMenuPermisos.RemoveRange(actuales);

        foreach (var p in dto.Permisos.Where(p => p.Lectura || p.Escritura || p.ControlTotal))
        {
            _db.RolMenuPermisos.Add(new RolMenuPermiso
            {
                RolId = id,
                MenuId = p.MenuId,
                // Los niveles son acumulativos: Escritura implica Lectura y ControlTotal implica ambos.
                Lectura = p.Lectura || p.Escritura || p.ControlTotal,
                Escritura = p.Escritura || p.ControlTotal,
                ControlTotal = p.ControlTotal,
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Permisos actualizados" });
    }
}
