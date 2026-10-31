using Siserp.Api.Models;

namespace Siserp.Api.Domain.Ports;

/// <summary>Puerto de escritura para <see cref="CentroCosto"/> respaldado por stored procedures.</summary>
public interface ICentroCostoRepository
{
    Task<CentroCosto?> RegistrarAsync(CentroCosto centroCosto, CancellationToken ct = default);
    Task<CentroCosto?> ModificarAsync(CentroCosto centroCosto, CancellationToken ct = default);
    Task<CentroCosto?> EliminarLogicoAsync(Guid id, Guid empresaId, CancellationToken ct = default);
}
