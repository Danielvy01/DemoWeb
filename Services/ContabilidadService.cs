using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

/// <summary>
/// Genera automáticamente los asientos del Libro Diario que corresponden a los documentos de
/// venta (Factura/Boleta/Nota de venta/Nota de crédito), de modo que los libros y reportes
/// contables (Libro Mayor, Estado de Resultados) queden actualizados sin intervención manual.
/// Usa el Plan Contable General Empresarial (PCGE) peruano por convención de código:
/// 10 Caja y bancos, 12 Cuentas por cobrar comerciales, 40 Tributos por pagar (IGV), 70 Ventas.
/// Si la empresa no tiene configuradas esas cuentas, no genera el asiento (no rompe la venta).
/// </summary>
public class ContabilidadService
{
    private readonly SiserpDbContext _db;
    private readonly EjercicioContableService _ejercicioService;

    public ContabilidadService(SiserpDbContext db, EjercicioContableService ejercicioService)
    {
        _db = db;
        _ejercicioService = ejercicioService;
    }

    /// <summary>
    /// Calcula el siguiente número correlativo de asiento para la empresa a partir del máximo
    /// existente (más robusto ante huecos que un conteo de filas, p. ej. si se eliminara un asiento).
    /// </summary>
    public async Task<int> ObtenerSiguienteNumeroAsientoAsync(Guid empresaId)
    {
        var maxNumero = await _db.AsientosContables
            .Where(a => a.EmpresaId == empresaId)
            .Select(a => (int?)a.Numero)
            .MaxAsync();
        return (maxNumero ?? 0) + 1;
    }

    /// <summary>
    /// Guarda un asiento recién agregado reintentando la asignación de <see cref="AsientoContable.Numero"/>
    /// si dos solicitudes concurrentes calcularon el mismo correlativo y una choca contra el índice único
    /// (EmpresaId, Numero): en vez de propagar un error 500, reasigna el siguiente número disponible y reintenta.
    /// </summary>
    public async Task GuardarAsientoConReintentoAsync(AsientoContable asiento, int maxIntentos = 5)
    {
        for (var intento = 1; ; intento++)
        {
            try
            {
                await _db.SaveChangesAsync();
                return;
            }
            catch (DbUpdateException ex) when (intento < maxIntentos && EsColisionNumeroAsiento(ex))
            {
                asiento.Numero = await ObtenerSiguienteNumeroAsientoAsync(asiento.EmpresaId);
            }
        }
    }

    private static bool EsColisionNumeroAsiento(DbUpdateException ex) =>
        ex.InnerException is SqlException sqlEx
        && (sqlEx.Number == 2601 || sqlEx.Number == 2627)
        && sqlEx.Message.Contains("IX_AsientosContables_EmpresaId_Numero", StringComparison.OrdinalIgnoreCase);

    private async Task<CuentaContable?> BuscarCuentaAsync(Guid empresaId, string prefijoCodigo) =>
        await _db.CuentasContables
            .Where(c => c.EmpresaId == empresaId && c.Activa && c.Codigo.StartsWith(prefijoCodigo))
            .OrderBy(c => c.Codigo)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Busca el "código de operación" configurado (al estilo CONCAR, p. ej. "FT. Emitidas
    /// M.N. Tercero") para la combinación exacta de movimiento/tipo de documento/moneda, que
    /// indica la cuenta contable COMPLETA (no solo la clase) y el subdiario a usar. Si no hay
    /// configuración, <c>null</c>: el llamador conserva el comportamiento anterior (primera
    /// cuenta activa por prefijo de clase, p. ej. "12"/"42").
    /// </summary>
    private async Task<ConfiguracionCuentaOperacion?> BuscarConfiguracionOperacionAsync(
        Guid empresaId, MovimientoOperacionContable movimiento, string tipoDocumento, string moneda) =>
        await _db.ConfiguracionesCuentaOperacion
            .Include(c => c.CuentaContable)
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.Activo
                && c.Movimiento == movimiento && c.TipoDocumento == tipoDocumento
                && c.Moneda == (moneda ?? "PEN"));

    /// <summary>
    /// Carga el código de operación indicado explícitamente por su Id (elegido por el usuario
    /// en el formulario de registro de la compra/venta, al estilo CONCAR), en vez de inferirlo
    /// automáticamente por (Movimiento, TipoDocumento, Moneda). Tiene prioridad sobre la
    /// búsqueda automática cuando viene informado.
    /// </summary>
    private async Task<ConfiguracionCuentaOperacion?> BuscarConfiguracionOperacionPorIdAsync(Guid empresaId, Guid id) =>
        await _db.ConfiguracionesCuentaOperacion
            .Include(c => c.CuentaContable)
            .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);

    /// <summary>
    /// Tipo de cambio (venta) vigente a la fecha indicada para convertir <paramref name="moneda"/>
    /// a soles; 1 si la moneda ya es PEN o si no hay ningún tipo de cambio registrado hasta esa
    /// fecha (en cuyo caso el asiento igual se genera, sin bloquear la venta/compra, pero queda
    /// expresado como si fuera 1:1 hasta que se registre el tipo de cambio correspondiente).
    /// </summary>
    private async Task<decimal> ObtenerTipoCambioAsync(Guid empresaId, string moneda, DateTime fecha)
    {
        if (string.Equals(moneda, "PEN", StringComparison.OrdinalIgnoreCase)) return 1m;
        var tipoCambio = await _db.TiposCambio
            .Where(t => t.EmpresaId == empresaId && t.Fecha <= fecha.Date)
            .OrderByDescending(t => t.Fecha)
            .FirstOrDefaultAsync();
        return tipoCambio?.Venta ?? 1m;
    }

    /// <summary>
    /// Soporte multimoneda (al estilo CONCAR): las líneas recibidas traen su importe en la
    /// moneda de origen de la operación (<paramref name="moneda"/>); esta función conserva ese
    /// importe en <see cref="AsientoDetalle.DebeMe"/>/<see cref="AsientoDetalle.HaberMe"/> y
    /// convierte <see cref="AsientoDetalle.Debe"/>/<see cref="AsientoDetalle.Haber"/> a soles
    /// (moneda funcional) con el tipo de cambio indicado, para que el Libro Mayor, el Balance
    /// General y el Estado de Resultados siempre se calculen en una sola moneda.
    /// </summary>
    private static void AplicarMoneda(IEnumerable<AsientoDetalle> detalles, string moneda, decimal tipoCambio)
    {
        var codigoMoneda = string.IsNullOrWhiteSpace(moneda) ? "PEN" : moneda.ToUpperInvariant();
        foreach (var detalle in detalles)
        {
            detalle.Moneda = codigoMoneda;
            detalle.TipoCambio = tipoCambio;
            detalle.DebeMe = detalle.Debe;
            detalle.HaberMe = detalle.Haber;
            if (codigoMoneda != "PEN")
            {
                detalle.Debe = Math.Round(detalle.Debe * tipoCambio, 2);
                detalle.Haber = Math.Round(detalle.Haber * tipoCambio, 2);
            }
        }
    }

    /// <summary>
    /// Registra el asiento contable de una venta (Factura, Boleta o Nota de venta), con
    /// imputación por cuenta de producto al estilo CONCAR:
    /// Debe: Caja/Bancos (si es al contado) o Cuentas por cobrar (si es al crédito).
    /// Haber: Ventas, agrupado por la cuenta de venta que tenga asignada cada producto
    /// (<see cref="Producto.CuentaVentaId"/>; por defecto la primera cuenta "70" del plan)
    /// + IGV por pagar (si corresponde).
    /// Adicionalmente genera el asiento de costo de ventas: Debe costo de venta (69 por
    /// defecto, o <see cref="Producto.CuentaCostoVentaId"/>) / Haber existencias (20 por
    /// defecto, o <see cref="Producto.CuentaExistenciasId"/>), por el costo de los productos
    /// vendidos (no aplica a servicios). Todas las líneas se imputan al centro de costo del
    /// comprobante (<see cref="Comprobante.CentroCostoId"/>), si se indicó alguno.
    /// La línea de Caja/Cuentas por cobrar queda con el cliente, el número de comprobante y la
    /// fecha de vencimiento: si la cuenta usada (típicamente 12) está marcada como documentaria
    /// (<see cref="CuentaContable.RequiereAnalisisDocumentario"/>), <see cref="CrearAsientoAsync"/>
    /// genera automáticamente el <see cref="DocumentoCtaCte"/> pendiente de cobro, sin necesidad
    /// de registrar una provisión manual.
    /// </summary>
    public async Task<AsientoContable?> RegistrarAsientoVentaAsync(Comprobante comprobante)
    {
        var cuentaVentasDefault = await BuscarCuentaAsync(comprobante.EmpresaId, "70");
        var cuentaIgv = comprobante.Igv > 0 ? await BuscarCuentaAsync(comprobante.EmpresaId, "40") : null;

        // Código de operación configurado (al estilo CONCAR, p. ej. "FT. Emitidas M.N. Tercero"):
        // de existir, indica la cuenta de cobro COMPLETA (no solo la clase 12/10) y el subdiario.
        var configuracion = await BuscarConfiguracionOperacionAsync(
            comprobante.EmpresaId, MovimientoOperacionContable.EMITIDO, comprobante.TipoComprobante.ToString(), comprobante.Moneda);
        var cuentaCobro = configuracion?.CuentaContable
            ?? (comprobante.CondicionPago == CondicionPago.CREDITO
                ? await BuscarCuentaAsync(comprobante.EmpresaId, "12")
                : await BuscarCuentaAsync(comprobante.EmpresaId, "10"));
        var subdiario = configuracion?.Subdiario ?? TipoSubdiario.VENTAS;
        var cuentaCostoVentaDefault = await BuscarCuentaAsync(comprobante.EmpresaId, "69");
        var cuentaExistenciasDefault = await BuscarCuentaAsync(comprobante.EmpresaId, "20");

        if (cuentaCobro is null) return null; // Plan contable no configurado: se omite sin afectar la venta

        var productos = await _db.Productos
            .Where(p => p.EmpresaId == comprobante.EmpresaId && comprobante.Detalles.Select(d => d.ProductoId).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var glosa = $"Venta {comprobante.TipoComprobante} {comprobante.Serie}-{comprobante.Correlativo:D8}";
        var centroCostoId = comprobante.CentroCostoId;
        var numeroComprobante = $"{comprobante.Serie}-{comprobante.Correlativo:D8}";

        // --- Ingreso por ventas, agrupado por cuenta de venta de cada producto ---
        var ventasPorCuenta = new Dictionary<Guid, decimal>();
        foreach (var detalle in comprobante.Detalles)
        {
            var producto = productos.GetValueOrDefault(detalle.ProductoId);
            var cuentaVentaId = producto?.CuentaVentaId ?? cuentaVentasDefault?.Id;
            if (cuentaVentaId is null) return null; // Ítem sin cuenta de venta (ni por defecto configurada): se omite el asiento completo
            ventasPorCuenta[cuentaVentaId.Value] = ventasPorCuenta.GetValueOrDefault(cuentaVentaId.Value) + detalle.ValorVenta;
        }

        var lineaCobro = new AsientoDetalle
        {
            CuentaId = cuentaCobro.Id,
            Debe = comprobante.Total,
            Haber = 0,
            Glosa = glosa,
            CentroCostoId = centroCostoId,
            ClienteId = comprobante.ClienteId,
            NumeroDocumento = numeroComprobante,
            TipoDocumento = comprobante.TipoComprobante.ToString(),
            FechaVencimiento = comprobante.FechaVencimiento ?? comprobante.FechaEmision,
        };
        var detalles = new List<AsientoDetalle> { lineaCobro };
        detalles.AddRange(ventasPorCuenta.Select(kv =>
            new AsientoDetalle { CuentaId = kv.Key, Debe = 0, Haber = kv.Value, Glosa = glosa, CentroCostoId = centroCostoId }));

        if (comprobante.Igv > 0 && cuentaIgv is not null)
        {
            detalles.Add(new AsientoDetalle { CuentaId = cuentaIgv.Id, Debe = 0, Haber = comprobante.Igv, Glosa = glosa, CentroCostoId = centroCostoId });
        }
        else if (comprobante.Igv > 0)
        {
            // Sin cuenta de IGV configurada: se suma a la primera línea de ventas para no descuadrar el asiento.
            var primeraVenta = detalles.First(d => d.CuentaId == detalles[1].CuentaId);
            primeraVenta.Haber += comprobante.Igv;
        }

        // --- Costo de ventas (Debe costo / Haber existencias), agrupado por cuenta de cada producto ---
        var costoPorCuenta = new Dictionary<(Guid CuentaCosto, Guid CuentaExistencias), decimal>();
        foreach (var detalle in comprobante.Detalles)
        {
            var producto = productos.GetValueOrDefault(detalle.ProductoId);
            if (producto is null || producto.EsServicio) continue;
            var cuentaCostoId = producto.CuentaCostoVentaId ?? cuentaCostoVentaDefault?.Id;
            var cuentaExistId = producto.CuentaExistenciasId ?? cuentaExistenciasDefault?.Id;
            if (cuentaCostoId is null || cuentaExistId is null) continue; // sin cuentas de costo configuradas: se omite solo esta parte, no la venta
            var costo = producto.PrecioCosto * detalle.Cantidad;
            if (costo <= 0) continue;
            var key = (cuentaCostoId.Value, cuentaExistId.Value);
            costoPorCuenta[key] = costoPorCuenta.GetValueOrDefault(key) + costo;
        }
        foreach (var ((cuentaCostoId, cuentaExistId), monto) in costoPorCuenta)
        {
            var montoRedondeado = Math.Round(monto, 2);
            detalles.Add(new AsientoDetalle { CuentaId = cuentaCostoId, Debe = montoRedondeado, Haber = 0, Glosa = $"Costo de venta - {glosa}", CentroCostoId = centroCostoId });
            detalles.Add(new AsientoDetalle { CuentaId = cuentaExistId, Debe = 0, Haber = montoRedondeado, Glosa = $"Costo de venta - {glosa}", CentroCostoId = centroCostoId });
        }

        var tipoCambio = await ObtenerTipoCambioAsync(comprobante.EmpresaId, comprobante.Moneda, comprobante.FechaEmision);
        AplicarMoneda(detalles, comprobante.Moneda, tipoCambio);

        return await CrearAsientoAsync(comprobante.EmpresaId, comprobante.FechaEmision, glosa, numeroComprobante, detalles, subdiario);
    }

    /// <summary>
    /// Registra el asiento contable de una Nota de Crédito: reduce la venta/cuenta por cobrar original.
    /// Debe: Ventas + IGV (reversa). Haber: Cuentas por cobrar/Caja (reversa).
    /// </summary>
    public async Task<AsientoContable?> RegistrarAsientoNotaCreditoAsync(Comprobante notaCredito, CondicionPago condicionPagoOriginal)
    {
        var cuentaVentas = await BuscarCuentaAsync(notaCredito.EmpresaId, "70");
        var cuentaIgv = notaCredito.Igv > 0 ? await BuscarCuentaAsync(notaCredito.EmpresaId, "40") : null;
        var cuentaCobro = condicionPagoOriginal == CondicionPago.CREDITO
            ? await BuscarCuentaAsync(notaCredito.EmpresaId, "12")
            : await BuscarCuentaAsync(notaCredito.EmpresaId, "10");

        if (cuentaVentas is null || cuentaCobro is null) return null;

        var glosa = $"Nota de crédito {notaCredito.Serie}-{notaCredito.Correlativo:D8}: {notaCredito.MotivoNota}";
        var centroCostoId = notaCredito.CentroCostoId;
        var detalles = new List<AsientoDetalle>
        {
            new() { CuentaId = cuentaVentas.Id, Debe = notaCredito.OpGravada + notaCredito.OpExonerada + notaCredito.OpInafecta, Haber = 0, Glosa = glosa, CentroCostoId = centroCostoId },
            new() { CuentaId = cuentaCobro.Id, Debe = 0, Haber = notaCredito.Total, Glosa = glosa, CentroCostoId = centroCostoId, ClienteId = notaCredito.ClienteId },
        };
        if (notaCredito.Igv > 0 && cuentaIgv is not null)
        {
            detalles.Add(new AsientoDetalle { CuentaId = cuentaIgv.Id, Debe = notaCredito.Igv, Haber = 0, Glosa = glosa, CentroCostoId = centroCostoId });
        }
        else if (notaCredito.Igv > 0)
        {
            detalles[0].Debe += notaCredito.Igv;
        }

        var tipoCambio = await ObtenerTipoCambioAsync(notaCredito.EmpresaId, notaCredito.Moneda, notaCredito.FechaEmision);
        AplicarMoneda(detalles, notaCredito.Moneda, tipoCambio);

        return await CrearAsientoAsync(notaCredito.EmpresaId, notaCredito.FechaEmision, glosa, $"{notaCredito.Serie}-{notaCredito.Correlativo}", detalles, TipoSubdiario.VENTAS);
    }

    /// <summary>
    /// Registra el asiento contable de una compra (factura/boleta/recibo por honorarios
    /// recibido del proveedor), imputando cada ítem a la cuenta contable que el usuario le
    /// asignó (Existencias/Mercaderías 20, Activo Fijo 33/36, Servicios prestados por
    /// terceros 63, etc. del PCGE) — al estilo de la contabilización automática del
    /// registro de compras de CONCAR. Si un ítem no indica cuenta, se usa por defecto
    /// Existencias (20) o Servicios (63) según si el producto está marcado como servicio.
    /// Debe: cuenta(s) de destino de cada ítem + IGV (crédito fiscal, 40).
    /// Haber: Cuentas por pagar comerciales (42) si la compra es al crédito, o Caja/Bancos
    /// (10) si es al contado (sin fecha de vencimiento). La línea de Cuentas por pagar/Caja
    /// queda con el proveedor, el número del comprobante y la fecha de vencimiento: si la
    /// cuenta usada (típicamente 42) es documentaria, <see cref="CrearAsientoAsync"/> genera
    /// automáticamente el <see cref="DocumentoCtaCte"/> pendiente de pago, sin necesidad de
    /// registrar una provisión manual.
    /// </summary>
    public async Task<AsientoContable?> RegistrarAsientoCompraAsync(Compra compra)
    {
        // Código de operación configurado (al estilo CONCAR, p. ej. "FT. Recibidas M.N.
        // Tercero"): de existir, indica la cuenta por pagar/caja COMPLETA (no solo la clase
        // 42/10) y el subdiario del asiento generado. El elegido explícitamente al registrar
        // la compra (compra.ConfiguracionCuentaOperacionId) tiene prioridad sobre la búsqueda
        // automática por (RECIBIDO, TipoDocumento, Moneda).
        var configuracion = compra.ConfiguracionCuentaOperacionId.HasValue
            ? await BuscarConfiguracionOperacionPorIdAsync(compra.EmpresaId, compra.ConfiguracionCuentaOperacionId.Value)
            : await BuscarConfiguracionOperacionAsync(
                compra.EmpresaId, MovimientoOperacionContable.RECIBIDO, compra.TipoDocumento.ToString(), compra.Moneda);
        var cuentaContraPartida = configuracion?.CuentaContable
            ?? (compra.FechaVencimiento.HasValue
                ? await BuscarCuentaAsync(compra.EmpresaId, "42")
                : await BuscarCuentaAsync(compra.EmpresaId, "10"));
        if (cuentaContraPartida is null) return null; // Plan contable no configurado: se omite sin afectar la compra
        var subdiario = configuracion?.Subdiario ?? TipoSubdiario.COMPRAS;

        var cuentaIgv = compra.Igv > 0 ? await BuscarCuentaAsync(compra.EmpresaId, "40") : null;
        var cuentaExistenciasDefault = await BuscarCuentaAsync(compra.EmpresaId, "20");
        var cuentaServiciosDefault = await BuscarCuentaAsync(compra.EmpresaId, "63");

        var productoIds = compra.Detalles.Select(d => d.ProductoId).Distinct().ToList();
        var serviciosIds = await _db.Productos
            .Where(p => productoIds.Contains(p.Id) && p.EsServicio)
            .Select(p => p.Id)
            .ToListAsync();

        var glosa = $"Compra {compra.TipoDocumento} {compra.Serie}-{compra.Numero}".TrimEnd('-');
        var referencia = string.IsNullOrWhiteSpace(compra.Serie) ? compra.Numero : $"{compra.Serie}-{compra.Numero}";

        var totalesPorCuenta = new Dictionary<Guid, decimal>();
        foreach (var detalle in compra.Detalles)
        {
            var cuentaDestinoId = detalle.CuentaContableId
                ?? (serviciosIds.Contains(detalle.ProductoId) ? cuentaServiciosDefault?.Id : cuentaExistenciasDefault?.Id);
            if (cuentaDestinoId is null) return null; // Ítem sin cuenta (ni por defecto configurada): se omite el asiento completo
            totalesPorCuenta[cuentaDestinoId.Value] = totalesPorCuenta.GetValueOrDefault(cuentaDestinoId.Value) + detalle.Total;
        }

        var detalles = totalesPorCuenta
            .Select(kv => new AsientoDetalle { CuentaId = kv.Key, Debe = kv.Value, Haber = 0, Glosa = glosa, CentroCostoId = compra.CentroCostoId })
            .ToList();

        if (compra.Igv > 0 && cuentaIgv is not null)
        {
            detalles.Add(new AsientoDetalle { CuentaId = cuentaIgv.Id, Debe = compra.Igv, Haber = 0, Glosa = glosa, CentroCostoId = compra.CentroCostoId });
        }
        else if (compra.Igv > 0)
        {
            // Sin cuenta de IGV configurada: se suma a la primera línea para no descuadrar el asiento.
            detalles[0].Debe += compra.Igv;
        }

        detalles.Add(new AsientoDetalle
        {
            CuentaId = cuentaContraPartida.Id,
            Debe = 0,
            Haber = compra.Total,
            Glosa = glosa,
            CentroCostoId = compra.CentroCostoId,
            ProveedorId = compra.ProveedorId,
            NumeroDocumento = string.IsNullOrWhiteSpace(referencia) ? compra.Id.ToString() : referencia,
            TipoDocumento = compra.TipoDocumento.ToString(),
            FechaVencimiento = compra.FechaVencimiento ?? compra.FechaEmision,
        });

        var tipoCambio = compra.TipoCambio ?? await ObtenerTipoCambioAsync(compra.EmpresaId, compra.Moneda, compra.FechaTipoCambio ?? compra.FechaEmision);
        AplicarMoneda(detalles, compra.Moneda, tipoCambio);

        return await CrearAsientoAsync(compra.EmpresaId, compra.FechaEmision, glosa, referencia ?? compra.Id.ToString(), detalles, subdiario);
    }

    /// <summary>
    /// Registra la provisión contable de una planilla aprobada (al estilo CONCAR):
    /// Debe: Gastos de personal (62, por defecto, o la cuenta "62" que tenga el plan de la
    /// empresa), imputado al centro de costo de cada empleado.
    /// Haber: Remuneraciones y participaciones por pagar (41) por el total neto a pagar, y
    /// Tributos/aportes por pagar (40) por el total de descuentos (ONP/AFP/otros), si hubiera.
    /// No genera <see cref="DocumentoCtaCte"/> (el modelo de línea de asiento no referencia al
    /// empleado como tercero); el seguimiento de pago lo hace el propio módulo de Planillas.
    /// </summary>
    public async Task<AsientoContable?> RegistrarAsientoPlanillaAsync(Planilla planilla)
    {
        var cuentaGasto = await BuscarCuentaAsync(planilla.EmpresaId, "62");
        var cuentaRemuneracionesPorPagar = await BuscarCuentaAsync(planilla.EmpresaId, "41");
        if (cuentaGasto is null || cuentaRemuneracionesPorPagar is null) return null; // Plan contable no configurado: se omite sin afectar la planilla

        var glosa = $"Provisión de planilla {planilla.Periodo}";
        var detalles = new List<AsientoDetalle>();

        foreach (var grupo in planilla.Detalles.GroupBy(d => d.Empleado.CentroCostoId))
        {
            var bruto = grupo.Sum(d => d.SueldoBase + d.Bonificaciones);
            if (bruto <= 0) continue;
            detalles.Add(new AsientoDetalle { CuentaId = cuentaGasto.Id, Debe = bruto, Haber = 0, Glosa = glosa, CentroCostoId = grupo.Key });
        }
        if (detalles.Count == 0) return null;

        detalles.Add(new AsientoDetalle { CuentaId = cuentaRemuneracionesPorPagar.Id, Debe = 0, Haber = planilla.TotalNeto, Glosa = glosa });

        if (planilla.TotalDescuentos > 0)
        {
            var cuentaTributos = await BuscarCuentaAsync(planilla.EmpresaId, "40");
            if (cuentaTributos is not null)
                detalles.Add(new AsientoDetalle { CuentaId = cuentaTributos.Id, Debe = 0, Haber = planilla.TotalDescuentos, Glosa = glosa });
            else
                detalles[^1].Haber += planilla.TotalDescuentos; // Sin cuenta de tributos configurada: se suma a Remuneraciones por pagar para no descuadrar.
        }

        var asiento = await CrearAsientoAsync(planilla.EmpresaId, DateTime.UtcNow, glosa, planilla.Periodo, detalles, TipoSubdiario.PLANILLA);
        if (asiento is not null)
        {
            planilla.AsientoProvisionId = asiento.Id;
        }
        return asiento;
    }

    /// <summary>
    /// Registra el pago efectivo de una planilla ya sincronizada/confirmada por el banco:
    /// Debe: Remuneraciones y participaciones por pagar (41), cancelando la provisión.
    /// Haber: Caja y bancos (10), por el total neto desembolsado.
    /// </summary>
    public async Task<AsientoContable?> RegistrarAsientoPagoPlanillaAsync(Planilla planilla)
    {
        var cuentaRemuneracionesPorPagar = await BuscarCuentaAsync(planilla.EmpresaId, "41");
        var cuentaCaja = await BuscarCuentaAsync(planilla.EmpresaId, "10");
        if (cuentaRemuneracionesPorPagar is null || cuentaCaja is null) return null;

        var glosa = $"Pago de planilla {planilla.Periodo}";
        var detalles = new List<AsientoDetalle>
        {
            new() { CuentaId = cuentaRemuneracionesPorPagar.Id, Debe = planilla.TotalNeto, Haber = 0, Glosa = glosa },
            new() { CuentaId = cuentaCaja.Id, Debe = 0, Haber = planilla.TotalNeto, Glosa = glosa },
        };

        var asiento = await CrearAsientoAsync(planilla.EmpresaId, DateTime.UtcNow, glosa, planilla.Periodo, detalles, TipoSubdiario.CAJA_BANCOS);
        if (asiento is not null)
        {
            planilla.AsientoPagoId = asiento.Id;
        }
        return asiento;
    }

    /// <summary>
    /// Anula el asiento contable previamente generado para una compra/venta (p. ej. al editarla o
    /// anularla, antes de regenerarlo). <paramref name="usuarioId"/> es null cuando la anulación la
    /// dispara un proceso automático (edición/anulación de la compra o venta de origen) en vez de
    /// una acción manual sobre el Libro Diario.
    /// </summary>
    public async Task AnularAsientoAsync(Guid empresaId, Guid asientoId, Guid? usuarioId = null, string? observacion = null)
    {
        var asiento = await _db.AsientosContables
            .Include(a => a.Detalles).ThenInclude(d => d.Cuenta)
            .FirstOrDefaultAsync(a => a.Id == asientoId && a.EmpresaId == empresaId);
        if (asiento is null || asiento.Estado == EstadoAsiento.ANULADO) return;
        var estadoAnterior = asiento.Estado;
        var snapshotAnterior = SerializarSnapshotAsiento(asiento);
        asiento.Estado = EstadoAsiento.ANULADO;
        asiento.UpdatedAt = DateTime.UtcNow;
        asiento.UsuarioModificacionId = usuarioId;
        RegistrarHistorial(asiento, AccionHistorialAsiento.ANULACION, estadoAnterior, usuarioId, snapshotAnterior, observacion);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Fotografía (JSON) de un asiento -cabecera y líneas- usada por el historial de auditoría
    /// (<see cref="AsientoContableHistorial"/>): se llama ANTES y DESPUÉS de cada cambio para dejar
    /// registrada la versión correspondiente sin depender de que el dato siga vigente hoy.
    /// </summary>
    public static string SerializarSnapshotAsiento(AsientoContable asiento) => JsonSerializer.Serialize(new
    {
        asiento.Numero,
        asiento.Fecha,
        asiento.Glosa,
        asiento.Referencia,
        Estado = asiento.Estado.ToString(),
        Detalles = asiento.Detalles.Select(d => new
        {
            d.CuentaId,
            CuentaCodigo = d.Cuenta?.Codigo,
            CuentaNombre = d.Cuenta?.Nombre,
            d.Debe,
            d.Haber,
            d.Glosa,
            d.Moneda,
            d.TipoCambio,
            d.DebeMe,
            d.HaberMe,
            d.CentroCostoId,
            d.ClienteId,
            d.ProveedorId,
            d.NumeroDocumento,
            d.TipoDocumento,
            d.FechaVencimiento,
        }),
    });

    /// <summary>
    /// Agrega al <see cref="AsientoContable.Historial"/> el evento de auditoría correspondiente e
    /// incrementa la <see cref="AsientoContable.Version"/> del asiento (no guarda cambios; el
    /// llamador debe hacer <c>SaveChangesAsync</c> junto con el resto de la operación).
    /// </summary>
    public void RegistrarHistorial(AsientoContable asiento, AccionHistorialAsiento accion, EstadoAsiento? estadoAnterior, Guid? usuarioId, string? snapshotAnteriorJson, string? observacion = null)
    {
        asiento.Version += 1;
        _db.AsientosContablesHistorial.Add(new AsientoContableHistorial
        {
            AsientoId = asiento.Id,
            Version = asiento.Version,
            Accion = accion,
            UsuarioId = usuarioId,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = asiento.Estado,
            SnapshotAnterior = snapshotAnteriorJson,
            SnapshotNuevo = SerializarSnapshotAsiento(asiento),
            Observacion = observacion,
        });
    }

    /// <summary>
    /// Punto único de generación del Análisis de Cuentas (CxC/CxP por tercero y vencimiento, al
    /// estilo CONCAR): si la línea tiene tercero + número de documento y su cuenta está marcada
    /// como documentaria (<see cref="CuentaContable.RequiereAnalisisDocumentario"/>), crea el
    /// <see cref="DocumentoCtaCte"/> pendiente correspondiente. Lo usan tanto la confirmación
    /// manual de asientos del Libro Diario (<c>ContabilidadController.ConfirmarAsiento</c>) como
    /// la contabilización automática de Ventas/Compras (<see cref="CrearAsientoAsync"/>), de modo
    /// que una venta al crédito o una compra con vencimiento generen su documento sin necesidad
    /// de una provisión manual.
    /// </summary>
    public async Task GenerarDocumentoCtaCteAsync(Guid empresaId, AsientoContable asiento, AsientoDetalle detalle, CuentaContable cuenta)
    {
        if (!cuenta.RequiereAnalisisDocumentario) return;
        if (detalle.ClienteId is null && detalle.ProveedorId is null) return;
        if (string.IsNullOrWhiteSpace(detalle.NumeroDocumento)) return;
        var yaExiste = await _db.DocumentosCtaCte.AnyAsync(d => d.AsientoDetalleId == detalle.Id);
        if (yaExiste) return;

        var monto = Math.Round(Math.Abs(detalle.Debe - detalle.Haber), 2);
        if (monto <= 0) return;

        _db.DocumentosCtaCte.Add(new DocumentoCtaCte
        {
            EmpresaId = empresaId,
            AsientoDetalleId = detalle.Id,
            CuentaId = detalle.CuentaId,
            Tipo = detalle.Debe > detalle.Haber ? TipoDocumentoCtaCte.CXC : TipoDocumentoCtaCte.CXP,
            ClienteId = detalle.ClienteId,
            ProveedorId = detalle.ProveedorId,
            TipoDocumento = detalle.TipoDocumento ?? "ASIENTO",
            NumeroDocumento = detalle.NumeroDocumento,
            FechaEmision = asiento.Fecha,
            FechaVencimiento = detalle.FechaVencimiento ?? asiento.Fecha,
            Glosa = detalle.Glosa ?? asiento.Glosa,
            CentroCostoId = detalle.CentroCostoId,
            MontoOriginal = monto,
            MontoCancelado = 0,
            Estado = EstadoDocumentoCtaCte.PENDIENTE,
        });
    }

    private async Task<AsientoContable?> CrearAsientoAsync(Guid empresaId, DateTime fecha, string glosa, string referencia, List<AsientoDetalle> detalles, TipoSubdiario subdiario = TipoSubdiario.GENERAL)
    {
        // Bloqueo de periodo cerrado: una venta/compra con fecha en un periodo ya cerrado no
        // genera su asiento automático (no bloquea el documento comercial, al igual que cuando
        // el plan de cuentas no está configurado), quedando pendiente de un ajuste manual una vez
        // reabierto el periodo o imputado al periodo abierto correspondiente.
        if (!await _ejercicioService.EstaPeriodoAbiertoAsync(empresaId, fecha)) return null;

        var numero = await ObtenerSiguienteNumeroAsientoAsync(empresaId);
        var asiento = new AsientoContable
        {
            EmpresaId = empresaId,
            Numero = numero,
            Fecha = fecha,
            Glosa = glosa,
            Referencia = referencia,
            Estado = EstadoAsiento.CONFIRMADO,
            Subdiario = subdiario,
            Detalles = detalles,
        };
        _db.AsientosContables.Add(asiento);
        await GuardarAsientoConReintentoAsync(asiento);
        RegistrarHistorial(asiento, AccionHistorialAsiento.CREACION, null, null, null, "Generado automáticamente desde Ventas/Compras");
        await _db.SaveChangesAsync();

        // Genera el documento de cuenta corriente (Análisis de Cuentas) de las líneas con
        // tercero + documento imputadas a una cuenta documentaria (p. ej. la Cuenta por Cobrar
        // de una venta al crédito, o la Cuenta por Pagar de una compra con vencimiento): así
        // las ventas y compras quedan integradas automáticamente, sin depender de una provisión manual.
        var huboTerceros = detalles.Any(d => d.ClienteId is not null || d.ProveedorId is not null);
        foreach (var detalle in detalles)
        {
            if (detalle.ClienteId is null && detalle.ProveedorId is null) continue;
            var cuenta = await _db.CuentasContables.FindAsync(detalle.CuentaId);
            if (cuenta is null) continue;
            await GenerarDocumentoCtaCteAsync(empresaId, asiento, detalle, cuenta);
        }
        if (huboTerceros) await _db.SaveChangesAsync();

        return asiento;
    }
}
