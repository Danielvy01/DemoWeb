using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

/// <summary>
/// Orden de trabajo: documento que se emite para iniciar la producción de un
/// producto terminado. Consume insumos (productos) del almacén y, al
/// completarse, ingresa stock del producto fabricado.
/// </summary>
public class OrdenTrabajo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(20)] public string Numero { get; set; } = null!;
    /// <summary>Orden de compra que originó esta orden de trabajo (p. ej. cuando se fabrica a partir de insumos recién comprados). Opcional.</summary>
    public Guid? OrdenCompraId { get; set; }
    public OrdenCompra? OrdenCompra { get; set; }
    /// <summary>Plantilla predefinida (producto + insumos) usada para generar esta orden, si aplica.</summary>
    public Guid? PlantillaId { get; set; }
    public OrdenTrabajoPlantilla? Plantilla { get; set; }
    /// <summary>Orden de salida de almacén generada al iniciar la orden, que es quien efectivamente descuenta los insumos del stock.</summary>
    public Guid? OrdenSalidaAlmacenId { get; set; }
    public OrdenSalidaAlmacen? OrdenSalidaAlmacen { get; set; }
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public Guid AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;
    [Column(TypeName = "decimal(12,2)")] public decimal CantidadPlanificada { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal CantidadProducida { get; set; }
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFin { get; set; }
    public EstadoOrdenTrabajo Estado { get; set; } = EstadoOrdenTrabajo.PLANIFICADA;
    [MaxLength(500)] public string? Observaciones { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrdenTrabajoInsumo> Insumos { get; set; } = new List<OrdenTrabajoInsumo>();
}

/// <summary>Insumo (producto/materia prima) requerido para fabricar el producto de la orden.</summary>
public class OrdenTrabajoInsumo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrdenTrabajoId { get; set; }
    public OrdenTrabajo OrdenTrabajo { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [Column(TypeName = "decimal(12,2)")] public decimal CantidadRequerida { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal CantidadConsumida { get; set; }
}

/// <summary>
/// Plantilla predefinida de orden de trabajo: fija de antemano qué producto se
/// fabrica y qué insumos (y en qué proporción por unidad) se necesitan, para
/// no tener que especificarlos manualmente cada vez. Al crear una orden de
/// trabajo a partir de una plantilla, los insumos se calculan multiplicando
/// la cantidad por unidad de la plantilla por la cantidad planificada.
/// </summary>
public class OrdenTrabajoPlantilla
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(255)] public string Nombre { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public bool Activo { get; set; } = true;
    [MaxLength(500)] public string? Observaciones { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrdenTrabajoPlantillaInsumo> Insumos { get; set; } = new List<OrdenTrabajoPlantillaInsumo>();
}

/// <summary>Insumo predefinido por unidad de producto a fabricar (receta/BOM).</summary>
public class OrdenTrabajoPlantillaInsumo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlantillaId { get; set; }
    public OrdenTrabajoPlantilla Plantilla { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    /// <summary>Cantidad de insumo requerida por cada unidad del producto a fabricar.</summary>
    [Column(TypeName = "decimal(12,4)")] public decimal CantidadPorUnidad { get; set; }
}
