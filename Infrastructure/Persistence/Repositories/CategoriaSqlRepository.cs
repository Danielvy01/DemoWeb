using Microsoft.Data.SqlClient;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Models;

namespace Siserp.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Adaptador de infraestructura (hexagonal) que implementa <see cref="ICategoriaRepository"/>
/// invocando sp_Categoria_Registrar / sp_Categoria_Modificar / sp_Categoria_EliminarLogico.
/// </summary>
public class CategoriaSqlRepository : ICategoriaRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CategoriaSqlRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Categoria?> RegistrarAsync(Categoria categoria, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Categoria_Registrar", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", categoria.Id);
        command.Parameters.AddWithValue("@EmpresaId", categoria.EmpresaId);
        command.Parameters.AddWithValue("@Nombre", categoria.Nombre);

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Categoria?> ModificarAsync(Categoria categoria, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Categoria_Modificar", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", categoria.Id);
        command.Parameters.AddWithValue("@EmpresaId", categoria.EmpresaId);
        command.Parameters.AddWithValue("@Nombre", categoria.Nombre.AsDbValue());

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Categoria?> EliminarLogicoAsync(Guid id, Guid empresaId, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Categoria_EliminarLogico", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@EmpresaId", empresaId);

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    private static Categoria Map(SqlDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        EmpresaId = reader.GetGuid(reader.GetOrdinal("EmpresaId")),
        Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
        Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
    };
}
