using Siserp.Api.Models;

namespace Siserp.Api.Domain.Ports;

/// <summary>
/// Puerto (arquitectura hexagonal) para las operaciones de escritura de <see cref="Cliente"/>.
/// El adaptador de infraestructura las implementa invocando stored procedures.
/// </summary>
public interface IClienteRepository
{
    Task<Cliente?> RegistrarAsync(Cliente cliente, CancellationToken ct = default);
    Task<Cliente?> ModificarAsync(Cliente cliente, CancellationToken ct = default);
    Task<Cliente?> EliminarLogicoAsync(Guid id, Guid empresaId, CancellationToken ct = default);
}
