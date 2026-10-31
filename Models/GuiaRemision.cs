using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

/// <summary>
/// Guía de remisión: documento que sustenta el traslado físico de mercadería.
/// Si está vinculada a un <see cref="Comprobante"/> (venta ya facturada) es
/// solo el sustento documentario del traslado (el stock ya se descontó al
/// emitir la venta). Si NO está vinculada a un comprobante (traslado entre
/// almacenes, consignación, envío previo a facturación, etc.) es ella misma
/// quien descuenta el stock del almacén de origen y actualiza el Kardex
/// (método de costeo promedio) al ser emitida.
/// </summary>
public class GuiaRemision
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(20)] public string Numero { get; set; } = null!;
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;

    public Guid AlmacenOrigenId { get; set; }
    public Almacen AlmacenOrigen { get; set; } = null!;
    /// <summary>Almacén de destino, cuando el traslado es entre almacenes propios.</summary>
    public Guid? AlmacenDestinoId { get; set; }
    public Almacen? AlmacenDestino { get; set; }

    /// <summary>Cliente destinatario de la mercadería (opcional para traslados entre almacenes propios).</summary>
    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    /// <summary>Venta que origina el traslado (opcional); si está presente, el stock ya fue descontado por la venta.</summary>
    public Guid? ComprobanteId { get; set; }
    public Comprobante? Comprobante { get; set; }

    [MaxLength(255)] public string? DireccionPartida { get; set; }
    [MaxLength(255)] public string? DireccionLlegada { get; set; }
    [MaxLength(255)] public string? Transportista { get; set; }
    [MaxLength(20)] public string? PlacaVehiculo { get; set; }
    [MaxLength(200)] public string MotivoTraslado { get; set; } = "VENTA";

    public EstadoGuiaRemision Estado { get; set; } = EstadoGuiaRemision.BORRADOR;
    [MaxLength(500)] public string? Observaciones { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GuiaRemisionDetalle> Detalles { get; set; } = new List<GuiaRemisionDetalle>();
}

public class GuiaRemisionDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GuiaRemisionId { get; set; }
    public GuiaRemision GuiaRemision { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [Column(TypeName = "decimal(12,2)")] public decimal Cantidad { get; set; }
}
