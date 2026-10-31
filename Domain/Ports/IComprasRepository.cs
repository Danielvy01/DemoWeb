using Siserp.Api.Models;

namespace Siserp.Api.Domain.Ports;

/// <summary>
/// Puerto de escritura para el registro de <see cref="Compra"/> (documento + ítems)
/// respaldado por stored procedures. A diferencia de los repositorios de las
/// entidades maestras, participa en la transacción EF Core ya abierta por el
/// controlador (<see cref="Controllers.ComprasController"/>), ya que el registro de
/// una compra también ingresa stock/Kardex (<see cref="Services.ProductosService"/>)
/// dentro de la misma unidad de trabajo.
/// </summary>
public interface IComprasRepository
{
    /// <summary>Inserta la cabecera de la compra y reemplaza sus ítems por los indicados en <paramref name="compra"/>.Detalles.</summary>
    Task RegistrarAsync(Compra compra, CancellationToken ct = default);

    /// <summary>
    /// Actualiza la cabecera de la compra y reemplaza por completo sus ítems.
    /// Devuelve <c>false</c> si la compra ya no existe (fue eliminada/modificada por
    /// otra operación concurrente entre la lectura y el guardado).
    /// </summary>
    Task<bool> ActualizarAsync(Compra compra, CancellationToken ct = default);

    /// <summary>
    /// Enlaza (o desenlaza) el asiento contable generado automáticamente para la compra,
    /// una vez contabilizada por <see cref="Services.ContabilidadService"/>.
    /// </summary>
    Task ActualizarAsientoContableAsync(Guid compraId, Guid? asientoContableId, CancellationToken ct = default);

    /// <summary>Marca la compra como anulada (baja lógica), registrando el motivo y la fecha de anulación.</summary>
    Task AnularAsync(Guid compraId, string? motivo, CancellationToken ct = default);
}
