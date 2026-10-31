using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

/// <summary>
/// Valida el "código de administrador" (credenciales de un usuario con rol Administrador)
/// que, al estilo CONCAR, se exige como autorización adicional para operaciones sensibles
/// e irreversibles: anular una compra registrada o anular/emitir una Nota de Crédito de una
/// venta. No crea un PIN/clave nuevo: reutiliza el email + contraseña de cualquier usuario
/// Administrador de la misma empresa, evitando duplicar el mecanismo de autenticación.
/// </summary>
public class AutorizacionAdminService
{
    private readonly SiserpDbContext _db;

    public AutorizacionAdminService(SiserpDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Verifica que <paramref name="adminEmail"/>/<paramref name="adminPassword"/> correspondan
    /// a un usuario activo, de la misma empresa y con rol Administrador (ya sea el rol clásico
    /// <see cref="RolUsuario.ADMIN"/> o el rol de seguridad "Administrador" asignado vía roles).
    /// Lanza <see cref="UnauthorizedAccessException"/> si la autorización no es válida.
    /// </summary>
    public async Task<Usuario> ValidarAsync(Guid empresaId, string? adminEmail, string? adminPassword)
    {
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            throw new UnauthorizedAccessException("Debe ingresar el usuario y código (contraseña) de un Administrador para autorizar esta operación");

        var admin = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == adminEmail && u.EmpresaId == empresaId);
        if (admin is null || !admin.Activo || !BCrypt.Net.BCrypt.Verify(adminPassword, admin.PasswordHash))
            throw new UnauthorizedAccessException("Código de administrador inválido");

        if (!await EsAdministradorAsync(admin))
            throw new UnauthorizedAccessException("El usuario indicado no tiene permisos de Administrador");

        return admin;
    }

    /// <summary>
    /// Igual que <see cref="ValidarAsync"/>, pero si el usuario que ya inició sesión
    /// (<paramref name="usuarioActualId"/>) es por sí mismo un Administrador, se omite el pedido
    /// de usuario/contraseña adicional: un Administrador no necesita reautorizarse a sí mismo
    /// para anular/emitir una Nota de Crédito.
    /// </summary>
    public async Task<Usuario> ValidarAsync(Guid empresaId, Guid usuarioActualId, string? adminEmail, string? adminPassword)
    {
        var usuarioActual = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioActualId && u.EmpresaId == empresaId);
        if (usuarioActual is not null && usuarioActual.Activo && await EsAdministradorAsync(usuarioActual))
            return usuarioActual;

        return await ValidarAsync(empresaId, adminEmail, adminPassword);
    }

    private async Task<bool> EsAdministradorAsync(Usuario usuario)
        => usuario.Rol == RolUsuario.ADMIN
            || await _db.UsuarioRoles.AnyAsync(ur => ur.UsuarioId == usuario.Id && ur.Rol.Nombre == RolSeederService.NombreRolAdministrador);
}
