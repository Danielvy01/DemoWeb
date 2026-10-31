using Microsoft.Data.SqlClient;

namespace Siserp.Api.Infrastructure.Persistence;

/// <summary>
/// Adaptador de infraestructura que crea conexiones ADO.NET hacia SQL Server, usadas por los
/// repositorios de escritura que invocan stored procedures (fuera del DbContext de EF Core).
/// </summary>
public interface ISqlConnectionFactory
{
    SqlConnection Create();
}

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'Default'.");
    }

    public SqlConnection Create() => new(_connectionString);
}
