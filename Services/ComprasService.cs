using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

/// <summary>
/// Se lanza al editar una compra cuyo registro ya no existe (fue eliminado/modificado
/// por otra operación concurrente entre la lectura del formulario y el guardado).
/// Equivalente, para el adaptador basado en stored procedures, a lo que antes reportaba
/// EF Core como <see cref="DbUpdateConcurrencyException"/>.
/// </summary>
public class CompraNoEncontradaException : Exception
{
    public Guid CompraId { get; }

    public CompraNoEncontradaException(Guid compraId)
        : base($"La compra {compraId} ya no existe o fue modificada por otra operación.")
    {
        CompraId = compraId;
    }
}

/// <summary>
/// Lógica compartida para registrar una compra: crea el documento (y la cuenta por
/// pagar según los días de crédito del proveedor) e ingresa el stock/Kardex de cada
/// línea. La usan tanto el registro directo de una compra independiente
/// (<see cref="Controllers.ComprasController"/>) como la recepción de una orden de
/// compra (<see cref="Controllers.OrdenesCompraController"/>), de modo que el stock
/// se ajusta una única vez sin importar el origen.
///
/// El alta y la edición de la cabecera + ítems de la compra (<see cref="IComprasRepository"/>)
/// se realizan mediante stored procedures en vez de EF Core <c>SaveChanges</c>, siguiendo
/// el mismo patrón hexagonal usado para las entidades maestras; el ajuste de stock/Kardex
/// sigue sobre EF Core y participa de la misma transacción.
/// </summary>
public class ComprasService
{
    private readonly SiserpDbContext _db;
    private readonly ProductosService _productosService;
    private readonly IComprasRepository _comprasRepository;
    private readonly ContabilidadService _contabilidadService;

    public ComprasService(SiserpDbContext db, ProductosService productosService, IComprasRepository comprasRepository, ContabilidadService contabilidadService)
    {
        _db = db;
        _productosService = productosService;
        _comprasRepository = comprasRepository;
        _contabilidadService = contabilidadService;
    }

    public record DetalleCompra(Guid ProductoId, decimal Cantidad, decimal PrecioUnitario, decimal PorcentajeDescuento = 0, Guid? CuentaContableId = null);

    /// <summary>Datos del comprobante (catálogos SUNAT 01/51) recibido del proveedor para esta compra.</summary>
    public record DatosDocumentoCompra(
        TipoDocumentoCompra TipoDocumento, string? Serie, string Numero, DateTime FechaDocumento,
        DateTime? FechaVencimiento, string? NumeroGuiaRemision, string? Referencia, string? LocalProveedor,
        TipoOperacionSunat TipoOperacionSunat, string? GlosaComprobante = null);

    public async Task<Compra> RegistrarCompraAsync(
        Guid empresaId, Proveedor proveedor, Guid almacenId, Guid centroCostoId, string moneda,
        List<DetalleCompra> detalles, Guid ordenCompraId, DatosDocumentoCompra documento,
        decimal? tasaIgv = null, DateTime? fechaTipoCambio = null, string? glosaMovimiento = null,
        Guid? empleadoCompradorId = null, Guid? configuracionCuentaOperacionId = null)
    {
        var total = Math.Round(detalles.Sum(d => d.Cantidad * d.PrecioUnitario * (1 - d.PorcentajeDescuento / 100m)), 2);
        // Tasa IGV aplicada a la compra; con ella se desagrega el Importe de los ítems en Subtotal + IGV.
        var tasa = tasaIgv ?? 18m;
        var subtotal = Math.Round(total / (1 + tasa / 100m), 2);
        var igv = Math.Round(total - subtotal, 2);

        // Fecha del tipo de cambio: la indicada explícitamente o, por defecto, el día de hoy
        // (solo aplica cuando la compra no está en soles). Se busca el tipo de cambio vigente
        // a esa fecha (el más reciente registrado hasta esa fecha).
        DateTime? fechaCambio = moneda == "PEN" ? null : (fechaTipoCambio?.Date ?? DateTime.UtcNow.Date);
        var tipoCambio = fechaCambio is null
            ? null
            : await _db.TiposCambio
                .Where(t => t.EmpresaId == empresaId && t.Fecha <= fechaCambio)
                .OrderByDescending(t => t.Fecha)
                .FirstOrDefaultAsync();

        var compra = new Compra
        {
            EmpresaId = empresaId,
            ProveedorId = proveedor.Id,
            CentroCostoId = centroCostoId,
            AlmacenId = almacenId,
            OrdenCompraId = ordenCompraId,
            EmpleadoCompradorId = empleadoCompradorId,
            Serie = documento.Serie,
            Numero = documento.Numero,
            TipoDocumento = documento.TipoDocumento,
            FechaEmision = documento.FechaDocumento,
            NumeroGuiaRemision = documento.NumeroGuiaRemision,
            Referencia = documento.Referencia,
            LocalProveedor = documento.LocalProveedor,
            TipoOperacionSunat = documento.TipoOperacionSunat,
            Moneda = moneda,
            TasaIgv = tasa,
            Subtotal = subtotal,
            Igv = igv,
            FechaTipoCambio = fechaCambio,
            TipoCambio = tipoCambio?.Venta,
            Total = total,
            GlosaComprobante = documento.GlosaComprobante,
            GlosaMovimiento = glosaMovimiento,
            ConfiguracionCuentaOperacionId = configuracionCuentaOperacionId,
            // Fecha de vencimiento indicada en el documento o, en su defecto, calculada con los días de crédito del proveedor.
            FechaVencimiento = documento.FechaVencimiento
                ?? (proveedor.DiasCredito > 0 ? documento.FechaDocumento.AddDays(proveedor.DiasCredito) : null),
            Detalles = detalles.Select(d => new CompraDetalle
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                PorcentajeDescuento = d.PorcentajeDescuento,
                Total = Math.Round(d.Cantidad * d.PrecioUnitario * (1 - d.PorcentajeDescuento / 100m), 2),
                CuentaContableId = d.CuentaContableId,
            }).ToList(),
        };
        await _comprasRepository.RegistrarAsync(compra);

        await IngresarStockDetallesAsync(detalles, almacenId, glosaMovimiento, compra.Id);

        // Contabilización automática de la compra (al estilo CONCAR): genera el asiento del
        // Libro Diario imputando cada ítem a su cuenta de destino contra Cuentas por
        // pagar/Caja-Bancos. Si el plan de cuentas no está configurado, no se genera el
        // asiento pero la compra se registra igualmente (no bloquea la operación).
        var asiento = await _contabilidadService.RegistrarAsientoCompraAsync(compra);
        if (asiento is not null)
        {
            compra.AsientoContableId = asiento.Id;
            await _comprasRepository.ActualizarAsientoContableAsync(compra.Id, asiento.Id);
        }

        return compra;
    }

    /// <summary>
    /// Edita una compra ya registrada: solo se permite mientras no esté pagada, ya que una
    /// vez cancelada al proveedor la cuenta por pagar y el Kardex se consideran definitivos.
    /// Revierte el stock/Kardex previamente ingresado (con los datos/almacén originales) y
    /// vuelve a ingresarlo con los datos nuevos, reemplazando además la lista de ítems.
    /// </summary>
    public async Task<Compra> ActualizarCompraAsync(
        Compra compra, Guid almacenId, Guid centroCostoId, string moneda, List<DetalleCompra> detalles,
        DatosDocumentoCompra documento, int diasCreditoProveedor,
        decimal? tasaIgv = null, DateTime? fechaTipoCambio = null, string? glosaMovimiento = null,
        Guid? empleadoCompradorId = null, Guid? configuracionCuentaOperacionId = null)
    {
        // Revierte el stock/Kardex ingresado originalmente (en el almacén y con las cantidades previas)
        // antes de aplicar los cambios, para no duplicar ni perder el ajuste de inventario.
        await RevertirStockDetallesAsync(compra, compra.AlmacenId);

        var total = Math.Round(detalles.Sum(d => d.Cantidad * d.PrecioUnitario * (1 - d.PorcentajeDescuento / 100m)), 2);
        var tasa = tasaIgv ?? 18m;
        var subtotal = Math.Round(total / (1 + tasa / 100m), 2);
        var igv = Math.Round(total - subtotal, 2);

        DateTime? fechaCambio = moneda == "PEN" ? null : (fechaTipoCambio?.Date ?? DateTime.UtcNow.Date);
        var tipoCambio = fechaCambio is null
            ? null
            : await _db.TiposCambio
                .Where(t => t.EmpresaId == compra.EmpresaId && t.Fecha <= fechaCambio)
                .OrderByDescending(t => t.Fecha)
                .FirstOrDefaultAsync();

        // Se arma un objeto nuevo (no rastreado por EF Core) con los datos actualizados en
        // vez de mutar la entidad `compra` que EF ya tiene en seguimiento: la cabecera y los
        // ítems se persisten mediante sp_Compra_Modificar, no con SaveChanges, por lo que no
        // deben quedar cambios pendientes en el change tracker que un SaveChanges posterior
        // (p. ej. el del ajuste de stock) termine aplicando por duplicado.
        var actualizada = new Compra
        {
            Id = compra.Id,
            EmpresaId = compra.EmpresaId,
            ProveedorId = compra.ProveedorId,
            CentroCostoId = centroCostoId,
            AlmacenId = almacenId,
            OrdenCompraId = compra.OrdenCompraId,
            EmpleadoCompradorId = empleadoCompradorId,
            Serie = documento.Serie,
            Numero = documento.Numero,
            TipoDocumento = documento.TipoDocumento,
            FechaEmision = documento.FechaDocumento,
            NumeroGuiaRemision = documento.NumeroGuiaRemision,
            Referencia = documento.Referencia,
            LocalProveedor = documento.LocalProveedor,
            TipoOperacionSunat = documento.TipoOperacionSunat,
            Moneda = moneda,
            TasaIgv = tasa,
            Subtotal = subtotal,
            Igv = igv,
            FechaTipoCambio = fechaCambio,
            TipoCambio = tipoCambio?.Venta,
            Total = total,
            GlosaComprobante = documento.GlosaComprobante,
            GlosaMovimiento = glosaMovimiento,
            ConfiguracionCuentaOperacionId = configuracionCuentaOperacionId,
            FechaVencimiento = documento.FechaVencimiento
                ?? (diasCreditoProveedor > 0 ? documento.FechaDocumento.AddDays(diasCreditoProveedor) : null),
            Detalles = detalles.Select(d => new CompraDetalle
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                PorcentajeDescuento = d.PorcentajeDescuento,
                Total = Math.Round(d.Cantidad * d.PrecioUnitario * (1 - d.PorcentajeDescuento / 100m), 2),
                CuentaContableId = d.CuentaContableId,
            }).ToList(),
        };

        var encontrada = await _comprasRepository.ActualizarAsync(actualizada);
        if (!encontrada) throw new CompraNoEncontradaException(compra.Id);

        await IngresarStockDetallesAsync(detalles, almacenId, glosaMovimiento, compra.Id);

        // Se anula el asiento contable previamente generado (si lo hubo) y se regenera con
        // los datos/ítems actualizados, para que el Libro Diario/Mayor reflejen la edición.
        if (compra.AsientoContableId.HasValue)
        {
            await _contabilidadService.AnularAsientoAsync(compra.EmpresaId, compra.AsientoContableId.Value);
        }
        var asiento = await _contabilidadService.RegistrarAsientoCompraAsync(actualizada);
        await _comprasRepository.ActualizarAsientoContableAsync(compra.Id, asiento?.Id);

        return compra;
    }

    /// <summary>
    /// Anula (baja lógica) una compra ya registrada: revierte el stock/Kardex ingresado
    /// (devolviéndolo como SALIDA del almacén de la compra) y anula el asiento contable
    /// generado, al estilo CONCAR. No se permite si la compra ya fue pagada (debe revertirse
    /// el pago primero) ni si ya estaba anulada. La autorización del administrador se valida
    /// antes de llamar a este método (<see cref="AutorizacionAdminService"/>).
    /// </summary>
    public async Task AnularCompraAsync(Compra compra, string? motivo, Guid? usuarioId = null)
    {
        if (compra.Anulado) throw new InvalidOperationException("La compra ya está anulada");
        if (compra.Pagado) throw new InvalidOperationException("No se puede anular una compra que ya fue pagada");

        await RevertirStockDetallesAsync(compra, compra.AlmacenId, $"Anulación compra {compra.Id}");

        if (compra.AsientoContableId.HasValue)
        {
            await _contabilidadService.AnularAsientoAsync(compra.EmpresaId, compra.AsientoContableId.Value, usuarioId, "Anulación de compra");
        }

        await _comprasRepository.AnularAsync(compra.Id, motivo);
    }

    /// <summary>Ingresa stock/Kardex (ENTRADA) por cada línea comprada, salvo las marcadas como Servicio.</summary>
    private async Task IngresarStockDetallesAsync(List<DetalleCompra> detalles, Guid almacenId, string? glosaMovimiento, Guid compraId)
    {
        var productoIds = detalles.Select(d => d.ProductoId).Distinct().ToList();
        var serviciosIds = await _db.Productos
            .Where(p => productoIds.Contains(p.Id) && p.EsServicio)
            .Select(p => p.Id)
            .ToListAsync();
        var glosaKardex = string.IsNullOrWhiteSpace(glosaMovimiento) ? $"Compra {compraId}" : glosaMovimiento;
        foreach (var detalle in detalles)
        {
            if (serviciosIds.Contains(detalle.ProductoId)) continue;
            await _productosService.AjustarStockAsync(
                detalle.ProductoId, almacenId, detalle.Cantidad, TipoMovimiento.ENTRADA,
                glosaKardex, detalle.PrecioUnitario);
        }
    }

    /// <summary>Revierte (SALIDA) el stock/Kardex que una compra había ingresado; usado al editarla o anularla.</summary>
    private async Task RevertirStockDetallesAsync(Compra compra, Guid almacenId, string? referencia = null)
    {
        var productoIds = compra.Detalles.Select(d => d.ProductoId).Distinct().ToList();
        var serviciosIds = await _db.Productos
            .Where(p => productoIds.Contains(p.Id) && p.EsServicio)
            .Select(p => p.Id)
            .ToListAsync();
        var glosa = referencia ?? $"Edición compra {compra.Id} (reversión)";
        foreach (var detalle in compra.Detalles)
        {
            if (serviciosIds.Contains(detalle.ProductoId)) continue;
            await _productosService.AjustarStockAsync(
                detalle.ProductoId, almacenId, detalle.Cantidad, TipoMovimiento.SALIDA,
                glosa);
        }
    }
}
