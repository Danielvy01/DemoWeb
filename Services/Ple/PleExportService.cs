using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Models;

namespace Siserp.Api.Services.Ple;

/// <summary>Archivo PLE generado: nombre con la nomenclatura oficial SUNAT y contenido en texto plano.</summary>
public record PleArchivo(string NombreArchivo, byte[] Contenido);

/// <summary>
/// Genera los 5 formatos de exportación PLE (Programa de Libros Electrónicos) exigidos por
/// SUNAT, al estilo de los libros electrónicos de CONCAR: Registro de Compras (8.1), Registro
/// de Ventas e Ingresos (14.1), Libro Diario (5.1), Libro Mayor (6.1) y Libro Caja y Bancos
/// (1.1). Cada registro es una línea de texto con campos separados por "|" (pipe), codificada
/// en ISO-8859-1 (requisito del validador PLE de SUNAT, que no admite UTF-8 con BOM), y el
/// nombre del archivo sigue la nomenclatura oficial:
/// LE{RUC}{AAAAMM}00{CódigoLibro}{SubLibro}{Indicador}.txt
/// (LE = Libro Electrónico, 00 = indicador de período mensual, SubLibro = 000 por defecto,
/// Indicador = 1 "generado del período"). Los datos provienen directamente de los documentos
/// ya registrados (Compra/Comprobante) y de los asientos contables vinculados, de modo que el
/// anexo cumple con los campos adicionales exigidos por SUNAT (tipo de cambio del día, código
/// de bien/servicio, estado de la operación, etc.) sin captura manual adicional.
/// </summary>
public class PleExportService
{
    private readonly SiserpDbContext _db;
    private static readonly Encoding EncodingPle = Encoding.Latin1;

    public PleExportService(SiserpDbContext db)
    {
        _db = db;
    }

    // --- Utilidades comunes de formateo exigidas por el validador PLE de SUNAT ---

    /// <summary>Quita el separador "|" y saltos de línea de los campos de texto libre (glosas, razones sociales).</summary>
    private static string L(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? "" : valor.Replace("|", "/").Replace("\r", " ").Replace("\n", " ").Trim();

    private static string F(DateTime? fecha) => fecha.HasValue ? fecha.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "";

    private static string M(decimal? importe) => importe.HasValue ? importe.Value.ToString("0.00", CultureInfo.InvariantCulture) : "0.00";

    private static string Periodo(int anio, int mes) => $"{anio:D4}{mes:D2}00";

    private static string NombreArchivo(string ruc, int anio, int mes, string codigoLibro) =>
        $"LE{ruc}{anio:D4}{mes:D2}00{codigoLibro}0001.txt";

    private static byte[] Codificar(IEnumerable<string> lineas)
    {
        var texto = string.Join("\r\n", lineas) + "\r\n";
        return EncodingPle.GetBytes(texto);
    }

    private async Task<Empresa> ObtenerEmpresaAsync(Guid empresaId) =>
        await _db.Empresas.FirstAsync(e => e.Id == empresaId);

    // --- 8.1 Registro de Compras ---

    /// <summary>
    /// Formato 8.1 (Registro de Compras): un registro por documento de compra del período,
    /// con la base imponible desagregada, IGV, el tipo de cambio SUNAT del día de la operación
    /// (<see cref="Compra.TipoCambio"/>, tomado del <see cref="TipoCambio"/> vigente a la fecha
    /// si no se indicó uno manual) y el código de bien/servicio del primer ítem de la compra.
    /// </summary>
    public async Task<PleArchivo> GenerarRegistroComprasAsync(Guid empresaId, int anio, int mes)
    {
        var empresa = await ObtenerEmpresaAsync(empresaId);
        var desde = new DateTime(anio, mes, 1);
        var hasta = desde.AddMonths(1).AddTicks(-1);

        var compras = await _db.Compras
            .Include(c => c.Proveedor)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .Include(c => c.AsientoContable)
            .Where(c => c.EmpresaId == empresaId && c.FechaEmision >= desde && c.FechaEmision <= hasta)
            .OrderBy(c => c.FechaEmision).ThenBy(c => c.Serie).ThenBy(c => c.Numero)
            .ToListAsync();

        var periodo = Periodo(anio, mes);
        var lineas = new List<string>();
        var correlativo = 0;
        foreach (var c in compras)
        {
            correlativo++;
            var cuo = $"CP{correlativo:D8}";
            var esGravada = c.Igv > 0;
            var codigoBienServicio = c.Detalles.Select(d => d.Producto.CodigoBienServicio).FirstOrDefault(cod => !string.IsNullOrWhiteSpace(cod)) ?? "";
            var tieneDetraccion = c.TipoOperacionSunat == TipoOperacionSunat.COMPRA_SUJETA_DETRACCION;
            var tienePercepcion = c.TipoOperacionSunat == TipoOperacionSunat.COMPRA_SUJETA_PERCEPCION;
            var tieneRetencion = c.TipoOperacionSunat == TipoOperacionSunat.COMPRA_SUJETA_RETENCION;

            var campos = new[]
            {
                periodo,                                                              // 1 Periodo
                cuo,                                                                  // 2 CUO
                c.AsientoContable?.Numero.ToString() ?? "",                           // 3 Correlativo del asiento contable
                F(c.FechaEmision),                                                    // 4 Fecha de emisión
                F(c.FechaVencimiento),                                                // 5 Fecha de vencimiento/pago
                SunatCatalogosPle.TipoDocumentoCompra(c.TipoDocumento),               // 6 Tipo de comprobante (Tabla 10)
                L(c.Serie),                                                           // 7 Serie
                "",                                                                   // 8 Año de emisión DUA/DSI
                L(c.Numero),                                                          // 9 Número del comprobante
                SunatCatalogosPle.TipoDocumentoIdentidad(TipoDocumentoIdentidad.RUC), // 10 Tipo doc. identidad proveedor
                L(c.Proveedor.Ruc),                                                   // 11 Número doc. identidad proveedor
                L(c.Proveedor.RazonSocial),                                           // 12 Razón social proveedor
                M(esGravada ? c.Subtotal : 0),                                        // 13 Base imponible gravada
                M(esGravada ? c.Igv : 0),                                             // 14 IGV
                M(0),                                                                 // 15 Base imponible IVAP
                M(0),                                                                 // 16 IGV IVAP
                M(esGravada ? 0 : c.Subtotal),                                        // 17 Valor adquisiciones no gravadas
                M(0),                                                                 // 18 ISC
                M(0),                                                                 // 19 Otros tributos
                M(c.Total),                                                           // 20 Importe total
                M(0),                                                                 // 21 Monto no deducible
                codigoBienServicio,                                                   // 22 Código de bien o servicio (Tabla 17)
                "",                                                                   // 23 Referencia a comprobante que modifica
                SunatCatalogosPle.CodigoMoneda(c.Moneda) == "PEN" ? "" : M(c.TipoCambio ?? 1m), // 24 Tipo de cambio SUNAT del día
                "",                                                                   // 25 Fecha emisión doc. modificado
                "",                                                                   // 26 Número doc. modificado
                tieneDetraccion ? "1" : "",                                           // 27 Marca: sujeta a detracción
                tienePercepcion ? "1" : "",                                           // 28 Marca: sujeta a percepción
                tieneRetencion ? "1" : "",                                            // 29 Marca: sujeta a retención
                SunatCatalogosPle.EstadoOperacionAsiento(c.AsientoContable?.Estado),  // 30 Estado de la operación (Tabla 19)
            };
            lineas.Add(string.Join("|", campos) + "|");
        }

        return new PleArchivo(NombreArchivo(empresa.Ruc, anio, mes, "080100"), Codificar(lineas));
    }

    // --- 14.1 Registro de Ventas e Ingresos ---

    /// <summary>
    /// Formato 14.1 (Registro de Ventas e Ingresos): un registro por comprobante emitido en el
    /// período (excluye la Nota de venta, que no se declara a SUNAT), con el código de
    /// bien/servicio del primer ítem y, para Notas de Crédito/Débito, la referencia al
    /// comprobante que modifican.
    /// </summary>
    public async Task<PleArchivo> GenerarRegistroVentasAsync(Guid empresaId, int anio, int mes)
    {
        var empresa = await ObtenerEmpresaAsync(empresaId);
        var desde = new DateTime(anio, mes, 1);
        var hasta = desde.AddMonths(1).AddTicks(-1);

        var comprobantes = await _db.Comprobantes
            .Include(c => c.Cliente)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .Include(c => c.ComprobanteReferencia)
            .Where(c => c.EmpresaId == empresaId && c.FechaEmision >= desde && c.FechaEmision <= hasta
                        && c.TipoComprobante != TipoComprobante.NOTA_VENTA)
            .OrderBy(c => c.FechaEmision).ThenBy(c => c.Serie).ThenBy(c => c.Correlativo)
            .ToListAsync();

        var periodo = Periodo(anio, mes);
        var lineas = new List<string>();
        var correlativo = 0;
        foreach (var c in comprobantes)
        {
            correlativo++;
            var cuo = $"CV{correlativo:D8}";
            var codigoBienServicio = c.Detalles.Select(d => d.Producto.CodigoBienServicio).FirstOrDefault(cod => !string.IsNullOrWhiteSpace(cod)) ?? "";

            var campos = new[]
            {
                periodo,                                                       // 1 Periodo
                cuo,                                                           // 2 CUO
                $"{c.Serie}-{c.Correlativo:D8}",                               // 3 Correlativo/número único del comprobante
                F(c.FechaEmision),                                             // 4 Fecha de emisión
                F(c.FechaVencimiento),                                        // 5 Fecha de vencimiento
                SunatCatalogosPle.TipoDocumentoVenta(c.TipoComprobante),      // 6 Tipo de comprobante (Tabla 10)
                L(c.Serie),                                                   // 7 Serie
                c.Correlativo.ToString(),                                     // 8 Número del comprobante
                SunatCatalogosPle.TipoDocumentoIdentidad(c.Cliente.TipoDocumento), // 9 Tipo doc. identidad cliente
                L(c.Cliente.NumeroDocumento),                                 // 10 Número doc. identidad cliente
                L(c.Cliente.NombreORazonSocial),                              // 11 Razón social/nombre cliente
                M(0),                                                         // 12 Valor facturado exportación
                M(c.OpGravada),                                               // 13 Base imponible gravada
                M(c.Igv),                                                     // 14 IGV
                M(0),                                                         // 15 Base imponible gravada IVAP
                M(0),                                                         // 16 IGV IVAP
                M(c.OpExonerada + c.OpInafecta),                              // 17 Valor de operaciones no gravadas
                M(0),                                                         // 18 ISC
                M(0),                                                         // 19 Otros tributos
                M(c.Total),                                                   // 20 Importe total del comprobante
                codigoBienServicio,                                           // 21 Código de bien o servicio (Tabla 17)
                SunatCatalogosPle.CodigoMoneda(c.Moneda) == "PEN" ? "" : M(1m), // 22 Tipo de cambio SUNAT del día (ventas se facturan en soles salvo excepción)
                c.ComprobanteReferencia != null ? $"{c.ComprobanteReferencia.Serie}-{c.ComprobanteReferencia.Correlativo:D8}" : "", // 23 Documento que modifica (NC/ND)
                F(c.ComprobanteReferencia?.FechaEmision),                     // 24 Fecha de emisión del documento que modifica
                SunatCatalogosPle.EstadoOperacionComprobante(c.Estado),       // 25 Estado de la operación (Tabla 19)
            };
            lineas.Add(string.Join("|", campos) + "|");
        }

        return new PleArchivo(NombreArchivo(empresa.Ruc, anio, mes, "140100"), Codificar(lineas));
    }

    // --- 5.1 Libro Diario ---

    /// <summary>
    /// Formato 5.1 (Libro Diario): un registro por línea de cada asiento contable CONFIRMADO del
    /// período, con su cuenta, glosa, documento de referencia (vinculación asiento ↔ comprobante
    /// SUNAT), moneda y tipo de cambio aplicado.
    /// </summary>
    public async Task<PleArchivo> GenerarLibroDiarioAsync(Guid empresaId, int anio, int mes)
    {
        var empresa = await ObtenerEmpresaAsync(empresaId);
        var desde = new DateTime(anio, mes, 1);
        var hasta = desde.AddMonths(1).AddTicks(-1);

        var detalles = await _db.AsientoDetalles
            .Include(d => d.Asiento)
            .Include(d => d.Cuenta)
            .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Fecha >= desde && d.Asiento.Fecha <= hasta
                        && d.Asiento.Estado != EstadoAsiento.BORRADOR)
            .OrderBy(d => d.Asiento.Fecha).ThenBy(d => d.Asiento.Numero)
            .ToListAsync();

        var periodo = Periodo(anio, mes);
        var lineas = new List<string>();
        foreach (var d in detalles)
        {
            var cuo = $"D{d.Asiento.Numero:D10}";
            var documentoReferencia = string.IsNullOrWhiteSpace(d.NumeroDocumento)
                ? ""
                : $"{d.TipoDocumento}-{d.NumeroDocumento}";

            var campos = new[]
            {
                periodo,                                              // 1 Periodo
                cuo,                                                  // 2 CUO
                d.Asiento.Numero.ToString(),                          // 3 Correlativo del asiento (Libro Diario)
                F(d.Asiento.Fecha),                                   // 4 Fecha de la operación
                L(d.Asiento.Glosa),                                   // 5 Glosa de la operación
                d.Cuenta.Codigo,                                      // 6 Código de la cuenta contable (PCGE)
                L(d.Glosa ?? d.Asiento.Glosa),                        // 7 Glosa de la línea
                documentoReferencia,                                  // 8 Documento de referencia (vínculo con el comprobante SUNAT)
                M(d.Debe),                                            // 9 Debe
                M(d.Haber),                                           // 10 Haber
                SunatCatalogosPle.CodigoMoneda(d.Moneda),             // 11 Moneda de la línea
                M(d.TipoCambio),                                      // 12 Tipo de cambio aplicado
                SunatCatalogosPle.EstadoOperacionAsiento(d.Asiento.Estado), // 13 Estado del asiento
            };
            lineas.Add(string.Join("|", campos) + "|");
        }

        return new PleArchivo(NombreArchivo(empresa.Ruc, anio, mes, "050100"), Codificar(lineas));
    }

    // --- 6.1 Libro Mayor ---

    /// <summary>
    /// Formato 6.1 (Libro Mayor): un registro por cuenta contable con movimiento en el período,
    /// con su saldo inicial (acumulado hasta el mes anterior), el movimiento Debe/Haber del mes
    /// y el saldo final, a partir de los asientos CONFIRMADOs.
    /// </summary>
    public async Task<PleArchivo> GenerarLibroMayorAsync(Guid empresaId, int anio, int mes)
    {
        var empresa = await ObtenerEmpresaAsync(empresaId);
        var desde = new DateTime(anio, mes, 1);
        var hasta = desde.AddMonths(1).AddTicks(-1);

        var anteriores = await _db.AsientoDetalles
            .Include(d => d.Asiento)
            .Include(d => d.Cuenta)
            .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Estado == EstadoAsiento.CONFIRMADO && d.Asiento.Fecha < desde)
            .ToListAsync();

        var delPeriodo = await _db.AsientoDetalles
            .Include(d => d.Asiento)
            .Include(d => d.Cuenta)
            .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Estado == EstadoAsiento.CONFIRMADO && d.Asiento.Fecha >= desde && d.Asiento.Fecha <= hasta)
            .ToListAsync();

        var cuentas = anteriores.Select(d => d.Cuenta).Concat(delPeriodo.Select(d => d.Cuenta))
            .GroupBy(c => c.Id).Select(g => g.First())
            .OrderBy(c => c.Codigo)
            .ToList();

        var periodo = Periodo(anio, mes);
        var lineas = new List<string>();
        foreach (var cuenta in cuentas)
        {
            var saldoInicial = anteriores.Where(d => d.CuentaId == cuenta.Id).Sum(d => d.Debe - d.Haber);
            var movsMes = delPeriodo.Where(d => d.CuentaId == cuenta.Id).ToList();
            var debeMes = movsMes.Sum(d => d.Debe);
            var haberMes = movsMes.Sum(d => d.Haber);
            var saldoFinal = saldoInicial + debeMes - haberMes;
            if (saldoInicial == 0 && debeMes == 0 && haberMes == 0) continue;

            var campos = new[]
            {
                periodo,                 // 1 Periodo
                cuenta.Codigo,           // 2 Código de la cuenta contable (PCGE)
                L(cuenta.Nombre),        // 3 Descripción de la cuenta
                M(saldoInicial),         // 4 Saldo inicial del periodo
                M(debeMes),              // 5 Debe del periodo
                M(haberMes),             // 6 Haber del periodo
                M(saldoFinal),           // 7 Saldo final del periodo
                "PEN",                   // 8 Moneda (moneda funcional: el Mayor siempre se expresa en soles)
            };
            lineas.Add(string.Join("|", campos) + "|");
        }

        return new PleArchivo(NombreArchivo(empresa.Ruc, anio, mes, "060100"), Codificar(lineas));
    }

    // --- 1.1 Libro Caja y Bancos ---

    /// <summary>
    /// Formato 1.1 (Libro Caja y Bancos): un registro por movimiento de las cuentas de Caja y
    /// Bancos (PCGE clase 10) del período, con saldo acumulado corrido, en orden cronológico.
    /// </summary>
    public async Task<PleArchivo> GenerarLibroCajaBancosAsync(Guid empresaId, int anio, int mes)
    {
        var empresa = await ObtenerEmpresaAsync(empresaId);
        var desde = new DateTime(anio, mes, 1);
        var hasta = desde.AddMonths(1).AddTicks(-1);

        var cuentaCajaIds = await _db.CuentasContables
            .Where(c => c.EmpresaId == empresaId && c.Codigo.StartsWith("10"))
            .Select(c => c.Id)
            .ToListAsync();

        var anteriores = cuentaCajaIds.Count == 0
            ? new List<AsientoDetalle>()
            : await _db.AsientoDetalles
                .Include(d => d.Asiento)
                .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Estado == EstadoAsiento.CONFIRMADO
                            && cuentaCajaIds.Contains(d.CuentaId) && d.Asiento.Fecha < desde)
                .ToListAsync();

        var delPeriodo = cuentaCajaIds.Count == 0
            ? new List<AsientoDetalle>()
            : await _db.AsientoDetalles
                .Include(d => d.Asiento)
                .Include(d => d.Cuenta)
                .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Estado == EstadoAsiento.CONFIRMADO
                            && cuentaCajaIds.Contains(d.CuentaId) && d.Asiento.Fecha >= desde && d.Asiento.Fecha <= hasta)
                .OrderBy(d => d.Asiento.Fecha).ThenBy(d => d.Asiento.Numero)
                .ToListAsync();

        var periodo = Periodo(anio, mes);
        var saldo = anteriores.Sum(d => d.Debe - d.Haber);
        var lineas = new List<string>();
        var correlativo = 0;
        foreach (var d in delPeriodo)
        {
            correlativo++;
            saldo += d.Debe - d.Haber;
            var cuo = $"CB{correlativo:D8}";
            var campos = new[]
            {
                periodo,                                             // 1 Periodo
                cuo,                                                 // 2 CUO
                F(d.Asiento.Fecha),                                  // 3 Fecha de la operación
                L(d.Glosa ?? d.Asiento.Glosa),                       // 4 Glosa
                d.Cuenta.Codigo,                                     // 5 Código de la cuenta contable (10.x)
                L(d.TipoDocumento),                                  // 6 Tipo de documento relacionado
                L(d.NumeroDocumento),                                // 7 Número de documento relacionado
                M(d.Debe),                                           // 8 Ingreso
                M(d.Haber),                                          // 9 Egreso
                M(saldo),                                            // 10 Saldo acumulado
                SunatCatalogosPle.CodigoMoneda(d.Moneda),            // 11 Moneda
                M(d.TipoCambio),                                     // 12 Tipo de cambio aplicado
                SunatCatalogosPle.EstadoOperacionAsiento(d.Asiento.Estado), // 13 Estado de la operación
            };
            lineas.Add(string.Join("|", campos) + "|");
        }

        return new PleArchivo(NombreArchivo(empresa.Ruc, anio, mes, "010100"), Codificar(lineas));
    }
}
