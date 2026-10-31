using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

/// <summary>
/// Orden de compra: documento que se emite y envía al proveedor para SOLICITAR
/// la compra. Cuando la mercadería llega se registra la recepción generando un
/// registro en <see cref="Compra"/> (que sí ingresa stock).
/// </summary>
public class OrdenCompra
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Guid ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;
    /// <summary>Personal con rol de Comprador responsable de la orden.</summary>
    public Guid? EmpleadoCompradorId { get; set; }
    public Empleado? EmpleadoComprador { get; set; }
    /// <summary>Venta (comprobante) a la que atiende esta orden de compra, si se emitió para abastecer un pedido puntual. Puede ser null cuando la compra es para reponer stock en general.</summary>
    public Guid? ComprobanteId { get; set; }
    public Comprobante? Comprobante { get; set; }
    [MaxLength(20)] public string Numero { get; set; } = null!;
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    /// <summary>Modalidad de entrega pactada: inmediata o a un plazo en días.</summary>
    public TipoEntregaOrdenCompra TipoEntrega { get; set; } = TipoEntregaOrdenCompra.INMEDIATA;
    /// <summary>Plazo de entrega en días, solo aplica cuando <see cref="TipoEntrega"/> es A_DIAS.</summary>
    public int? PlazoEntregaDias { get; set; }
    /// <summary>Fecha estimada de entrega; se calcula a partir de <see cref="TipoEntrega"/>/<see cref="PlazoEntregaDias"/> o se indica manualmente.</summary>
    public DateTime? FechaEntregaEsperada { get; set; }
    [MaxLength(5)] public string Moneda { get; set; } = "PEN";
    [Column(TypeName = "decimal(12,2)")] public decimal Total { get; set; }
    public EstadoOrdenCompra Estado { get; set; } = EstadoOrdenCompra.BORRADOR;
    [MaxLength(500)] public string? Observaciones { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrdenCompraDetalle> Detalles { get; set; } = new List<OrdenCompraDetalle>();
}

public class OrdenCompraDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrdenCompraId { get; set; }
    public OrdenCompra OrdenCompra { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [Column(TypeName = "decimal(12,2)")] public decimal Cantidad { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal PrecioUnitario { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal CantidadRecibida { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Total { get; set; }
    /// <summary>
    /// Cuenta contable (del plan de cuentas) a la que se imputa contablemente este ítem
    /// de la compra, según su naturaleza (p. ej. "Insumos/Mercaderías" -&gt; Existencias,
    /// "Servicios" -&gt; Servicios Prestados por Terceros, "Equipos Diversos" -&gt; Activo Fijo,
    /// etc.). Opcional: si no se indica, se contabiliza según la cuenta por defecto del producto.
    /// </summary>
    public Guid? CuentaContableId { get; set; }
    public CuentaContable? CuentaContable { get; set; }
}
