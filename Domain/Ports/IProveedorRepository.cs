using Siserp.Api.Models;

namespace Siserp.Api.Domain.Ports;

/// <summary>Puerto de escritura para <see cref="Proveedor"/> respaldado por stored procedures.</summary>
public interface IProveedorRepository
{
    Task<Proveedor?> RegistrarAsync(Proveedor proveedor, CancellationToken ct = default);
    Task<Proveedor?> ModificarAsync(Proveedor proveedor, CancellationToken ct = default);
    Task<Proveedor?> EliminarLogicoAsync(Guid id, Guid empresaId, CancellationToken ct = default);
}
