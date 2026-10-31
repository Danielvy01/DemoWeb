using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

/// <summary>
/// Orden de salida de almacén: documento interno que sustenta y ejecuta la
/// salida física de mercadería del almacén por un motivo distinto a la venta
/// (consumo de insumos de una orden de trabajo, merma, traslado interno,
/// consumo interno, etc.). Al confirmarse descuenta el stock y genera el
/// movimiento de Kardex correspondiente (costo valorizado por promedio).
/// </summary>
public class OrdenSalidaAlmacen
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(20)] public string Numero { get; set; } = null!;
    public Guid AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;
    [MaxLength(200)] public string Motivo { get; set; } = null!;
    /// <summary>Orden de trabajo que origina esta salida (consumo de insumos), cuando aplica.</summary>
    public Guid? OrdenTrabajoId { get; set; }
    public OrdenTrabajo? OrdenTrabajo { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    public EstadoOrdenSalidaAlmacen Estado { get; set; } = EstadoOrdenSalidaAlmacen.BORRADOR;
    [MaxLength(500)] public string? Observaciones { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrdenSalidaAlmacenDetalle> Detalles { get; set; } = new List<OrdenSalidaAlmacenDetalle>();
}

public class OrdenSalidaAlmacenDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrdenSalidaAlmacenId { get; set; }
    public OrdenSalidaAlmacen OrdenSalidaAlmacen { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [Column(TypeName = "decimal(12,2)")] public decimal Cantidad { get; set; }
}
