using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Siserp.Api.Data;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Models;

namespace Siserp.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Adaptador de infraestructura (hexagonal) que implementa <see cref="IComprasRepository"/>
/// invocando sp_Compra_Registrar / sp_Compra_Modificar. A diferencia de los repositorios de
/// las entidades maestras (que abren su propia <see cref="SqlConnection"/> vía
/// <see cref="ISqlConnectionFactory"/>), reutiliza la conexión/transacción del
/// <see cref="SiserpDbContext"/> ya abierta por el controlador, de modo que el alta/edición
/// de la compra y el ajuste de stock/Kardex (que sigue usando EF Core) se confirman o
/// revierten de forma atómica como una única transacción.
/// </summary>
public class ComprasSqlRepository : IComprasRepository
{
    private readonly SiserpDbContext _db;

    public ComprasSqlRepository(SiserpDbContext db)
    {
        _db = db;
    }

    public async Task RegistrarAsync(Compra compra, CancellationToken ct = default)
    {
        var (connection, transaction) = await GetOpenConnectionAsync(ct);
        await using var command = new SqlCommand("dbo.sp_Compra_Registrar", connection)
        {
            CommandType = CommandType.StoredProcedure,
            Transaction = transaction,
        };
        AddCabeceraParameters(command, compra);
        command.Parameters.Add(BuildDetallesTable(compra.Detalles));

        await command.ExecuteNonQueryAsync(ct);

        // Evita que una lectura posterior (p. ej. el Include(...) que hace el controlador
        // para devolver la compra recién creada) devuelva instancias ya rastreadas con
        // valores obsoletos: al escribir por fuera de EF Core (ADO.NET puro) el change
        // tracker no se entera del alta, así que se limpia para forzar una lectura fresca.
        _db.ChangeTracker.Clear();
    }

    public async Task<bool> ActualizarAsync(Compra compra, CancellationToken ct = default)
    {
        var (connection, transaction) = await GetOpenConnectionAsync(ct);
        await using var command = new SqlCommand("dbo.sp_Compra_Modificar", connection)
        {
            CommandType = CommandType.StoredProcedure,
            Transaction = transaction,
        };
        AddCabeceraParameters(command, compra);
        command.Parameters.Add(BuildDetallesTable(compra.Detalles));
        var encontrada = command.Parameters.Add("@Encontrada", SqlDbType.Bit);
        encontrada.Direction = ParameterDirection.Output;

        await command.ExecuteNonQueryAsync(ct);

        // Ídem que en RegistrarAsync: limpia el change tracker para que cualquier
        // consulta posterior en el mismo DbContext obtenga los valores recién escritos
        // por el stored procedure, en vez de instancias ya rastreadas con datos previos.
        _db.ChangeTracker.Clear();

        return encontrada.Value is bool b && b;
    }

    public async Task ActualizarAsientoContableAsync(Guid compraId, Guid? asientoContableId, CancellationToken ct = default)
    {
        var (connection, transaction) = await GetOpenConnectionAsync(ct);
        await using var command = new SqlCommand(
            "UPDATE dbo.Compras SET AsientoContableId = @AsientoContableId WHERE Id = @Id", connection)
        {
            Transaction = transaction,
        };
        command.Parameters.AddWithValue("@Id", compraId);
        command.Parameters.AddWithValue("@AsientoContableId", asientoContableId.AsDbValue());
        await command.ExecuteNonQueryAsync(ct);
        _db.ChangeTracker.Clear();
    }

    public async Task AnularAsync(Guid compraId, string? motivo, CancellationToken ct = default)
    {
        var (connection, transaction) = await GetOpenConnectionAsync(ct);
        await using var command = new SqlCommand(
            "UPDATE dbo.Compras SET Anulado = 1, MotivoAnulacion = @Motivo, FechaAnulacion = SYSUTCDATETIME() WHERE Id = @Id",
            connection)
        {
            Transaction = transaction,
        };
        command.Parameters.AddWithValue("@Id", compraId);
        command.Parameters.AddWithValue("@Motivo", motivo.AsDbValue());
        await command.ExecuteNonQueryAsync(ct);
        _db.ChangeTracker.Clear();
    }

    /// <summary>
    /// Reutiliza la conexión del <see cref="SiserpDbContext"/> (abriéndola si aún no lo
    /// está) y su transacción ambiente, si el controlador ya inició una con
    /// <c>_db.Database.BeginTransactionAsync()</c>.
    /// </summary>
    private async Task<(SqlConnection Connection, SqlTransaction? Transaction)> GetOpenConnectionAsync(CancellationToken ct)
    {
        var connection = (SqlConnection)_db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);
        var transaction = (SqlTransaction?)_db.Database.CurrentTransaction?.GetDbTransaction();
        return (connection, transaction);
    }

    private static void AddCabeceraParameters(SqlCommand command, Compra compra)
    {
        command.Parameters.AddWithValue("@Id", compra.Id);
        command.Parameters.AddWithValue("@EmpresaId", compra.EmpresaId);
        command.Parameters.AddWithValue("@ProveedorId", compra.ProveedorId);
        command.Parameters.AddWithValue("@CentroCostoId", compra.CentroCostoId);
        command.Parameters.AddWithValue("@AlmacenId", compra.AlmacenId);
        command.Parameters.AddWithValue("@OrdenCompraId", compra.OrdenCompraId.AsDbValue());
        command.Parameters.AddWithValue("@EmpleadoCompradorId", compra.EmpleadoCompradorId.AsDbValue());
        command.Parameters.AddWithValue("@Serie", compra.Serie.AsDbValue());
        command.Parameters.AddWithValue("@Numero", compra.Numero.AsDbValue());
        command.Parameters.AddWithValue("@TipoDocumento", compra.TipoDocumento.ToString());
        command.Parameters.AddWithValue("@FechaEmision", compra.FechaEmision);
        command.Parameters.AddWithValue("@NumeroGuiaRemision", compra.NumeroGuiaRemision.AsDbValue());
        command.Parameters.AddWithValue("@Referencia", compra.Referencia.AsDbValue());
        command.Parameters.AddWithValue("@LocalProveedor", compra.LocalProveedor.AsDbValue());
        command.Parameters.AddWithValue("@TipoOperacionSunat", compra.TipoOperacionSunat.ToString());
        command.Parameters.AddWithValue("@Moneda", compra.Moneda);
        command.Parameters.AddWithValue("@TasaIgv", compra.TasaIgv);
        command.Parameters.AddWithValue("@Subtotal", compra.Subtotal);
        command.Parameters.AddWithValue("@Igv", compra.Igv);
        command.Parameters.AddWithValue("@FechaTipoCambio", compra.FechaTipoCambio.AsDbValue());
        command.Parameters.AddWithValue("@TipoCambio", compra.TipoCambio.AsDbValue());
        command.Parameters.AddWithValue("@Total", compra.Total);
        command.Parameters.AddWithValue("@GlosaComprobante", compra.GlosaComprobante.AsDbValue());
        command.Parameters.AddWithValue("@GlosaMovimiento", compra.GlosaMovimiento.AsDbValue());
        command.Parameters.AddWithValue("@FechaVencimiento", compra.FechaVencimiento.AsDbValue());
        command.Parameters.AddWithValue("@ConfiguracionCuentaOperacionId", compra.ConfiguracionCuentaOperacionId.AsDbValue());
    }

    /// <summary>Arma el parámetro con valores de tabla (TVP) dbo.CompraDetalleTableType a partir de los ítems de la compra.</summary>
    private static SqlParameter BuildDetallesTable(IEnumerable<CompraDetalle> detalles)
    {
        var tabla = new DataTable();
        tabla.Columns.Add("Id", typeof(Guid));
        tabla.Columns.Add("ProductoId", typeof(Guid));
        tabla.Columns.Add("Cantidad", typeof(decimal));
        tabla.Columns.Add("PrecioUnitario", typeof(decimal));
        tabla.Columns.Add("PorcentajeDescuento", typeof(decimal));
        tabla.Columns.Add("Total", typeof(decimal));
        tabla.Columns.Add("CuentaContableId", typeof(Guid));
        foreach (var detalle in detalles)
        {
            tabla.Rows.Add(detalle.Id, detalle.ProductoId, detalle.Cantidad, detalle.PrecioUnitario, detalle.PorcentajeDescuento, detalle.Total, (object?)detalle.CuentaContableId ?? DBNull.Value);
        }

        return new SqlParameter("@Detalles", SqlDbType.Structured)
        {
            TypeName = "dbo.CompraDetalleTableType",
            Value = tabla,
        };
    }
}
