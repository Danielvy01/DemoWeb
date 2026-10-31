using Microsoft.Data.SqlClient;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Models;

namespace Siserp.Api.Infrastructure.Persistence.Repositories;

/// <summary>
/// Adaptador de infraestructura (hexagonal) que implementa <see cref="IProductoRepository"/>
/// invocando sp_Producto_Registrar / sp_Producto_Modificar / sp_Producto_EliminarLogico.
/// </summary>
public class ProductoSqlRepository : IProductoRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ProductoSqlRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Producto?> RegistrarAsync(Producto producto, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Producto_Registrar", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", producto.Id);
        command.Parameters.AddWithValue("@EmpresaId", producto.EmpresaId);
        command.Parameters.AddWithValue("@CategoriaId", producto.CategoriaId.AsDbValue());
        command.Parameters.AddWithValue("@Codigo", producto.Codigo);
        command.Parameters.AddWithValue("@CodigoAlternativo", producto.CodigoAlternativo.AsDbValue());
        command.Parameters.AddWithValue("@Descripcion", producto.Descripcion);
        command.Parameters.AddWithValue("@DescripcionAlternativa", producto.DescripcionAlternativa.AsDbValue());
        command.Parameters.AddWithValue("@UnidadMedida", producto.UnidadMedida);
        command.Parameters.AddWithValue("@PrecioVenta", producto.PrecioVenta);
        command.Parameters.AddWithValue("@PrecioLista", producto.PrecioLista.AsDbValue());
        command.Parameters.AddWithValue("@PrecioCosto", producto.PrecioCosto);
        command.Parameters.AddWithValue("@MonedaCompra", producto.MonedaCompra);
        command.Parameters.AddWithValue("@AfectoIGV", producto.AfectoIGV);
        command.Parameters.AddWithValue("@TipoExistencia", producto.TipoExistencia);
        command.Parameters.AddWithValue("@EsServicio", producto.EsServicio);
        command.Parameters.AddWithValue("@StockMinimo", producto.StockMinimo);
        command.Parameters.AddWithValue("@StockMaximo", producto.StockMaximo);
        command.Parameters.AddWithValue("@DescuentoMayoristaPct", producto.DescuentoMayoristaPct);
        command.Parameters.AddWithValue("@DescuentoSuperMayoristaPct", producto.DescuentoSuperMayoristaPct);
        command.Parameters.AddWithValue("@Observaciones", producto.Observaciones.AsDbValue());
        command.Parameters.AddWithValue("@CuentaVentaId", producto.CuentaVentaId.AsDbValue());
        command.Parameters.AddWithValue("@CuentaCostoVentaId", producto.CuentaCostoVentaId.AsDbValue());
        command.Parameters.AddWithValue("@CuentaExistenciasId", producto.CuentaExistenciasId.AsDbValue());
        command.Parameters.AddWithValue("@CodigoBienServicio", producto.CodigoBienServicio.AsDbValue());

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Producto?> ModificarAsync(
        Producto producto, bool categoriaIdSet, bool cuentaVentaIdSet = false,
        bool cuentaCostoVentaIdSet = false, bool cuentaExistenciasIdSet = false, bool codigoBienServicioSet = false, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Producto_Modificar", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", producto.Id);
        command.Parameters.AddWithValue("@EmpresaId", producto.EmpresaId);
        command.Parameters.AddWithValue("@CategoriaId", producto.CategoriaId.AsDbValue());
        command.Parameters.AddWithValue("@Codigo", producto.Codigo.AsDbValue());
        command.Parameters.AddWithValue("@CodigoAlternativo", producto.CodigoAlternativo.AsDbValue());
        command.Parameters.AddWithValue("@Descripcion", producto.Descripcion.AsDbValue());
        command.Parameters.AddWithValue("@DescripcionAlternativa", producto.DescripcionAlternativa.AsDbValue());
        command.Parameters.AddWithValue("@UnidadMedida", producto.UnidadMedida.AsDbValue());
        command.Parameters.AddWithValue("@PrecioVenta", producto.PrecioVenta);
        command.Parameters.AddWithValue("@PrecioLista", producto.PrecioLista.AsDbValue());
        command.Parameters.AddWithValue("@PrecioCosto", producto.PrecioCosto);
        command.Parameters.AddWithValue("@MonedaCompra", producto.MonedaCompra.AsDbValue());
        command.Parameters.AddWithValue("@AfectoIGV", producto.AfectoIGV);
        command.Parameters.AddWithValue("@TipoExistencia", producto.TipoExistencia.AsDbValue());
        command.Parameters.AddWithValue("@EsServicio", producto.EsServicio);
        command.Parameters.AddWithValue("@StockMinimo", producto.StockMinimo);
        command.Parameters.AddWithValue("@StockMaximo", producto.StockMaximo);
        command.Parameters.AddWithValue("@DescuentoMayoristaPct", producto.DescuentoMayoristaPct);
        command.Parameters.AddWithValue("@DescuentoSuperMayoristaPct", producto.DescuentoSuperMayoristaPct);
        command.Parameters.AddWithValue("@Observaciones", producto.Observaciones.AsDbValue());
        command.Parameters.AddWithValue("@CategoriaIdSet", categoriaIdSet);
        command.Parameters.AddWithValue("@CuentaVentaId", producto.CuentaVentaId.AsDbValue());
        command.Parameters.AddWithValue("@CuentaVentaIdSet", cuentaVentaIdSet);
        command.Parameters.AddWithValue("@CuentaCostoVentaId", producto.CuentaCostoVentaId.AsDbValue());
        command.Parameters.AddWithValue("@CuentaCostoVentaIdSet", cuentaCostoVentaIdSet);
        command.Parameters.AddWithValue("@CuentaExistenciasId", producto.CuentaExistenciasId.AsDbValue());
        command.Parameters.AddWithValue("@CuentaExistenciasIdSet", cuentaExistenciasIdSet);
        command.Parameters.AddWithValue("@CodigoBienServicio", producto.CodigoBienServicio.AsDbValue());
        command.Parameters.AddWithValue("@CodigoBienServicioSet", codigoBienServicioSet);

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Producto?> EliminarLogicoAsync(Guid id, Guid empresaId, CancellationToken ct = default)
    {
        await using var connection = _connectionFactory.Create();
        await using var command = new SqlCommand("sp_Producto_EliminarLogico", connection) { CommandType = System.Data.CommandType.StoredProcedure };
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@EmpresaId", empresaId);

        await connection.OpenAsync(ct);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    private static Producto Map(SqlDataReader reader) => new()
    {
        Id = reader.GetGuid(reader.GetOrdinal("Id")),
        EmpresaId = reader.GetGuid(reader.GetOrdinal("EmpresaId")),
        CategoriaId = reader.GetGuidOrNull("CategoriaId"),
        Codigo = reader.GetString(reader.GetOrdinal("Codigo")),
        CodigoAlternativo = reader.GetStringOrNull("CodigoAlternativo"),
        Descripcion = reader.GetString(reader.GetOrdinal("Descripcion")),
        DescripcionAlternativa = reader.GetStringOrNull("DescripcionAlternativa"),
        UnidadMedida = reader.GetString(reader.GetOrdinal("UnidadMedida")),
        PrecioVenta = reader.GetDecimal(reader.GetOrdinal("PrecioVenta")),
        PrecioLista = reader.GetDecimalOrNull("PrecioLista"),
        PrecioCosto = reader.GetDecimal(reader.GetOrdinal("PrecioCosto")),
        MonedaCompra = reader.GetString(reader.GetOrdinal("MonedaCompra")),
        AfectoIGV = reader.GetBoolean(reader.GetOrdinal("AfectoIGV")),
        TipoExistencia = reader.GetString(reader.GetOrdinal("TipoExistencia")),
        EsServicio = reader.GetBoolean(reader.GetOrdinal("EsServicio")),
        StockMinimo = reader.GetDecimal(reader.GetOrdinal("StockMinimo")),
        StockMaximo = reader.GetDecimal(reader.GetOrdinal("StockMaximo")),
        DescuentoMayoristaPct = reader.GetDecimal(reader.GetOrdinal("DescuentoMayoristaPct")),
        DescuentoSuperMayoristaPct = reader.GetDecimal(reader.GetOrdinal("DescuentoSuperMayoristaPct")),
        FotoUrl = reader.GetStringOrNull("FotoUrl"),
        Observaciones = reader.GetStringOrNull("Observaciones"),
        CuentaVentaId = reader.GetGuidOrNull("CuentaVentaId"),
        CuentaCostoVentaId = reader.GetGuidOrNull("CuentaCostoVentaId"),
        CuentaExistenciasId = reader.GetGuidOrNull("CuentaExistenciasId"),
        CodigoBienServicio = reader.GetStringOrNull("CodigoBienServicio"),
        Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
        UltimaAlertaStockBajo = reader.GetDateTimeOrNull("UltimaAlertaStockBajo"),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
    };
}
