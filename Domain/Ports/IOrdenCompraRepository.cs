using Siserp.Api.Models;

namespace Siserp.Api.Domain.Ports;

/// <summary>
/// Puerto de escritura para el registro de <see cref="OrdenCompra"/> (documento + ítems)
/// respaldado por stored procedures, siguiendo el mismo patrón que
/// <see cref="IComprasRepository"/>: participa en la transacción EF Core ya abierta por el
/// controlador (<see cref="Controllers.OrdenesCompraController"/>) para que las validaciones
/// que siguen resolviéndose con EF Core (proveedor, comprobante, cuentas contables, etc.)
/// y la escritura vía stored procedure se confirmen/reviertan como una única unidad de trabajo.
/// </summary>
public interface IOrdenCompraRepository
{
    /// <summary>Inserta la cabecera de la orden de compra y sus ítems (<paramref name="orden"/>.Detalles).</summary>
    Task RegistrarAsync(OrdenCompra orden, CancellationToken ct = default);

    /// <summary>
    /// Actualiza la cabecera de la orden de compra y reemplaza por completo sus ítems.
    /// Devuelve <c>false</c> si la orden ya no existe (fue eliminada/modificada por otra
    /// operación concurrente entre la lectura y el guardado).
    /// </summary>
    Task<bool> ActualizarAsync(OrdenCompra orden, CancellationToken ct = default);
}
