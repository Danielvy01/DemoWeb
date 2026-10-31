using Siserp.Api.Models;

namespace Siserp.Api.Services.Ple;

/// <summary>
/// Tablas/catálogos de SUNAT usados para traducir los enums internos del ERP a los códigos
/// exigidos por el PLE (Programa de Libros Electrónicos) en los formatos de Registro de
/// Compras (8.1), Registro de Ventas e Ingresos (14.1), Libro Diario (5.1), Libro Mayor (6.1)
/// y Libro Caja y Bancos (1.1), según el Anexo de la R.S. N° 234-2006/SUNAT y modificatorias.
/// </summary>
public static class SunatCatalogosPle
{
    /// <summary>Tabla 10: Tipo de Comprobante de Pago o Documento (para el Registro de Compras).</summary>
    public static string TipoDocumentoCompra(TipoDocumentoCompra tipo) => tipo switch
    {
        Models.TipoDocumentoCompra.FACTURA => "01",
        Models.TipoDocumentoCompra.BOLETA => "03",
        Models.TipoDocumentoCompra.NOTA_CREDITO => "07",
        Models.TipoDocumentoCompra.NOTA_DEBITO => "08",
        Models.TipoDocumentoCompra.RECIBO_POR_HONORARIOS => "02",
        Models.TipoDocumentoCompra.GUIA_REMISION_REMITENTE => "09",
        Models.TipoDocumentoCompra.LIQUIDACION_COMPRA => "04",
        _ => "91", // Otros documentos (comprobantes no incluidos en las tablas anteriores)
    };

    /// <summary>Tabla 10: Tipo de Comprobante de Pago o Documento (para el Registro de Ventas).</summary>
    public static string TipoDocumentoVenta(TipoComprobante tipo) => tipo switch
    {
        TipoComprobante.FACTURA => "01",
        TipoComprobante.BOLETA => "03",
        TipoComprobante.NOTA_CREDITO => "07",
        TipoComprobante.NOTA_DEBITO => "08",
        TipoComprobante.NOTA_VENTA => "91",
        _ => "91",
    };

    /// <summary>Tabla 2: Tipo de Documento de Identidad.</summary>
    public static string TipoDocumentoIdentidad(TipoDocumentoIdentidad tipo) => tipo switch
    {
        Models.TipoDocumentoIdentidad.DNI => "1",
        Models.TipoDocumentoIdentidad.RUC => "6",
        Models.TipoDocumentoIdentidad.CE => "4",
        Models.TipoDocumentoIdentidad.PASAPORTE => "7",
        _ => "0",
    };

    /// <summary>Tabla 51: Tipo de Operación del Registro de Compras.</summary>
    public static string TipoOperacionCompra(TipoOperacionSunat tipo) => tipo switch
    {
        TipoOperacionSunat.COMPRA_INTERNA => "01",
        TipoOperacionSunat.IMPORTACION_BIENES => "02",
        TipoOperacionSunat.IMPORTACION_SERVICIOS => "03",
        TipoOperacionSunat.COMPRA_SUJETA_DETRACCION => "04",
        TipoOperacionSunat.COMPRA_SUJETA_PERCEPCION => "05",
        TipoOperacionSunat.COMPRA_SUJETA_RETENCION => "06",
        _ => "99",
    };

    /// <summary>Tabla 51: Tipo de Operación del Registro de Ventas.</summary>
    public static string TipoOperacionVenta(TipoOperacionVenta tipo) => tipo switch
    {
        Models.TipoOperacionVenta.VENTA_INTERNA => "01",
        Models.TipoOperacionVenta.EXPORTACION => "07",
        _ => "99",
    };

    /// <summary>
    /// Tabla 19: Estado que identifica la operación de un comprobante del Registro de Compras
    /// o Ventas ("1" Vigente/No anulado, "9" Anulado), derivado del estado interno del ERP.
    /// </summary>
    public static string EstadoOperacionComprobante(EstadoComprobante estado) =>
        estado is EstadoComprobante.ANULADO or EstadoComprobante.RECHAZADO ? "9" : "1";

    /// <summary>
    /// Estado de la operación de una compra, derivado del estado del asiento contable que la
    /// sustenta: "1" Vigente, "9" Anulada (cuando el asiento fue anulado, p. ej. al editar la
    /// compra se regenera el asiento y el anterior queda ANULADO).
    /// </summary>
    public static string EstadoOperacionAsiento(EstadoAsiento? estado) =>
        estado == EstadoAsiento.ANULADO ? "9" : "1";

    /// <summary>
    /// Código de moneda ISO 4217 (Tabla 4) tal cual se usa en el ERP ("PEN"/"USD"), que SUNAT
    /// acepta directamente en los 5 formatos de libros electrónicos.
    /// </summary>
    public static string CodigoMoneda(string moneda) => string.IsNullOrWhiteSpace(moneda) ? "PEN" : moneda.ToUpperInvariant();
}
