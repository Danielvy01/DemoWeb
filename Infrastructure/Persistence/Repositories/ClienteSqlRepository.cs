using Microsoft.Data.SqlClient;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Models;

namespace Siserp.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Adaptador de infraestructura (hexagonal) que implementa <see cref="IClienteRepository"/>
/// invocando los stored procedures sp_Cliente_Registrar / sp_Cliente_Modificar / sp_Cliente_EliminarLogico.
/// </summary>
public class ClienteSqlRepository : IClienteRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ClienteSqlRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Cliente?> RegistrarAsync(Cliente cliente, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Cliente_Registrar", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", cliente.Id);
        command.Parameters.AddWithValue("@EmpresaId", cliente.EmpresaId);
        command.Parameters.AddWithValue("@TipoDocumento", cliente.TipoDocumento.ToString());
        command.Parameters.AddWithValue("@NumeroDocumento", cliente.NumeroDocumento);
        command.Parameters.AddWithValue("@NombreORazonSocial", cliente.NombreORazonSocial);
        command.Parameters.AddWithValue("@Direccion", cliente.Direccion.AsDbValue());
        command.Parameters.AddWithValue("@Email", cliente.Email.AsDbValue());
        command.Parameters.AddWithValue("@Telefono", cliente.Telefono.AsDbValue());
        command.Parameters.AddWithValue("@DiasCredito", cliente.DiasCredito);
        command.Parameters.AddWithValue("@Clasificacion", cliente.Clasificacion.ToString());

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Cliente?> ModificarAsync(Cliente cliente, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Cliente_Modificar", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", cliente.Id);
        command.Parameters.AddWithValue("@EmpresaId", cliente.EmpresaId);
        command.Parameters.AddWithValue("@TipoDocumento", cliente.TipoDocumento.ToString());
        command.Parameters.AddWithValue("@NumeroDocumento", cliente.NumeroDocumento);
        command.Parameters.AddWithValue("@NombreORazonSocial", cliente.NombreORazonSocial);
        command.Parameters.AddWithValue("@Direccion", cliente.Direccion.AsDbValue());
        command.Parameters.AddWithValue("@Email", cliente.Email.AsDbValue());
        command.Parameters.AddWithValue("@Telefono", cliente.Telefono.AsDbValue());
        command.Parameters.AddWithValue("@DiasCredito", cliente.DiasCredito);
        command.Parameters.AddWithValue("@Clasificacion", cliente.Clasificacion.ToString());

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Cliente?> EliminarLogicoAsync(Guid id, Guid empresaId, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Cliente_EliminarLogico", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@EmpresaId", empresaId);

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    private static Cliente Map(SqlDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        EmpresaId = reader.GetGuid(reader.GetOrdinal("EmpresaId")),
        TipoDocumento = Enum.Parse<TipoDocumentoIdentidad>(reader.GetString(reader.GetOrdinal("TipoDocumento"))),
        NumeroDocumento = reader.GetString(reader.GetOrdinal("NumeroDocumento")),
        NombreORazonSocial = reader.GetString(reader.GetOrdinal("NombreORazonSocial")),
        Direccion = reader.GetStringOrNull("Direccion"),
        Email = reader.GetStringOrNull("Email"),
        Telefono = reader.GetStringOrNull("Telefono"),
        DiasCredito = reader.GetInt32(reader.GetOrdinal("DiasCredito")),
        Clasificacion = Enum.Parse<ClasificacionCliente>(reader.GetString(reader.GetOrdinal("Clasificacion"))),
        Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
        Bloqueado = reader.GetBoolean(reader.GetOrdinal("Bloqueado")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
    };
}
