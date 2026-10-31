namespace Siserp.Api.Models;

public enum SunatModo
{
    LOCAL,
    BETA,
    PRODUCCION,
}

public enum RolUsuario
{
    ADMIN,
    CONTADOR,
    VENDEDOR,
    ALMACENERO,
}

public enum TipoDocumentoIdentidad
{
    DNI,
    RUC,
    CE,
    PASAPORTE,
    SIN_DOCUMENTO,
}

/// <summary>Clasificación comercial del cliente; determina qué descuento por producto se le aplica (Mayorista/Super Mayorista).</summary>
public enum ClasificacionCliente
{
    NORMAL,
    MAYORISTA,
    SUPER_MAYORISTA,
}

public enum TipoMovimiento
{
    ENTRADA,
    SALIDA,
    AJUSTE,
}

public enum TipoComprobante
{
    FACTURA,
    BOLETA,
    NOTA_CREDITO,
    NOTA_DEBITO,
    /// <summary>
    /// Nota de venta: comprobante interno de control de ventas que, a diferencia de la
    /// Factura/Boleta, NO se declara a SUNAT (no se envía al SunatSyncQueue).
    /// </summary>
    NOTA_VENTA,
}

/// <summary>Tipo de operación de la venta (similar al catálogo de compras, simplificado para ventas).</summary>
public enum TipoOperacionVenta
{
    VENTA_INTERNA,
    EXPORTACION,
    OTROS,
}

/// <summary>
/// Canal por el que se originó la venta, para el reporte gerencial "Canal de Ventas"
/// (ventas en el local físico frente a marketplaces/plataformas de delivery y ventas corporativas).
/// </summary>
public enum CanalVenta
{
    LOCAL,
    WEB,
    CORPORATIVO,
    PEDIDOS_YA,
    RAPPI,
}

/// <summary>Medio de pago usado para cancelar una venta al contado (puede haber varios por comprobante).</summary>
public enum MetodoPago
{
    EFECTIVO,
    TARJETA,
    TRANSFERENCIA,
    YAPE_PLIN,
    CHEQUE,
    OTRO,
}

/// <summary>Condición de pago pactada con el cliente para la venta.</summary>
public enum CondicionPago
{
    CONTADO,
    CREDITO,
}

/// <summary>
/// Tipo de documento (catálogo 01 de SUNAT) del comprobante recibido del proveedor
/// al registrar una compra.
/// </summary>
public enum TipoDocumentoCompra
{
    FACTURA,
    BOLETA,
    NOTA_CREDITO,
    NOTA_DEBITO,
    RECIBO_POR_HONORARIOS,
    GUIA_REMISION_REMITENTE,
    LIQUIDACION_COMPRA,
    OTROS,
}

/// <summary>
/// Tipo de operación (catálogo 51 de SUNAT) que clasifica la naturaleza de la compra
/// para el Registro de Compras Electrónico (RCE).
/// </summary>
public enum TipoOperacionSunat
{
    COMPRA_INTERNA,
    IMPORTACION_BIENES,
    IMPORTACION_SERVICIOS,
    COMPRA_SUJETA_DETRACCION,
    COMPRA_SUJETA_PERCEPCION,
    COMPRA_SUJETA_RETENCION,
    OTROS,
}

public enum EstadoComprobante
{
    BORRADOR,
    PENDIENTE_ENVIO,
    ENVIADO,
    ACEPTADO,
    ACEPTADO_CON_OBS,
    RECHAZADO,
    ERROR_ENVIO,
    ANULADO,
    /// <summary>Documento finalizado que no se declara a SUNAT (p. ej. Nota de venta).</summary>
    EMITIDO_LOCAL,
}

public enum EstadoCola
{
    PENDIENTE,
    PROCESANDO,
    COMPLETADO,
    FALLIDO,
}

// --- Órdenes de compra (solicitud a proveedor) ---
public enum EstadoOrdenCompra
{
    BORRADOR,
    ENVIADA,
    APROBADA,
    RECIBIDA_PARCIAL,
    RECIBIDA,
    CANCELADA,
}

/// <summary>Modalidad del plazo de entrega pactado con el proveedor en una orden de compra.</summary>
public enum TipoEntregaOrdenCompra
{
    /// <summary>El proveedor entrega la mercadería de inmediato (mismo día de la orden).</summary>
    INMEDIATA,
    /// <summary>El proveedor entrega a un plazo pactado en días (ver <see cref="OrdenCompra.PlazoEntregaDias"/>).</summary>
    A_DIAS,
}

// --- Órdenes de trabajo / producción ---
public enum EstadoOrdenTrabajo
{
    PLANIFICADA,
    EN_PROCESO,
    COMPLETADA,
    CANCELADA,
}

// --- Guías de remisión (traslado de mercadería) ---
public enum EstadoGuiaRemision
{
    BORRADOR,
    EMITIDA,
    ANULADA,
}

// --- Órdenes de salida de almacén (salidas distintas a la venta) ---
public enum EstadoOrdenSalidaAlmacen
{
    BORRADOR,
    CONFIRMADA,
    ANULADA,
}

// --- Cotizaciones (propuestas comerciales previas a la venta) ---
public enum EstadoCotizacion
{
    BORRADOR,
    ENVIADA,
    APROBADA,
    RECHAZADA,
    CONVERTIDA,
    VENCIDA,
    ANULADA,
}

// --- Pedidos de venta (paso intermedio entre cotización/venta y factura) ---
public enum EstadoPedidoVenta
{
    BORRADOR,
    CONFIRMADO,
    FACTURADO,
    ANULADO,
}

// --- Marketing (campañas WhatsApp / correo) ---
public enum CanalCampana
{
    EMAIL,
    WHATSAPP,
}

public enum EstadoCampana
{
    BORRADOR,
    PROGRAMADA,
    ENVIANDO,
    COMPLETADA,
    FALLIDA,
}

public enum EstadoMensajeCampana
{
    PENDIENTE,
    ENVIADO,
    FALLIDO,
}

// --- Contabilidad ---
public enum TipoCuentaContable
{
    ACTIVO,
    PASIVO,
    PATRIMONIO,
    INGRESO,
    GASTO,
    COSTO,
}

public enum NaturalezaCuenta
{
    DEUDORA,
    ACREEDORA,
}

public enum EstadoAsiento
{
    BORRADOR,
    CONFIRMADO,
    ANULADO,
}

/// <summary>
/// Subdiario (libro auxiliar) al que pertenece un <see cref="AsientoContable"/>, al estilo
/// CONCAR: permite listar/filtrar el Libro Diario por el origen de cada asiento (Registro de
/// Compras, Registro de Ventas, Libro Caja, etc.) en vez de verlo todo mezclado en un único
/// libro. Se asigna automáticamente según el proceso que generó el asiento.
/// </summary>
public enum TipoSubdiario
{
    /// <summary>Asientos manuales del Libro Diario sin un origen específico.</summary>
    GENERAL,
    /// <summary>Generado por el Registro de Compras (documentos recibidos del proveedor).</summary>
    COMPRAS,
    /// <summary>Generado por el Registro de Ventas (comprobantes emitidos al cliente).</summary>
    VENTAS,
    /// <summary>Movimientos de caja/bancos (cobros, pagos, pago de planilla).</summary>
    CAJA_BANCOS,
    /// <summary>Provisión/pago de planillas de remuneraciones.</summary>
    PLANILLA,
    /// <summary>Depreciación y demás movimientos de Activos Fijos.</summary>
    ACTIVOS_FIJOS,
    /// <summary>Asientos de apertura o cierre de ejercicio contable.</summary>
    APERTURA_CIERRE,
}

/// <summary>
/// Distingue si el documento (comprobante/compra) fue EMITIDO por la empresa (venta) o
/// RECIBIDO del proveedor (compra), al estilo del catálogo de "Códigos de Operación" de
/// CONCAR (p. ej. "FT. Emitidas M.N. Tercero" / "FT. Recibidas M.N. Tercero").
/// </summary>
public enum MovimientoOperacionContable
{
    EMITIDO,
    RECIBIDO,
}

/// <summary>
/// Tipo de evento registrado en el historial de auditoría de un <see cref="AsientoContable"/>
/// (al estilo CONCAR): toda la trazabilidad de quién hizo qué y cuándo sobre un asiento se
/// construye a partir de esta secuencia de eventos.
/// </summary>
public enum AccionHistorialAsiento
{
    /// <summary>Alta inicial del asiento (versión 1).</summary>
    CREACION,
    /// <summary>Edición de la cabecera y/o líneas de un asiento aún en BORRADOR.</summary>
    MODIFICACION,
    CONFIRMACION,
    ANULACION,
}

/// <summary>
/// Tipo de documento de cuenta corriente (análisis de cuentas al estilo CONCAR): indica si el
/// documento representa un derecho de cobro (Cuenta por Cobrar, p. ej. una factura de venta al
/// crédito) o una obligación de pago (Cuenta por Pagar, p. ej. una provisión de compra/gasto).
/// </summary>
public enum TipoDocumentoCtaCte
{
    CXC,
    CXP,
}

/// <summary>Estado de cobranza/pago de un documento de cuenta corriente, según cuánto se le haya aplicado.</summary>
public enum EstadoDocumentoCtaCte
{
    PENDIENTE,
    PARCIAL,
    CANCELADO,
    ANULADO,
}

/// <summary>
/// Estado de un <see cref="EjercicioContable"/> (año fiscal). Mientras está ABIERTO se pueden
/// registrar y confirmar asientos en cualquiera de sus periodos abiertos; al quedar CERRADO ya
/// no admite nuevos movimientos (el Asiento de Cierre dejó su resultado trasladado a Patrimonio).
/// </summary>
public enum EstadoEjercicioContable
{
    ABIERTO,
    CERRADO,
}

/// <summary>
/// Estado de un <see cref="PeriodoContable"/> (mes calendario dentro de un ejercicio), al estilo
/// del bloqueo de periodos de CONCAR: con el periodo CERRADO no se pueden crear, confirmar ni
/// anular asientos cuya fecha caiga dentro de ese mes (evita modificar meses ya reportados/declarados).
/// </summary>
public enum EstadoPeriodoContable
{
    ABIERTO,
    CERRADO,
}

/// <summary>
/// Estado de un <see cref="PresupuestoAnual"/> (al estilo del módulo de Presupuestos y Control de
/// CONCAR). Mientras está BORRADOR se pueden agregar, editar o eliminar líneas libremente; al
/// quedar APROBADO el presupuesto queda fijo (de solo lectura) y sirve de base estable para el
/// reporte de ejecución Real vs. Presupuestado.
/// </summary>
public enum EstadoPresupuesto
{
    BORRADOR,
    APROBADO,
}

/// <summary>
/// Tipo de reporte financiero que construye una <see cref="Models.ReporteFinancieroPlantilla"/>
/// (módulo "NIIF / Dinámica de Cuentas", configurable por el usuario a diferencia del PCGE y del
/// Balance General/Estado de Resultados que trae el sistema fijos en código): define qué forma
/// de estado financiero u nota representa la plantilla.
/// </summary>
public enum TipoReporteFinanciero
{
    /// <summary>Estado de Situación Financiera (Balance General).</summary>
    BALANCE_GENERAL,
    /// <summary>Estado de Resultados (Ganancias y Pérdidas).</summary>
    ESTADO_RESULTADOS,
    /// <summary>Nota a los Estados Financieros NIIF (p. ej. "Nota 1 - Efectivo y Equivalentes de Efectivo").</summary>
    NOTA_EEFF,
    /// <summary>Cualquier otro reporte dinámico armado libremente por el usuario a partir del plan de cuentas.</summary>
    OTRO,
}

// --- RRHH / Planillas ---
public enum EstadoPlanilla
{
    BORRADOR,
    CALCULADA,
    APROBADA,
    SINCRONIZADA,
    PAGADA,
}

public enum EstadoPagoPlanilla
{
    PENDIENTE,
    SINCRONIZADO,
    PAGADO,
    ERROR,
}

// --- Activos fijos / depreciación ---

/// <summary>Método de cálculo de la depreciación. Por ahora solo línea recta (el más usado en Perú para la mayoría de activos según la Ley del IR).</summary>
public enum MetodoDepreciacion
{
    LINEA_RECTA,
}

/// <summary>Situación actual del activo fijo dentro de su ciclo de vida contable.</summary>
public enum EstadoActivoFijo
{
    ACTIVO,
    TOTALMENTE_DEPRECIADO,
    DADO_DE_BAJA,
    VENDIDO,
}
