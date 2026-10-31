using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

/// <summary>Plan de cuentas contables (catálogo jerárquico) de la empresa.</summary>
public class CuentaContable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(20)] public string Codigo { get; set; } = null!;
    [MaxLength(255)] public string Nombre { get; set; } = null!;
    public TipoCuentaContable Tipo { get; set; }
    public NaturalezaCuenta Naturaleza { get; set; }
    public Guid? CuentaPadreId { get; set; }
    public CuentaContable? CuentaPadre { get; set; }
    public bool Activa { get; set; } = true;
    /// <summary>
    /// Marca las cuentas "de enlace" o "documentarias" (al estilo CONCAR), típicamente Cuentas
    /// por Cobrar Comerciales (12) y Cuentas por Pagar Comerciales (42): cuando un asiento mueve
    /// una de estas cuentas con un tercero y número de documento, el sistema crea automáticamente
    /// un <see cref="DocumentoCtaCte"/> para el módulo de Análisis de Cuentas / Provisiones.
    /// </summary>
    public bool RequiereAnalisisDocumentario { get; set; }

    public ICollection<AsientoDetalle> Movimientos { get; set; } = new List<AsientoDetalle>();
}

/// <summary>
/// Configuración del "código de operación" contable por tipo de documento y moneda (al estilo
/// del catálogo de Tablas Maestras de CONCAR, p. ej. "FT. Emitidas M.N. Tercero" / "FT.
/// Recibidas M.E. Tercero"): define, para cada combinación de <see cref="Movimiento"/> (emitido
/// = venta / recibido = compra), <see cref="TipoDocumento"/> y <see cref="Moneda"/>, la cuenta
/// contable COMPLETA y específica (no solo la clase/prefijo, p. ej. "1212" en vez de solo "12")
/// que debe usar la contabilización automática de Ventas/Compras, y el <see cref="Subdiario"/>
/// (libro auxiliar) al que pertenece el asiento generado. Si no existe configuración para una
/// combinación dada, <see cref="Services.ContabilidadService"/> conserva el comportamiento
/// anterior (primera cuenta activa cuyo código empiece con la clase genérica 10/12/40/42).
/// </summary>
public class ConfiguracionCuentaOperacion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public MovimientoOperacionContable Movimiento { get; set; }
    /// <summary>Tipo de documento (p. ej. "FACTURA", "BOLETA"): coincide con el nombre de <see cref="TipoComprobante"/> (ventas) o <see cref="TipoDocumentoCompra"/> (compras).</summary>
    [MaxLength(50)] public string TipoDocumento { get; set; } = null!;
    /// <summary>Código de moneda ISO (p. ej. "PEN" = M.N., "USD" = M.E.).</summary>
    [MaxLength(5)] public string Moneda { get; set; } = "PEN";
    /// <summary>Cuenta contable completa y específica (no solo la clase) a imputar como contrapartida (Cuentas por Cobrar/Pagar) de esta operación.</summary>
    public Guid CuentaContableId { get; set; }
    public CuentaContable CuentaContable { get; set; } = null!;
    public TipoSubdiario Subdiario { get; set; } = TipoSubdiario.GENERAL;
    /// <summary>Código corto opcional, al estilo CONCAR (p. ej. "FT00").</summary>
    [MaxLength(20)] public string? Codigo { get; set; }
    /// <summary>Descripción del código de operación (p. ej. "FT. Emitidas M.N. Tercero").</summary>
    [MaxLength(255)] public string Descripcion { get; set; } = null!;
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Asiento del Libro Diario. Cada asiento agrupa líneas de debe/haber
/// (<see cref="AsientoDetalle"/>) que deben sumar lo mismo (partida doble).
/// El Libro Mayor y el Estado de Resultados se derivan por reporte a partir
/// de los asientos CONFIRMADOs.
/// </summary>
public class AsientoContable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public int Numero { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string Glosa { get; set; } = null!;
    [MaxLength(255)] public string? Referencia { get; set; }
    public EstadoAsiento Estado { get; set; } = EstadoAsiento.BORRADOR;
    /// <summary>
    /// Subdiario (libro auxiliar) al que pertenece el asiento (Compras, Ventas, Caja/Bancos,
    /// Planilla, etc.), al estilo CONCAR. Se asigna automáticamente según el proceso que generó
    /// el asiento; <see cref="TipoSubdiario.GENERAL"/> para asientos manuales del Libro Diario.
    /// </summary>
    public TipoSubdiario Subdiario { get; set; } = TipoSubdiario.GENERAL;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Número de versión actual del asiento (auditoría/trazabilidad al estilo CONCAR): empieza en
    /// 1 al crearse y se incrementa en cada evento registrado en <see cref="Historial"/> (edición,
    /// confirmación, anulación), de modo que cada fila del historial corresponde a una versión.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>Usuario que registró el asiento (null si proviene de un proceso automático, p. ej. la contabilización de una venta/compra).</summary>
    public Guid? UsuarioCreacionId { get; set; }
    public Usuario? UsuarioCreacion { get; set; }
    /// <summary>Usuario responsable del último cambio (edición, confirmación o anulación) sobre el asiento.</summary>
    public Guid? UsuarioModificacionId { get; set; }
    public Usuario? UsuarioModificacion { get; set; }

    public ICollection<AsientoDetalle> Detalles { get; set; } = new List<AsientoDetalle>();
    /// <summary>Pista de auditoría completa del asiento: quién, cuándo y qué cambió en cada versión.</summary>
    public ICollection<AsientoContableHistorial> Historial { get; set; } = new List<AsientoContableHistorial>();
}

/// <summary>
/// Evento de auditoría/trazabilidad sobre un <see cref="AsientoContable"/> (módulo "Auditoría y
/// Trazabilidad" al estilo CONCAR): registra quién realizó el cambio, cuándo, qué acción fue
/// (creación/edición/confirmación/anulación) y una fotografía (JSON) del asiento completo -cabecera
/// y líneas- tal como quedó antes y después del evento, de modo que cualquier versión anterior del
/// asiento se pueda reconstruir e inspeccionar sin depender de que ese dato siga vigente hoy.
/// </summary>
public class AsientoContableHistorial
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AsientoId { get; set; }
    public AsientoContable Asiento { get; set; } = null!;
    /// <summary>Versión del asiento que resultó de este evento (coincide con <see cref="AsientoContable.Version"/> al momento de registrarlo).</summary>
    public int Version { get; set; }
    public AccionHistorialAsiento Accion { get; set; }
    /// <summary>Usuario que ejecutó la acción; null si fue un proceso automático del sistema.</summary>
    public Guid? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    /// <summary>Estado del asiento antes del evento; null en la CREACION (no existía versión previa).</summary>
    public EstadoAsiento? EstadoAnterior { get; set; }
    public EstadoAsiento EstadoNuevo { get; set; }
    /// <summary>Fotografía (JSON) del asiento -cabecera y líneas- tal como estaba ANTES de este evento; null en la CREACION.</summary>
    public string? SnapshotAnterior { get; set; }
    /// <summary>Fotografía (JSON) del asiento -cabecera y líneas- tal como quedó DESPUÉS de este evento.</summary>
    public string SnapshotNuevo { get; set; } = null!;
    [MaxLength(500)] public string? Observacion { get; set; }
}

public class AsientoDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AsientoId { get; set; }
    public AsientoContable Asiento { get; set; } = null!;
    public Guid CuentaId { get; set; }
    public CuentaContable Cuenta { get; set; } = null!;
    /// <summary>Debe expresado en la moneda funcional (soles), usado por el Libro Mayor, Balance General y Estado de Resultados.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal Debe { get; set; }
    /// <summary>Haber expresado en la moneda funcional (soles), usado por el Libro Mayor, Balance General y Estado de Resultados.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal Haber { get; set; }
    [MaxLength(500)] public string? Glosa { get; set; }

    /// <summary>
    /// Moneda de origen de esta línea (código ISO, p. ej. PEN/USD), al estilo de la doble
    /// moneda de CONCAR: las operaciones pueden pactarse en moneda extranjera, pero el Libro
    /// Mayor y los estados financieros siempre se presentan en la moneda funcional (soles).
    /// </summary>
    [MaxLength(5)] public string Moneda { get; set; } = "PEN";
    /// <summary>Tipo de cambio aplicado para convertir el importe de <see cref="Moneda"/> a soles (1 cuando la línea ya está en PEN).</summary>
    [Column(TypeName = "decimal(8,4)")] public decimal TipoCambio { get; set; } = 1m;
    /// <summary>Debe en su moneda de origen (<see cref="Moneda"/>), antes de aplicar el tipo de cambio; igual a <see cref="Debe"/> cuando la moneda es PEN.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal DebeMe { get; set; }
    /// <summary>Haber en su moneda de origen (<see cref="Moneda"/>), antes de aplicar el tipo de cambio; igual a <see cref="Haber"/> cuando la moneda es PEN.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal HaberMe { get; set; }
    /// <summary>Centro de costo/área al que se imputa esta línea del asiento (opcional); habilita reportes y el Libro Mayor segmentados por centro de costo, al estilo CONCAR.</summary>
    public Guid? CentroCostoId { get; set; }
    public CentroCosto? CentroCosto { get; set; }

    /// <summary>Cliente asociado a esta línea (solo si la cuenta es documentaria, p. ej. 12 - Cuentas por cobrar).</summary>
    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    /// <summary>Proveedor asociado a esta línea (solo si la cuenta es documentaria, p. ej. 42 - Cuentas por pagar).</summary>
    public Guid? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    /// <summary>Número del documento sustentatorio (factura, recibo, planilla, etc.) de esta línea, usado en el Análisis de Cuentas.</summary>
    [MaxLength(50)] public string? NumeroDocumento { get; set; }
    /// <summary>Tipo del documento sustentatorio (FACTURA, BOLETA, PLANILLA, etc.), usado para clasificar el <see cref="DocumentoCtaCte"/> generado; "ASIENTO" si no se indica.</summary>
    [MaxLength(50)] public string? TipoDocumento { get; set; }
    /// <summary>Fecha de vencimiento pactada para esta línea, cuando corresponde a una cuenta documentaria (cobro/pago pendiente).</summary>
    public DateTime? FechaVencimiento { get; set; }

    /// <summary>Documento de cuenta corriente (cobranza/pago pendiente) generado a partir de esta línea, si su cuenta lo requiere.</summary>
    public DocumentoCtaCte? DocumentoCtaCte { get; set; }
}

/// <summary>
/// Documento de cuenta corriente (Análisis de Cuentas, al estilo CONCAR): representa el saldo
/// pendiente de cobro (CXC) o pago (CXP) de una línea de asiento imputada a una cuenta
/// "documentaria" (<see cref="CuentaContable.RequiereAnalisisDocumentario"/>), con su tercero,
/// vencimiento y aplicaciones (cobros/pagos) parciales o totales. Las provisiones (módulo
/// Provisiones) son el caso típico de generación: una compra/gasto/planilla por pagar que crea
/// un documento CXP pendiente, el cual luego se cancela (total o parcialmente) con un pago.
/// </summary>
public class DocumentoCtaCte
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Guid AsientoDetalleId { get; set; }
    public AsientoDetalle AsientoDetalle { get; set; } = null!;
    public Guid CuentaId { get; set; }
    public CuentaContable Cuenta { get; set; } = null!;
    public TipoDocumentoCtaCte Tipo { get; set; }
    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public Guid? ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    [MaxLength(50)] public string TipoDocumento { get; set; } = "OTROS";
    [MaxLength(50)] public string? NumeroDocumento { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateTime FechaVencimiento { get; set; }
    [MaxLength(500)] public string? Glosa { get; set; }
    public Guid? CentroCostoId { get; set; }
    public CentroCosto? CentroCosto { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal MontoOriginal { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal MontoCancelado { get; set; }
    public EstadoDocumentoCtaCte Estado { get; set; } = EstadoDocumentoCtaCte.PENDIENTE;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AplicacionDocumentoCtaCte> Aplicaciones { get; set; } = new List<AplicacionDocumentoCtaCte>();

    [NotMapped] public decimal SaldoPendiente => MontoOriginal - MontoCancelado;
}

/// <summary>
/// Ejercicio contable (año fiscal), al estilo CONCAR: agrupa los 12 <see cref="PeriodoContable"/>
/// (meses) del año y controla su apertura/cierre. Al abrir un ejercicio, si existe un ejercicio
/// anterior ya CERRADO, se genera automáticamente el <see cref="AsientoApertura"/> que traslada
/// (arrastra) los saldos de Activo/Pasivo/Patrimonio de ese ejercicio anterior como saldos
/// iniciales del nuevo año (fecha 1 de enero). Al cerrarlo, se genera el <see cref="AsientoCierre"/>
/// que cancela las cuentas de Ingresos/Costos/Gastos contra Resultados del Ejercicio (Patrimonio),
/// dejando el <see cref="Resultado"/> (utilidad o pérdida) listo para arrastrarse al abrir el
/// siguiente ejercicio.
/// </summary>
public class EjercicioContable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    /// <summary>Año fiscal (p. ej. 2026); único por empresa.</summary>
    public int Anio { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public EstadoEjercicioContable Estado { get; set; } = EstadoEjercicioContable.ABIERTO;
    /// <summary>Asiento de apertura (saldos iniciales arrastrados del ejercicio anterior), null si es el primer ejercicio registrado en el sistema.</summary>
    public Guid? AsientoAperturaId { get; set; }
    public AsientoContable? AsientoApertura { get; set; }
    /// <summary>Asiento de cierre (cancelación de cuentas de resultado contra Patrimonio), se genera al cerrar el ejercicio.</summary>
    public Guid? AsientoCierreId { get; set; }
    public AsientoContable? AsientoCierre { get; set; }
    /// <summary>Resultado del ejercicio (Ingresos - Costos - Gastos) calculado al momento del cierre; positivo = utilidad, negativo = pérdida.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal? Resultado { get; set; }
    public DateTime? FechaCierre { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PeriodoContable> Periodos { get; set; } = new List<PeriodoContable>();
}

/// <summary>
/// Periodo contable mensual dentro de un <see cref="EjercicioContable"/>, al estilo del bloqueo
/// de periodos de CONCAR: mientras está ABIERTO admite asientos con fecha dentro de ese mes;
/// una vez CERRADO (manualmente, o automáticamente al cerrar el ejercicio) ya no se pueden crear,
/// confirmar ni anular asientos con fecha en ese mes, protegiendo la información ya reportada
/// (declaraciones SUNAT, estados financieros entregados, etc.).
/// </summary>
public class PeriodoContable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Guid EjercicioId { get; set; }
    public EjercicioContable Ejercicio { get; set; } = null!;
    public int Anio { get; set; }
    /// <summary>Mes calendario (1-12).</summary>
    public int Mes { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public EstadoPeriodoContable Estado { get; set; } = EstadoPeriodoContable.ABIERTO;
    public DateTime? FechaCierre { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// "Cruce" o aplicación de un pago/cobro contra un <see cref="DocumentoCtaCte"/> pendiente
/// (puede haber varias aplicaciones parciales hasta cancelar el documento). Cada aplicación
/// queda ligada al asiento contable que registró el movimiento de caja/bancos correspondiente.
/// </summary>
public class AplicacionDocumentoCtaCte
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentoCtaCteId { get; set; }
    public DocumentoCtaCte DocumentoCtaCte { get; set; } = null!;
    public Guid AsientoId { get; set; }
    public AsientoContable Asiento { get; set; } = null!;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(12,2)")] public decimal Monto { get; set; }
    [MaxLength(500)] public string? Glosa { get; set; }
}

/// <summary>
/// Presupuesto anual (módulo "Presupuestos y Control" al estilo CONCAR): planifica, mes a mes,
/// el monto esperado para cada cuenta contable (y opcionalmente centro de costo) del año. Sirve
/// de base para el reporte de ejecución Real vs. Presupuestado (<see cref="PresupuestoDetalle"/>
/// contiene las líneas mensuales). Puede existir más de un presupuesto por año (p. ej. una versión
/// "Inicial" y una "Reformulado"); el reporte comparativo permite elegir cuál usar.
/// </summary>
public class PresupuestoAnual
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(255)] public string Nombre { get; set; } = null!;
    /// <summary>Año fiscal presupuestado (p. ej. 2026).</summary>
    public int Anio { get; set; }
    /// <summary>
    /// BORRADOR admite altas/bajas/modificaciones de líneas; APROBADO lo deja fijo como línea
    /// base estable para el comparativo Real vs. Presupuestado.
    /// </summary>
    public EstadoPresupuesto Estado { get; set; } = EstadoPresupuesto.BORRADOR;
    public DateTime? FechaAprobacion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PresupuestoDetalle> Detalles { get; set; } = new List<PresupuestoDetalle>();
}

/// <summary>
/// Línea de un <see cref="PresupuestoAnual"/>: monto planificado para una cuenta contable (y,
/// opcionalmente, un centro de costo) en un mes concreto del año presupuestado. Una misma cuenta
/// puede tener hasta 12 líneas (una por mes) dentro del mismo presupuesto y centro de costo.
/// </summary>
public class PresupuestoDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PresupuestoId { get; set; }
    public PresupuestoAnual Presupuesto { get; set; } = null!;
    public Guid CuentaId { get; set; }
    public CuentaContable Cuenta { get; set; } = null!;
    /// <summary>Centro de costo al que se imputa esta línea; null si el presupuesto de esa cuenta es a nivel de toda la empresa.</summary>
    public Guid? CentroCostoId { get; set; }
    public CentroCosto? CentroCosto { get; set; }
    /// <summary>Mes calendario presupuestado (1-12).</summary>
    public int Mes { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Monto { get; set; }
    [MaxLength(500)] public string? Glosa { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
