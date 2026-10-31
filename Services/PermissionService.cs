using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

public interface IPermissionService
{
    /// <summary>
    /// Calcula el acceso efectivo del usuario a cada menú: el máximo nivel otorgado por
    /// cualquiera de los roles que tenga asignados (un usuario puede tener varios roles).
    /// </summary>
    Task<Dictionary<string, NivelAcceso>> GetPermisosEfectivosAsync(Guid usuarioId);

    /// <summary>Nivel de acceso efectivo del usuario a un menú puntual (NINGUNO si no tiene acceso).</summary>
    Task<NivelAcceso> GetNivelAsync(Guid usuarioId, string menuCodigo);
}

public class PermissionService : IPermissionService
{
    private readonly SiserpDbContext _db;

    public PermissionService(SiserpDbContext db)
    {
        _db = db;
    }

    public async Task<Dictionary<string, NivelAcceso>> GetPermisosEfectivosAsync(Guid usuarioId)
    {
        var permisos = await _db.UsuarioRoles
            .Where(ur => ur.UsuarioId == usuarioId && ur.Rol.Activo)
            .SelectMany(ur => ur.Rol.Permisos)
            .Select(p => new { p.Menu.Codigo, p.Lectura, p.Escritura, p.ControlTotal })
            .ToListAsync();

        var resultado = new Dictionary<string, NivelAcceso>();
        foreach (var p in permisos)
        {
            var nivel = p.ControlTotal ? NivelAcceso.CONTROL_TOTAL
                : p.Escritura ? NivelAcceso.ESCRITURA
                : p.Lectura ? NivelAcceso.LECTURA
                : NivelAcceso.NINGUNO;

            if (!resultado.TryGetValue(p.Codigo, out var actual) || nivel > actual)
            {
                resultado[p.Codigo] = nivel;
            }
        }
        return resultado;
    }

    public async Task<NivelAcceso> GetNivelAsync(Guid usuarioId, string menuCodigo)
    {
        var permisos = await GetPermisosEfectivosAsync(usuarioId);
        return permisos.TryGetValue(menuCodigo, out var nivel) ? nivel : NivelAcceso.NINGUNO;
    }
}
