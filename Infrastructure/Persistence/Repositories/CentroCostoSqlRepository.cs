using Microsoft.Data.SqlClient;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Models;

namespace Siserp.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Adaptador de infraestructura (hexagonal) que implementa <see cref="ICentroCostoRepository"/>
/// invocando sp_CentroCosto_Registrar / sp_CentroCosto_Modificar / sp_CentroCosto_EliminarLogico.
/// </summary>
public class CentroCostoSqlRepository : ICentroCostoRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CentroCostoSqlRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CentroCosto?> RegistrarAsync(CentroCosto centroCosto, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_CentroCosto_Registrar", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", centroCosto.Id);
        command.Parameters.AddWithValue("@EmpresaId", centroCosto.EmpresaId);
        command.Parameters.AddWithValue("@Nombre", centroCosto.Nombre);
        command.Parameters.AddWithValue("@Descripcion", centroCosto.Descripcion.AsDbValue());

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<CentroCosto?> ModificarAsync(CentroCosto centroCosto, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_CentroCosto_Modificar", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", centroCosto.Id);
        command.Parameters.AddWithValue("@EmpresaId", centroCosto.EmpresaId);
        command.Parameters.AddWithValue("@Nombre", centroCosto.Nombre.AsDbValue());
        command.Parameters.AddWithValue("@Descripcion", centroCosto.Descripcion.AsDbValue());

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<CentroCosto?> EliminarLogicoAsync(Guid id, Guid empresaId, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_CentroCosto_EliminarLogico", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@EmpresaId", empresaId);

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    private static CentroCosto Map(SqlDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        EmpresaId = reader.GetGuid(reader.GetOrdinal("EmpresaId")),
        Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
        Descripcion = reader.GetStringOrNull("Descripcion"),
        Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
    };
}
