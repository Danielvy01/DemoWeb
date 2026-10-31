using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Dtos;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;

namespace Siserp.Api.Controllers;

/// <summary>Maestro de Usuarios: alta/edición, activar/desactivar y asignación de uno o varios roles por usuario.</summary>
[Authorize]
[ApiController]
[Route("usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly SiserpDbContext _db;

    public UsuariosController(SiserpDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Listado básico de usuarios de la empresa. No exige permiso sobre el menú "usuarios":
    /// se usa en selects de otras pantallas (p. ej. vendedor/comprador), y cualquier usuario
    /// autenticado puede consultarlo.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> FindAll([FromQuery] bool incluirInactivos = false)
    {
        var empresaId = User.GetEmpresaId();
        var usuarios = await _db.Usuarios
            .Where(u => u.EmpresaId == empresaId && (incluirInactivos || u.Activo))
            .OrderBy(u => u.Nombre)
            .Select(u => new
            {
                u.Id,
                u.Nombre,
                u.Email,
                u.Rol,
                u.Activo,
                Roles = u.UsuarioRoles.Select(ur => new { ur.RolId, ur.Rol.Nombre }),
            })
            .ToListAsync();
        return Ok(usuarios);
    }

    [MenuPermiso("usuarios")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var usuario = await _db.Usuarios
            .Where(u => u.Id == id && u.EmpresaId == empresaId)
            .Select(u => new
            {
                u.Id,
                u.Nombre,
                u.Email,
                u.Rol,
                u.Activo,
                Roles = u.UsuarioRoles.Select(ur => new { ur.RolId, ur.Rol.Nombre }),
            })
            .FirstOrDefaultAsync();
        if (usuario is null) return NotFound(new { message = "Usuario no encontrado" });
        return Ok(usuario);
    }

    [MenuPermiso("usuarios")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateUsuarioDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var existeEmail = await _db.Usuarios.AnyAsync(u => u.Email == dto.Email);
        if (existeEmail) return Conflict(new { message = "Ya existe un usuario con ese correo" });

        var usuario = new Usuario
        {
            EmpresaId = empresaId,
            Email = dto.Email,
            Nombre = dto.Nombre,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        };
        _db.Usuarios.Add(usuario);

        if (dto.RolIds is { Count: > 0 })
        {
            var rolesValidos = await _db.Roles
                .Where(r => r.EmpresaId == empresaId && dto.RolIds.Contains(r.Id))
                .Select(r => r.Id)
                .ToListAsync();
            foreach (var rolId in rolesValidos)
            {
                _db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = usuario.Id, RolId = rolId });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { usuario.Id, usuario.Email, usuario.Nombre, usuario.Activo });
    }

    [MenuPermiso("usuarios")]
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateUsuarioDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.EmpresaId == empresaId);
        if (usuario is null) return NotFound(new { message = "Usuario no encontrado" });

        if (dto.Nombre is not null) usuario.Nombre = dto.Nombre;
        if (dto.Email is not null) usuario.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Password)) usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        if (dto.Activo is not null) usuario.Activo = dto.Activo.Value;
        usuario.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { usuario.Id, usuario.Email, usuario.Nombre, usuario.Activo });
    }

    /// <summary>Baja lógica (desactiva el acceso sin borrar el historial asociado al usuario).</summary>
    [MenuPermiso("usuarios")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.EmpresaId == empresaId);
        if (usuario is null) return NotFound(new { message = "Usuario no encontrado" });

        usuario.Activo = false;
        usuario.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(usuario);
    }

    [MenuPermiso("usuarios")]
    [HttpPut("{id:guid}/reactivar")]
    public async Task<IActionResult> Reactivar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id && u.EmpresaId == empresaId);
        if (usuario is null) return NotFound(new { message = "Usuario no encontrado" });
        usuario.Activo = true;
        usuario.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(usuario);
    }

    /// <summary>Roles asignados actualmente al usuario (un usuario puede tener más de un rol).</summary>
    [MenuPermiso("usuarios")]
    [HttpGet("{id:guid}/roles")]
    public async Task<IActionResult> GetRoles(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var usuario = await _db.Usuarios.AnyAsync(u => u.Id == id && u.EmpresaId == empresaId);
        if (!usuario) return NotFound(new { message = "Usuario no encontrado" });

        var roles = await _db.UsuarioRoles
            .Where(ur => ur.UsuarioId == id)
            .Select(ur => new { ur.RolId, ur.Rol.Nombre })
            .ToListAsync();
        return Ok(roles);
    }

    /// <summary>Reemplaza la lista completa de roles asignados al usuario.</summary>
    [MenuPermiso("usuarios")]
    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> SetRoles(Guid id, SetRolesUsuarioDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var usuario = await _db.Usuarios.AnyAsync(u => u.Id == id && u.EmpresaId == empresaId);
        if (!usuario) return NotFound(new { message = "Usuario no encontrado" });

        var rolesValidos = await _db.Roles
            .Where(r => r.EmpresaId == empresaId && dto.RolIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync();

        var actuales = await _db.UsuarioRoles.Where(ur => ur.UsuarioId == id).ToListAsync();
        _db.UsuarioRoles.RemoveRange(actuales);
        foreach (var rolId in rolesValidos)
        {
            _db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = id, RolId = rolId });
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "Roles actualizados" });
    }
}
