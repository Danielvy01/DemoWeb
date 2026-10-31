using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

/// <summary>
/// Pedido de venta: paso intermedio entre la <see cref="Cotizacion"/> (o una
/// venta directa sin cotización previa) y el <see cref="Comprobante"/>
/// (factura/boleta). Permite comprometer la venta con el cliente (y, a
/// futuro, reservar stock) antes de facturar y descontar inventario.
/// </summary>
public class PedidoVenta
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    [MaxLength(20)] public string Numero { get; set; } = null!;
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    /// <summary>Fecha comprometida de entrega/atención del pedido (opcional).</summary>
    public DateTime? FechaEntrega { get; set; }
    [MaxLength(5)] public string Moneda { get; set; } = "PEN";
    [Column(TypeName = "decimal(12,2)")] public decimal Total { get; set; }
    public EstadoPedidoVenta Estado { get; set; } = EstadoPedidoVenta.BORRADOR;
    [MaxLength(500)] public string? Observaciones { get; set; }
    /// <summary>Cotización de la que se originó el pedido (null si se creó directamente).</summary>
    public Guid? CotizacionId { get; set; }
    public Cotizacion? Cotizacion { get; set; }
    /// <summary>Comprobante (factura/boleta) generado al facturar el pedido (null mientras no se facture).</summary>
    public Guid? ComprobanteId { get; set; }
    public Comprobante? Comprobante { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PedidoVentaDetalle> Detalles { get; set; } = new List<PedidoVentaDetalle>();
}

public class PedidoVentaDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PedidoVentaId { get; set; }
    public PedidoVenta PedidoVenta { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [Column(TypeName = "decimal(12,2)")] public decimal Cantidad { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal PrecioUnitario { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Total { get; set; }
}
