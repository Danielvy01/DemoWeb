using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Siserp.Api.Data;
using Siserp.Api.Dtos;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

public class AuthService
{
    private readonly SiserpDbContext _db;
    private readonly IConfiguration _config;
    private readonly RolSeederService _rolSeeder;
    private readonly IPermissionService _permissionService;

    public AuthService(SiserpDbContext db, IConfiguration config, RolSeederService rolSeeder, IPermissionService permissionService)
    {
        _db = db;
        _config = config;
        _rolSeeder = rolSeeder;
        _permissionService = permissionService;
    }

    public async Task<Usuario> ValidateUserAsync(string email, string password)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        if (usuario is null || !usuario.Activo)
        {
            throw new UnauthorizedAccessException("Credenciales invalidas");
        }
        if (!BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
        {
            throw new UnauthorizedAccessException("Credenciales invalidas");
        }
        return usuario;
    }

    public async Task<object> LoginAsync(string email, string password)
    {
        var usuario = await ValidateUserAsync(email, password);

        // Sincroniza el rol de sistema "Administrador" de la empresa con los menús activos
        // (otorga Control Total sobre cualquier menú agregado después de que el rol ya existía),
        // así el Administrador siempre tiene acceso a las funciones nuevas del ERP sin pasos
        // manuales adicionales.
        await _rolSeeder.CrearRolAdministradorAsync(usuario.EmpresaId);

        var jwtSecret = _config["Jwt:Secret"] ?? "change-this-secret-in-production";
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim("email", usuario.Email),
            new Claim("empresaId", usuario.EmpresaId.ToString()),
            new Claim("rol", usuario.Rol.ToString()),
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);

        var (roles, permisos) = await ObtenerRolesYPermisosAsync(usuario.Id);

        return new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            usuario = new
            {
                id = usuario.Id,
                email = usuario.Email,
                nombre = usuario.Nombre,
                rol = usuario.Rol.ToString(),
                empresaId = usuario.EmpresaId,
                roles,
                permisos,
            },
        };
    }

    /// <summary>Roles asignados y mapa de permisos efectivos (menú → nivel) de un usuario; usado en login y en /auth/permisos.</summary>
    public async Task<(List<string> Roles, Dictionary<string, string> Permisos)> ObtenerRolesYPermisosAsync(Guid usuarioId)
    {
        var roles = await _db.UsuarioRoles
            .Where(ur => ur.UsuarioId == usuarioId)
            .Select(ur => ur.Rol.Nombre)
            .ToListAsync();

        var permisosPorMenu = await _permissionService.GetPermisosEfectivosAsync(usuarioId);
        var permisos = permisosPorMenu.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        return (roles, permisos);
    }

    public async Task<object> RegisterAsync(RegisterDto dto)
    {
        var usuario = new Usuario
        {
            EmpresaId = dto.EmpresaId,
            Email = dto.Email,
            Nombre = dto.Nombre,
            Rol = Enum.Parse<RolUsuario>(dto.Rol),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        // Compatibilidad: el registro clásico (RolUsuario.ADMIN) asigna automáticamente
        // el rol de seguridad "Administrador" (Control Total en todos los menús).
        if (usuario.Rol == RolUsuario.ADMIN)
        {
            var rolAdmin = await _rolSeeder.CrearRolAdministradorAsync(usuario.EmpresaId);
            _db.UsuarioRoles.Add(new UsuarioRol { UsuarioId = usuario.Id, RolId = rolAdmin.Id });
            await _db.SaveChangesAsync();
        }

        return new { id = usuario.Id, email = usuario.Email, nombre = usuario.Nombre, rol = usuario.Rol.ToString() };
    }
}
