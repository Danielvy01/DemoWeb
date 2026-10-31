using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

/// <summary>
/// Cotización: propuesta comercial enviada al cliente antes de concretar la
/// venta. No afecta stock ni contabilidad; al ser aprobada por el cliente se
/// puede convertir en un <see cref="Comprobante"/> (venta) sin volver a
/// digitar los ítems.
/// </summary>
public class Cotizacion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    [MaxLength(20)] public string Numero { get; set; } = null!;
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    /// <summary>Fecha límite de vigencia de la cotización (a partir de la cual se considera vencida).</summary>
    public DateTime? FechaValidez { get; set; }
    [MaxLength(5)] public string Moneda { get; set; } = "PEN";
    [Column(TypeName = "decimal(12,2)")] public decimal Total { get; set; }
    public EstadoCotizacion Estado { get; set; } = EstadoCotizacion.BORRADOR;
    [MaxLength(500)] public string? Observaciones { get; set; }
    /// <summary>Venta generada al convertir la cotización directamente (null mientras no se convierta).</summary>
    public Guid? ComprobanteId { get; set; }
    public Comprobante? Comprobante { get; set; }
    /// <summary>Pedido de venta generado al convertir la cotización aprobada (flujo cotización → pedido → factura).</summary>
    public Guid? PedidoVentaId { get; set; }
    public PedidoVenta? PedidoVenta { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CotizacionDetalle> Detalles { get; set; } = new List<CotizacionDetalle>();
}

public class CotizacionDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CotizacionId { get; set; }
    public Cotizacion Cotizacion { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [Column(TypeName = "decimal(12,2)")] public decimal Cantidad { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal PrecioUnitario { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Total { get; set; }
}
