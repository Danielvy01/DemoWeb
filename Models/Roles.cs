using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

/// <summary>
/// Nivel de acceso que un rol puede tener sobre un menú/módulo específico.
/// Los niveles son acumulativos: Escritura incluye Lectura; ControlTotal incluye Escritura y Lectura.
/// </summary>
public enum NivelAcceso
{
    /// <summary>Sin acceso: el menú no aparece y la API rechaza cualquier operación.</summary>
    NINGUNO = 0,
    /// <summary>Puede ver/listar/consultar (GET), pero no puede grabar ni eliminar.</summary>
    LECTURA = 1,
    /// <summary>Además de Lectura, puede grabar (crear/modificar); no puede eliminar.</summary>
    ESCRITURA = 2,
    /// <summary>Acceso completo: además de Lectura y Escritura, puede usar los botones de grabar y eliminar.</summary>
    CONTROL_TOTAL = 3,
}

/// <summary>
/// Rol de seguridad de la empresa (tenant). Un usuario puede tener asignados varios roles
/// (ver <see cref="UsuarioRol"/>) y su acceso efectivo a cada menú es el máximo nivel
/// otorgado por cualquiera de sus roles.
/// </summary>
public class Rol
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(150)] public string Nombre { get; set; } = null!;
    [MaxLength(500)] public string? Descripcion { get; set; }
    /// <summary>Marca el rol "Administrador" creado automáticamente con la empresa; no se puede eliminar.</summary>
    public bool EsSistema { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RolMenuPermiso> Permisos { get; set; } = new List<RolMenuPermiso>();
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}

/// <summary>
/// Catálogo global de menús/módulos de la aplicación (uno por pantalla del sidebar).
/// Sirve para armar la matriz de permisos del maestro de Roles y para validar el acceso
/// tanto en el backend (atributo <c>MenuPermiso</c>) como en el frontend (guard/directiva).
/// </summary>
public class Menu
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Código único y estable (coincide con la ruta del frontend, p. ej. "clientes").</summary>
    [MaxLength(100)] public string Codigo { get; set; } = null!;
    [MaxLength(150)] public string Nombre { get; set; } = null!;
    [MaxLength(100)] public string Grupo { get; set; } = null!;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<RolMenuPermiso> Permisos { get; set; } = new List<RolMenuPermiso>();
}

/// <summary>Nivel de acceso otorgado por un rol a un menú específico.</summary>
public class RolMenuPermiso
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public Guid MenuId { get; set; }
    public Menu Menu { get; set; } = null!;
    public bool Lectura { get; set; }
    public bool Escritura { get; set; }
    public bool ControlTotal { get; set; }
}

public static class RolMenuPermisoExtensions
{
    /// <summary>Nivel de acceso equivalente a las tres banderas booleanas (Lectura/Escritura/ControlTotal).</summary>
    public static NivelAcceso NivelDe(bool lectura, bool escritura, bool controlTotal) =>
        controlTotal ? NivelAcceso.CONTROL_TOTAL :
        escritura ? NivelAcceso.ESCRITURA :
        lectura ? NivelAcceso.LECTURA : NivelAcceso.NINGUNO;
}

/// <summary>Asignación de un rol a un usuario (relación muchos a muchos: un usuario puede tener varios roles).</summary>
public class UsuarioRol
{
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public Guid RolId { get; set; }
    public Rol Rol { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
