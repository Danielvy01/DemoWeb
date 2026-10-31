using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Siserp.Api.Data;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Models;

namespace Siserp.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Adaptador de infraestructura (hexagonal) que implementa <see cref="IOrdenCompraRepository"/>
/// invocando sp_OrdenCompra_Registrar / sp_OrdenCompra_Modificar. Igual que
/// <see cref="ComprasSqlRepository"/>, reutiliza la conexión/transacción del
/// <see cref="SiserpDbContext"/> ya abierta por el controlador en lugar de abrir una conexión
/// propia, de modo que las validaciones resueltas con EF Core (proveedor, comprador,
/// comprobante, cuentas contables) y el alta/edición de la orden se confirman o revierten de
/// forma atómica como una única transacción.
/// </summary>
public class OrdenCompraSqlRepository : IOrdenCompraRepository
{
    private readonly SiserpDbContext _db;

    public OrdenCompraSqlRepository(SiserpDbContext db)
    {
        _db = db;
    }

    public async Task RegistrarAsync(OrdenCompra orden, CancellationToken ct = default)
    {
        var (connection, transaction) = await GetOpenConnectionAsync(ct);
        await using var command = new SqlCommand("dbo.sp_OrdenCompra_Registrar", connection)
        {
            CommandType = CommandType.StoredProcedure,
            Transaction = transaction,
        };
        AddCabeceraParameters(command, orden);
        command.Parameters.Add(BuildDetallesTable(orden.Detalles));

        await command.ExecuteNonQueryAsync(ct);

        // Igual que en ComprasSqlRepository: al escribir por fuera de EF Core (ADO.NET puro)
        // el change tracker no se entera del alta, así que se limpia para forzar una lectura
        // fresca en la consulta posterior que hace el controlador para devolver la orden recién creada.
        _db.ChangeTracker.Clear();
    }

    public async Task<bool> ActualizarAsync(OrdenCompra orden, CancellationToken ct = default)
    {
        var (connection, transaction) = await GetOpenConnectionAsync(ct);
        await using var command = new SqlCommand("dbo.sp_OrdenCompra_Modificar", connection)
        {
            CommandType = CommandType.StoredProcedure,
            Transaction = transaction,
        };
        AddCabeceraParameters(command, orden);
        command.Parameters.Add(BuildDetallesTable(orden.Detalles));
        var encontrada = command.Parameters.Add("@Encontrada", SqlDbType.Bit);
        encontrada.Direction = ParameterDirection.Output;

        await command.ExecuteNonQueryAsync(ct);

        _db.ChangeTracker.Clear();

        return encontrada.Value is bool b && b;
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

    private static void AddCabeceraParameters(SqlCommand command, OrdenCompra orden)
    {
        command.Parameters.AddWithValue("@Id", orden.Id);
        command.Parameters.AddWithValue("@EmpresaId", orden.EmpresaId);
        command.Parameters.AddWithValue("@ProveedorId", orden.ProveedorId);
        command.Parameters.AddWithValue("@EmpleadoCompradorId", orden.EmpleadoCompradorId.AsDbValue());
        command.Parameters.AddWithValue("@ComprobanteId", orden.ComprobanteId.AsDbValue());
        command.Parameters.AddWithValue("@Numero", orden.Numero);
        command.Parameters.AddWithValue("@FechaEmision", orden.FechaEmision);
        command.Parameters.AddWithValue("@TipoEntrega", orden.TipoEntrega.ToString());
        command.Parameters.AddWithValue("@PlazoEntregaDias", orden.PlazoEntregaDias.AsDbValue());
        command.Parameters.AddWithValue("@FechaEntregaEsperada", orden.FechaEntregaEsperada.AsDbValue());
        command.Parameters.AddWithValue("@Moneda", orden.Moneda);
        command.Parameters.AddWithValue("@Total", orden.Total);
        command.Parameters.AddWithValue("@Estado", orden.Estado.ToString());
        command.Parameters.AddWithValue("@Observaciones", orden.Observaciones.AsDbValue());
    }

    /// <summary>Arma el parámetro con valores de tabla (TVP) dbo.OrdenCompraDetalleTableType a partir de los ítems de la orden.</summary>
    private static SqlParameter BuildDetallesTable(IEnumerable<OrdenCompraDetalle> detalles)
    {
        var tabla = new DataTable();
        tabla.Columns.Add("Id", typeof(Guid));
        tabla.Columns.Add("ProductoId", typeof(Guid));
        tabla.Columns.Add("Cantidad", typeof(decimal));
        tabla.Columns.Add("PrecioUnitario", typeof(decimal));
        tabla.Columns.Add("CantidadRecibida", typeof(decimal));
        tabla.Columns.Add("Total", typeof(decimal));
        tabla.Columns.Add("CuentaContableId", typeof(Guid));
        foreach (var detalle in detalles)
        {
            tabla.Rows.Add(detalle.Id, detalle.ProductoId, detalle.Cantidad, detalle.PrecioUnitario, detalle.CantidadRecibida, detalle.Total, (object?)detalle.CuentaContableId ?? DBNull.Value);
        }

        return new SqlParameter("@Detalles", SqlDbType.Structured)
        {
            TypeName = "dbo.OrdenCompraDetalleTableType",
            Value = tabla,
        };
    }
}
