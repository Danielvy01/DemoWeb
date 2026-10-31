using Microsoft.Data.SqlClient;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Models;

namespace Siserp.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Adaptador de infraestructura (hexagonal) que implementa <see cref="IProveedorRepository"/>
/// invocando sp_Proveedor_Registrar / sp_Proveedor_Modificar / sp_Proveedor_EliminarLogico.
/// </summary>
public class ProveedorSqlRepository : IProveedorRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProveedorSqlRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Proveedor?> RegistrarAsync(Proveedor proveedor, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Proveedor_Registrar", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", proveedor.Id);
        command.Parameters.AddWithValue("@EmpresaId", proveedor.EmpresaId);
        command.Parameters.AddWithValue("@Ruc", proveedor.Ruc);
        command.Parameters.AddWithValue("@RazonSocial", proveedor.RazonSocial);
        command.Parameters.AddWithValue("@Direccion", proveedor.Direccion.AsDbValue());
        command.Parameters.AddWithValue("@Email", proveedor.Email.AsDbValue());
        command.Parameters.AddWithValue("@Telefono", proveedor.Telefono.AsDbValue());
        command.Parameters.AddWithValue("@DiasCredito", proveedor.DiasCredito);

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Proveedor?> ModificarAsync(Proveedor proveedor, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Proveedor_Modificar", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", proveedor.Id);
        command.Parameters.AddWithValue("@EmpresaId", proveedor.EmpresaId);
        command.Parameters.AddWithValue("@Ruc", proveedor.Ruc);
        command.Parameters.AddWithValue("@RazonSocial", proveedor.RazonSocial);
        command.Parameters.AddWithValue("@Direccion", proveedor.Direccion.AsDbValue());
        command.Parameters.AddWithValue("@Email", proveedor.Email.AsDbValue());
        command.Parameters.AddWithValue("@Telefono", proveedor.Telefono.AsDbValue());
        command.Parameters.AddWithValue("@DiasCredito", proveedor.DiasCredito);

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Proveedor?> EliminarLogicoAsync(Guid id, Guid empresaId, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Proveedor_EliminarLogico", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@EmpresaId", empresaId);

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    private static Proveedor Map(SqlDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        EmpresaId = reader.GetGuid(reader.GetOrdinal("EmpresaId")),
        Ruc = reader.GetString(reader.GetOrdinal("Ruc")),
        RazonSocial = reader.GetString(reader.GetOrdinal("RazonSocial")),
        Direccion = reader.GetStringOrNull("Direccion"),
        Email = reader.GetStringOrNull("Email"),
        Telefono = reader.GetStringOrNull("Telefono"),
        DiasCredito = reader.GetInt32(reader.GetOrdinal("DiasCredito")),
        Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
        Bloqueado = reader.GetBoolean(reader.GetOrdinal("Bloqueado")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
    };
}
