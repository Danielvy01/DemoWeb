using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

/// <summary>
/// Crea el rol "Administrador" de una empresa: se genera automáticamente al registrar la
/// empresa (y, de forma idempotente, para empresas ya existentes vía migración) con
/// Control Total sobre todos los menús del catálogo, de modo que siempre exista al menos
/// un rol capaz de ver y administrar todas las opciones del sistema.
/// </summary>
public class RolSeederService
{
    private readonly SiserpDbContext _db;

    public RolSeederService(SiserpDbContext db)
    {
        _db = db;
    }

    public const string NombreRolAdministrador = "Administrador";

    public async Task<Rol> CrearRolAdministradorAsync(Guid empresaId)
    {
        var existente = await _db.Roles
            .Include(r => r.Permisos)
            .FirstOrDefaultAsync(r => r.EmpresaId == empresaId && r.EsSistema);
        // Se consultan los menús activos directamente de la tabla (y no del catálogo estático
        // MenuCatalogo) para que el Administrador reciba automáticamente el acceso a cualquier
        // menú nuevo, incluso si fue agregado a la base de datos vía migración de datos.
        var menuIds = await _db.Menus.Where(m => m.Activo).Select(m => m.Id).ToListAsync();

        if (existente is not null)
        {
            // Sincroniza retroactivamente: si se agregó un menú nuevo al sistema (p. ej.
            // "Exportación PLE") después de que este rol ya existía, se le otorga Control Total
            // automáticamente en el siguiente uso, sin necesidad de una migración de datos
            // adicional por cada menú nuevo.
            var menuIdsConPermiso = existente.Permisos.Select(p => p.MenuId).ToHashSet();
            var faltantes = menuIds.Where(id => !menuIdsConPermiso.Contains(id));
            var huboCambios = false;
            foreach (var menuId in faltantes)
            {
                existente.Permisos.Add(new RolMenuPermiso
                {
                    RolId = existente.Id,
                    MenuId = menuId,
                    Lectura = true,
                    Escritura = true,
                    ControlTotal = true,
                });
                huboCambios = true;
            }
            if (huboCambios)
            {
                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Dos solicitudes concurrentes (p. ej. logins simultáneos) pueden intentar
                    // sincronizar los mismos permisos faltantes al mismo tiempo: una gana la
                    // carrera y la otra choca contra el índice único (RolId, MenuId) o una
                    // excepción de concurrencia. Como la operación es idempotente (el objetivo es
                    // que el permiso exista), se descartan los cambios locales en conflicto y se
                    // recarga el estado real desde la base de datos en vez de propagar un 500.
                    foreach (var entry in _db.ChangeTracker.Entries<RolMenuPermiso>().ToList())
                    {
                        if (entry.State == EntityState.Added) entry.State = EntityState.Detached;
                    }
                    existente.Permisos = await _db.RolMenuPermisos
                        .Where(p => p.RolId == existente.Id)
                        .ToListAsync();
                }
            }
            return existente;
        }

        var rol = new Rol
        {
            EmpresaId = empresaId,
            Nombre = NombreRolAdministrador,
            Descripcion = "Rol de sistema con acceso total a todas las opciones del ERP.",
            EsSistema = true,
        };
        foreach (var menuId in menuIds)
        {
            rol.Permisos.Add(new RolMenuPermiso
            {
                MenuId = menuId,
                Lectura = true,
                Escritura = true,
                ControlTotal = true,
            });
        }
        _db.Roles.Add(rol);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Carrera al crear la empresa/rol por primera vez: otra solicitud concurrente ya
            // insertó el rol "Administrador" (violación del índice único EmpresaId+Nombre).
            // Se descarta el intento local y se devuelve el rol ya creado por la otra solicitud.
            _db.Entry(rol).State = EntityState.Detached;
            foreach (var permiso in rol.Permisos) _db.Entry(permiso).State = EntityState.Detached;

            var existenteTrasConflicto = await _db.Roles
                .Include(r => r.Permisos)
                .FirstOrDefaultAsync(r => r.EmpresaId == empresaId && r.EsSistema);
            if (existenteTrasConflicto is not null) return existenteTrasConflicto;
            throw;
        }
        return rol;
    }
}
