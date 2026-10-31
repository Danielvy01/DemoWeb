using Siserp.Api.Models;

namespace Siserp.Api.Domain.Ports;

/// <summary>
/// Puerto de escritura para <see cref="Producto"/> respaldado por stored procedures.
/// Opera solo sobre los campos propios de la tabla Productos; la composición (BOM,
/// tabla ProductoComponente) se gestiona aparte a través de EF Core.
/// </summary>
public interface IProductoRepository
{
    Task<Producto?> RegistrarAsync(Producto producto, CancellationToken ct = default);

    /// <param name="categoriaIdSet">
    /// Indica si <see cref="Producto.CategoriaId"/> debe sobrescribirse aunque venga en null
    /// (para permitir desasignar la categoría); si es false, null significa "no modificar".
    /// </param>
    /// <param name="cuentaVentaIdSet">Igual semántica que <paramref name="categoriaIdSet"/> pero para <see cref="Producto.CuentaVentaId"/>.</param>
    /// <param name="cuentaCostoVentaIdSet">Igual semántica que <paramref name="categoriaIdSet"/> pero para <see cref="Producto.CuentaCostoVentaId"/>.</param>
    /// <param name="cuentaExistenciasIdSet">Igual semántica que <paramref name="categoriaIdSet"/> pero para <see cref="Producto.CuentaExistenciasId"/>.</param>
    /// <param name="codigoBienServicioSet">Igual semántica que <paramref name="categoriaIdSet"/> pero para <see cref="Producto.CodigoBienServicio"/>.</param>
    Task<Producto?> ModificarAsync(
        Producto producto, bool categoriaIdSet, bool cuentaVentaIdSet = false,
        bool cuentaCostoVentaIdSet = false, bool cuentaExistenciasIdSet = false, bool codigoBienServicioSet = false, CancellationToken ct = default);
    Task<Producto?> EliminarLogicoAsync(Guid id, Guid empresaId, CancellationToken ct = default);
}
