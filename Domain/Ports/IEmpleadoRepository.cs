using Siserp.Api.Models;

namespace Siserp.Api.Domain.Ports;

/// <summary>Puerto de escritura para <see cref="Empleado"/> respaldado por stored procedures.</summary>
public interface IEmpleadoRepository
{
    Task<Empleado?> RegistrarAsync(Empleado empleado, CancellationToken ct = default);
    Task<Empleado?> ModificarAsync(Empleado empleado, CancellationToken ct = default);
    Task<Empleado?> EliminarLogicoAsync(Guid id, Guid empresaId, string? motivo, DateTime? fecha, CancellationToken ct = default);
}
