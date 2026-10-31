using Microsoft.Data.SqlClient;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Models;

namespace Siserp.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Adaptador de infraestructura (hexagonal) que implementa <see cref="IEmpleadoRepository"/>
/// invocando sp_Empleado_Registrar / sp_Empleado_Modificar / sp_Empleado_EliminarLogico.
/// </summary>
public class EmpleadoSqlRepository : IEmpleadoRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public EmpleadoSqlRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Empleado?> RegistrarAsync(Empleado empleado, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Empleado_Registrar", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", empleado.Id);
        command.Parameters.AddWithValue("@EmpresaId", empleado.EmpresaId);
        command.Parameters.AddWithValue("@NumeroDocumento", empleado.NumeroDocumento);
        command.Parameters.AddWithValue("@Nombres", empleado.Nombres);
        command.Parameters.AddWithValue("@Apellidos", empleado.Apellidos);
        command.Parameters.AddWithValue("@CentroCostoId", empleado.CentroCostoId);
        command.Parameters.AddWithValue("@Cargo", empleado.Cargo);
        command.Parameters.AddWithValue("@EsComprador", empleado.EsComprador);
        command.Parameters.AddWithValue("@SalarioBase", empleado.SalarioBase);
        command.Parameters.AddWithValue("@Email", empleado.Email.AsDbValue());
        command.Parameters.AddWithValue("@Telefono", empleado.Telefono.AsDbValue());
        command.Parameters.AddWithValue("@CuentaBancaria", empleado.CuentaBancaria.AsDbValue());
        command.Parameters.AddWithValue("@BancoNombre", empleado.BancoNombre.AsDbValue());
        command.Parameters.AddWithValue("@CCI", empleado.CCI.AsDbValue());
        command.Parameters.AddWithValue("@FechaIngreso", empleado.FechaIngreso);
        command.Parameters.AddWithValue("@TipoContrato", empleado.TipoContrato.AsDbValue());
        command.Parameters.AddWithValue("@RegimenPensionario", empleado.RegimenPensionario.AsDbValue());

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Empleado?> ModificarAsync(Empleado empleado, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Empleado_Modificar", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", empleado.Id);
        command.Parameters.AddWithValue("@EmpresaId", empleado.EmpresaId);
        command.Parameters.AddWithValue("@CentroCostoId", empleado.CentroCostoId.AsDbValue());
        command.Parameters.AddWithValue("@Cargo", empleado.Cargo.AsDbValue());
        command.Parameters.AddWithValue("@EsComprador", empleado.EsComprador);
        command.Parameters.AddWithValue("@SalarioBase", empleado.SalarioBase);
        command.Parameters.AddWithValue("@Email", empleado.Email.AsDbValue());
        command.Parameters.AddWithValue("@Telefono", empleado.Telefono.AsDbValue());
        command.Parameters.AddWithValue("@CuentaBancaria", empleado.CuentaBancaria.AsDbValue());
        command.Parameters.AddWithValue("@BancoNombre", empleado.BancoNombre.AsDbValue());
        command.Parameters.AddWithValue("@CCI", empleado.CCI.AsDbValue());
        command.Parameters.AddWithValue("@Activo", empleado.Activo);
        command.Parameters.AddWithValue("@TipoContrato", empleado.TipoContrato.AsDbValue());
        command.Parameters.AddWithValue("@RegimenPensionario", empleado.RegimenPensionario.AsDbValue());

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Empleado?> EliminarLogicoAsync(Guid id, Guid empresaId, string? motivo, DateTime? fecha, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Empleado_EliminarLogico", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@EmpresaId", empresaId);
        command.Parameters.AddWithValue("@Motivo", motivo.AsDbValue());
        command.Parameters.AddWithValue("@Fecha", fecha.AsDbValue());

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    private static Empleado Map(SqlDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        EmpresaId = reader.GetGuid(reader.GetOrdinal("EmpresaId")),
        NumeroDocumento = reader.GetString(reader.GetOrdinal("NumeroDocumento")),
        Nombres = reader.GetString(reader.GetOrdinal("Nombres")),
        Apellidos = reader.GetString(reader.GetOrdinal("Apellidos")),
        CentroCostoId = reader.GetGuid(reader.GetOrdinal("CentroCostoId")),
        Cargo = reader.GetString(reader.GetOrdinal("Cargo")),
        EsComprador = reader.GetBoolean(reader.GetOrdinal("EsComprador")),
        SalarioBase = reader.GetDecimal(reader.GetOrdinal("SalarioBase")),
        Email = reader.GetStringOrNull("Email"),
        Telefono = reader.GetStringOrNull("Telefono"),
        CuentaBancaria = reader.GetStringOrNull("CuentaBancaria"),
        BancoNombre = reader.GetStringOrNull("BancoNombre"),
        CCI = reader.GetStringOrNull("CCI"),
        TipoContrato = reader.GetStringOrNull("TipoContrato"),
        RegimenPensionario = reader.GetStringOrNull("RegimenPensionario"),
        FechaIngreso = reader.GetDateTime(reader.GetOrdinal("FechaIngreso")),
        Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
        FechaCese = reader.GetDateTimeOrNull("FechaCese"),
        MotivoCese = reader.GetStringOrNull("MotivoCese"),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
    };
}
