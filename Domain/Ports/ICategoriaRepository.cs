using Siserp.Api.Models;

namespace Siserp.Api.Domain.Ports;

/// <summary>Puerto de escritura para <see cref="Categoria"/> respaldado por stored procedures.</summary>
public interface ICategoriaRepository
{
    Task<Categoria?> RegistrarAsync(Categoria categoria, CancellationToken ct = default);
    Task<Categoria?> ModificarAsync(Categoria categoria, CancellationToken ct = default);
    Task<Categoria?> EliminarLogicoAsync(Guid id, Guid empresaId, CancellationToken ct = default);
}
