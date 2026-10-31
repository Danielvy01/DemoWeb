namespace Siserp.Api.Dtos;

public record LoginDto(string Email, string Password);

public record RegisterDto(Guid EmpresaId, string Email, string Nombre, string Password, string Rol);

public record CreateEmpresaDto(string Ruc, string RazonSocial, string? NombreComercial, string? Direccion, string? Ubigeo);

public record UpdateEmpresaDto(string? RazonSocial, string? NombreComercial, string? Direccion, string? Ubigeo);

public record CreateClienteDto(string TipoDocumento, string NumeroDocumento, string NombreORazonSocial, string? Direccion, string? Email, string? Telefono, int? DiasCredito, string? Clasificacion);

public record UpdateClienteDto(string? TipoDocumento, string? NumeroDocumento, string? NombreORazonSocial, string? Direccion, string? Email, string? Telefono, int? DiasCredito, string? Clasificacion);

public record CreateProductoDto(
    string Codigo, string Descripcion, string? UnidadMedida, decimal PrecioVenta, bool? AfectoIGV, decimal? StockMinimo,
    Guid? CategoriaId, decimal? DescuentoMayoristaPct, decimal? DescuentoSuperMayoristaPct,
    string? CodigoAlternativo = null, string? DescripcionAlternativa = null, string? MonedaCompra = null,
    decimal? PrecioCosto = null, string? TipoExistencia = null, decimal? StockMaximo = null, string? Observaciones = null,
    bool? EsServicio = null, decimal? PrecioLista = null, List<ComponenteProductoDto>? Componentes = null,
    Guid? CuentaVentaId = null, Guid? CuentaCostoVentaId = null, Guid? CuentaExistenciasId = null,
    string? CodigoBienServicio = null);

public record UpdateProductoDto(
    string? Codigo, string? Descripcion, string? UnidadMedida, decimal? PrecioVenta, bool? AfectoIGV, decimal? StockMinimo,
    Guid? CategoriaId, decimal? DescuentoMayoristaPct, decimal? DescuentoSuperMayoristaPct,
    string? CodigoAlternativo = null, string? DescripcionAlternativa = null, string? MonedaCompra = null,
    decimal? PrecioCosto = null, string? TipoExistencia = null, decimal? StockMaximo = null, string? Observaciones = null,
    bool? EsServicio = null, decimal? PrecioLista = null, List<ComponenteProductoDto>? Componentes = null,
    Guid? CuentaVentaId = null, Guid? CuentaCostoVentaId = null, Guid? CuentaExistenciasId = null,
    string? CodigoBienServicio = null);

/// <summary>Línea de la composición (BOM) de un producto terminado: insumo/servicio y cantidad requerida.</summary>
public record ComponenteProductoDto(Guid InsumoId, decimal Cantidad);

// --- Categorías (familia de producto) ---
public record CreateCategoriaDto(string Nombre);

public record UpdateCategoriaDto(string? Nombre);

// --- Centros de costo (áreas de la empresa: Operaciones, Ventas, Sistemas, etc.) ---
public record CreateCentroCostoDto(string Nombre, string? Descripcion);

public record UpdateCentroCostoDto(string? Nombre, string? Descripcion);

public record CreateAlmacenDto(string Nombre, string? Direccion, bool? Principal);

public record ItemComprobanteDto(Guid ProductoId, decimal Cantidad, decimal? PrecioUnitario = null);

/// <summary>Uno de los medios de pago con los que se cancela una venta al contado.</summary>
public record PagoComprobanteDto(string MetodoPago, decimal Monto, string? Referencia = null);

public record CreateComprobanteDto(
    string TipoComprobante, Guid ClienteId, Guid AlmacenId, string? Moneda, List<ItemComprobanteDto> Items,
    Guid? VendedorId = null, string? TipoOperacion = null, string? CondicionPago = null,
    DateTime? FechaVencimiento = null, List<PagoComprobanteDto>? Pagos = null, string? Observaciones = null,
    string? CanalVenta = null, Guid? CentroCostoId = null);

/// <summary>
/// <paramref name="AdminEmail"/>/<paramref name="AdminPassword"/>: código de administrador
/// (credenciales de un usuario con rol Administrador) exigido, al estilo CONCAR, para
/// autorizar la anulación de un comprobante ya emitido.
/// </summary>
public record AnularComprobanteDto(Guid? AlmacenId, string? Motivo, string? AdminEmail = null, string? AdminPassword = null);

/// <summary>
/// Datos para emitir una Nota de Crédito que afecta a un comprobante ya emitido.
/// <paramref name="AdminEmail"/>/<paramref name="AdminPassword"/>: código de administrador
/// exigido, al estilo CONCAR, para autorizar la emisión de la Nota de Crédito.
/// </summary>
public record CreateNotaCreditoDto(Guid AlmacenId, string Motivo, string? AdminEmail = null, string? AdminPassword = null, List<ItemComprobanteDto>? Items = null);

public record CreateProveedorDto(string Ruc, string RazonSocial, string? Direccion, string? Email, string? Telefono, int? DiasCredito);

public record UpdateProveedorDto(string? Ruc, string? RazonSocial, string? Direccion, string? Email, string? Telefono, int? DiasCredito);

/// <summary>
/// <paramref name="CuentaContableId"/>: cuenta contable del plan de cuentas a la que se
/// imputa este ítem (p. ej. Existencias/Mercaderías vs. Activo Fijo vs. Gasto/Servicios);
/// opcional, si no se indica se usa Existencias (o Servicios si el producto es servicio).
/// </summary>
public record CreateCompraDetalleDto(Guid ProductoId, decimal Cantidad, decimal PrecioUnitario, decimal? PorcentajeDescuento = null, Guid? CuentaContableId = null);

/// <summary>
/// Datos del comprobante de compra recibido del proveedor (catálogos SUNAT 01/51),
/// obligatorios para registrar la compra a partir de una orden de compra ya aprobada.
/// </summary>
/// <param name="ConfiguracionCuentaOperacionId">
/// Código de operación (al estilo CONCAR, p. ej. "FT. Recibidas M.N. Tercero") elegido
/// explícitamente en el formulario de registro de la compra; determina la cuenta por
/// pagar/caja completa y el subdiario del asiento generado. Opcional: si no se indica, se
/// busca automáticamente por (RECIBIDO, TipoDocumento, Moneda).
/// </param>
public record CreateCompraDto(
    Guid ProveedorId, Guid AlmacenId, Guid OrdenCompraId, Guid CentroCostoId, Guid EmpleadoCompradorId, string TipoDocumento, string? Serie, string Numero,
    DateTime FechaDocumento, DateTime? FechaVencimiento, string? NumeroGuiaRemision, string? Referencia,
    string? LocalProveedor, string TipoOperacionSunat, string? Moneda, List<CreateCompraDetalleDto> Detalles,
    decimal? TasaIgv = null, DateTime? FechaTipoCambio = null, string? GlosaComprobante = null, string? GlosaMovimiento = null,
    Guid? ConfiguracionCuentaOperacionId = null);

/// <summary>
/// Edición de una compra aún no pagada: permite corregir los datos del comprobante, el
/// centro de costo/comprador y reemplazar por completo la lista de ítems. Al guardar se
/// revierte el stock/Kardex previamente ingresado y se vuelve a ingresar con los datos nuevos.
/// </summary>
public record UpdateCompraDto(
    Guid AlmacenId, Guid CentroCostoId, Guid EmpleadoCompradorId, string TipoDocumento, string? Serie, string Numero,
    DateTime FechaDocumento, DateTime? FechaVencimiento, string? NumeroGuiaRemision, string? Referencia,
    string? LocalProveedor, string TipoOperacionSunat, string? Moneda, List<CreateCompraDetalleDto> Detalles,
    decimal? TasaIgv = null, DateTime? FechaTipoCambio = null, string? GlosaComprobante = null, string? GlosaMovimiento = null,
    Guid? ConfiguracionCuentaOperacionId = null);

/// <summary>
/// Anulación (baja lógica) de una compra registrada: revierte el stock/Kardex ingresado y
/// anula el asiento contable generado. <paramref name="AdminEmail"/>/<paramref name="AdminPassword"/>:
/// código de administrador exigido, al estilo CONCAR, para autorizar la anulación.
/// </summary>
public record AnularCompraDto(string? Motivo, string? AdminEmail = null, string? AdminPassword = null);

// --- Órdenes de compra ---
public record CreateOrdenCompraDetalleDto(Guid ProductoId, decimal Cantidad, decimal PrecioUnitario, Guid? CuentaContableId = null);

public record CreateOrdenCompraDto(
    Guid ProveedorId, string? Moneda, string? Observaciones, List<CreateOrdenCompraDetalleDto> Detalles, Guid? ComprobanteId,
    Guid? EmpleadoCompradorId = null, string? TipoEntrega = null, int? PlazoEntregaDias = null, DateTime? FechaEntregaEsperada = null);

/// <summary>
/// Edición de una orden de compra en estado BORRADOR: permite modificar sus
/// datos generales y reemplazar por completo la lista de ítems (agregar, quitar
/// o editar cantidades/precios de los detalles).
/// </summary>
public record UpdateOrdenCompraDto(
    Guid ProveedorId, string? Moneda, string? Observaciones, List<CreateOrdenCompraDetalleDto> Detalles, Guid? ComprobanteId,
    Guid? EmpleadoCompradorId = null, string? TipoEntrega = null, int? PlazoEntregaDias = null, DateTime? FechaEntregaEsperada = null);

public record RecibirOrdenCompraDetalleDto(Guid DetalleId, decimal CantidadRecibida, decimal? PorcentajeDescuento = null);

/// <summary>
/// Datos del comprobante de compra (catálogos SUNAT 01/51) con el que el proveedor
/// entrega la mercadería recepcionada, necesarios para generar la <see cref="Siserp.Api.Models.Compra"/>.
/// </summary>
public record RecibirOrdenCompraDto(
    Guid AlmacenId, Guid CentroCostoId, List<RecibirOrdenCompraDetalleDto> Detalles, string TipoDocumento, string? Serie, string Numero,
    DateTime FechaDocumento, DateTime? FechaVencimiento, string? NumeroGuiaRemision, string? Referencia,
    string? LocalProveedor, string TipoOperacionSunat, decimal? TasaIgv = null, DateTime? FechaTipoCambio = null,
    string? GlosaComprobante = null, string? GlosaMovimiento = null, Guid? EmpleadoCompradorId = null,
    Guid? ConfiguracionCuentaOperacionId = null);

// --- Tipo de cambio ---
public record CreateTipoCambioDto(DateTime Fecha, decimal Compra, decimal Venta);

// --- Órdenes de trabajo / producción ---
public record CreateOrdenTrabajoInsumoDto(Guid ProductoId, decimal CantidadRequerida);

public record CreateOrdenTrabajoDto(Guid? ProductoId, Guid AlmacenId, decimal CantidadPlanificada, string? Observaciones, List<CreateOrdenTrabajoInsumoDto>? Insumos, Guid? OrdenCompraId, Guid? PlantillaId);

public record CompletarOrdenTrabajoDto(decimal CantidadProducida);

// --- Plantillas de orden de trabajo (receta/BOM predefinida) ---
public record CreatePlantillaInsumoDto(Guid ProductoId, decimal CantidadPorUnidad);

public record CreatePlantillaOrdenTrabajoDto(string Nombre, Guid ProductoId, string? Observaciones, List<CreatePlantillaInsumoDto> Insumos);

public record UpdatePlantillaOrdenTrabajoDto(string? Nombre, string? Observaciones, bool? Activo, List<CreatePlantillaInsumoDto>? Insumos);

// --- Guías de remisión ---
public record CreateGuiaRemisionDetalleDto(Guid ProductoId, decimal Cantidad);

public record CreateGuiaRemisionDto(
    Guid AlmacenOrigenId, Guid? AlmacenDestinoId, Guid? ClienteId, Guid? ComprobanteId,
    string? DireccionPartida, string? DireccionLlegada, string? Transportista, string? PlacaVehiculo,
    string? MotivoTraslado, string? Observaciones, List<CreateGuiaRemisionDetalleDto> Detalles);

// --- Órdenes de salida de almacén ---
public record CreateOrdenSalidaAlmacenDetalleDto(Guid ProductoId, decimal Cantidad);

public record CreateOrdenSalidaAlmacenDto(Guid AlmacenId, string Motivo, string? Observaciones, List<CreateOrdenSalidaAlmacenDetalleDto> Detalles);

// --- Impresión / envío por correo de documentos ---
public record EnviarCorreoDto(string? Para, string? Asunto, string? Mensaje);

// --- Cotizaciones ---
public record CreateCotizacionDetalleDto(Guid ProductoId, decimal Cantidad, decimal PrecioUnitario);

public record CreateCotizacionDto(Guid ClienteId, DateTime? FechaValidez, string? Moneda, string? Observaciones, List<CreateCotizacionDetalleDto> Detalles);

public record ConvertirCotizacionDto(string TipoComprobante, Guid AlmacenId);

// --- Pedidos de venta (cotización → pedido → factura) ---
public record CreatePedidoVentaDetalleDto(Guid ProductoId, decimal Cantidad, decimal PrecioUnitario);

public record CreatePedidoVentaDto(Guid ClienteId, DateTime? FechaEntrega, string? Moneda, string? Observaciones, List<CreatePedidoVentaDetalleDto> Detalles);

public record ConvertirPedidoVentaDto(string TipoComprobante, Guid AlmacenId);

// --- Kardex / inventario móvil ---
public record AjusteInventarioDto(Guid ProductoId, Guid AlmacenId, decimal CantidadContada, string? Observacion);

// --- Notificaciones push (Web Push / VAPID) ---
public record SuscripcionPushDto(string Endpoint, string P256dh, string Auth, string? UserAgent);


// --- Marketing (campañas WhatsApp / correo) ---
public record CreateCampanaDto(string Nombre, string Canal, string? Asunto, string Mensaje, DateTime? FechaProgramada, List<Guid>? ClienteIds);

/// <summary>Reemplaza la lista de clientes destinatarios de una campaña aún no enviada.</summary>
public record UpdateClientesCampanaDto(List<Guid> ClienteIds);

// --- Contabilidad ---
public record CreateCuentaContableDto(string Codigo, string Nombre, string Tipo, string Naturaleza, Guid? CuentaPadreId, bool RequiereAnalisisDocumentario = false);

/// <summary>
/// Alta/edición de un "código de operación" (al estilo del catálogo de Tablas Maestras de
/// CONCAR, p. ej. "FT. Emitidas M.N. Tercero"): fija la cuenta contable COMPLETA y el
/// subdiario que debe usar la contabilización automática de Ventas (<paramref name="Movimiento"/>
/// = "EMITIDO") o Compras (<paramref name="Movimiento"/> = "RECIBIDO") para esa combinación
/// exacta de tipo de documento y moneda.
/// </summary>
/// <param name="Movimiento">"EMITIDO" (venta) o "RECIBIDO" (compra).</param>
/// <param name="TipoDocumento">P. ej. "FACTURA", "BOLETA" (debe coincidir con el tipo de comprobante/documento de compra correspondiente).</param>
/// <param name="Moneda">Código ISO de moneda, p. ej. "PEN" (M.N.) o "USD" (M.E.).</param>
/// <param name="CuentaContableId">Cuenta contable completa (no solo la clase) a usar como contrapartida de Cuentas por Cobrar/Pagar.</param>
/// <param name="Subdiario">Libro auxiliar del asiento generado: GENERAL, COMPRAS, VENTAS, CAJA_BANCOS, PLANILLA, ACTIVOS_FIJOS o APERTURA_CIERRE.</param>
/// <param name="Codigo">Código corto opcional, al estilo CONCAR (p. ej. "FT00").</param>
public record CreateConfiguracionCuentaOperacionDto(string Movimiento, string TipoDocumento, string Moneda, Guid CuentaContableId, string Subdiario, string? Codigo, string Descripcion, bool Activo = true);

/// <param name="ClienteId">Tercero (cliente) de la línea, cuando se imputa a una cuenta documentaria de Cuentas por Cobrar.</param>
/// <param name="ProveedorId">Tercero (proveedor) de la línea, cuando se imputa a una cuenta documentaria de Cuentas por Pagar.</param>
/// <param name="NumeroDocumento">Número del documento sustentatorio (factura/recibo/planilla) de esta línea.</param>
/// <param name="FechaVencimiento">Fecha de vencimiento del cobro/pago, si la cuenta es documentaria.</param>
/// <param name="Debe">Debe expresado en soles (moneda funcional). Si <paramref name="Moneda"/> es distinta de PEN, se recalcula como DebeMe * TipoCambio y este valor se ignora.</param>
/// <param name="Haber">Haber expresado en soles (moneda funcional); misma regla que <paramref name="Debe"/>.</param>
/// <param name="Moneda">Moneda de origen de la línea (PEN por defecto). Soporta asientos multimoneda al estilo CONCAR.</param>
/// <param name="TipoCambio">Tipo de cambio para convertir <paramref name="Moneda"/> a soles; si no se indica y la moneda no es PEN, se usa el tipo de cambio vigente a la fecha del asiento.</param>
/// <param name="DebeMe">Debe en su moneda de origen (<paramref name="Moneda"/>); igual a <paramref name="Debe"/> cuando la moneda es PEN.</param>
/// <param name="HaberMe">Haber en su moneda de origen (<paramref name="Moneda"/>); igual a <paramref name="Haber"/> cuando la moneda es PEN.</param>
public record CreateAsientoDetalleDto(
    Guid CuentaId, decimal Debe, decimal Haber, string? Glosa, Guid? CentroCostoId = null,
    Guid? ClienteId = null, Guid? ProveedorId = null, string? NumeroDocumento = null, DateTime? FechaVencimiento = null,
    string? TipoDocumento = null, string? Moneda = null, decimal? TipoCambio = null,
    decimal? DebeMe = null, decimal? HaberMe = null);

public record CreateAsientoDto(DateTime? Fecha, string Glosa, string? Referencia, List<CreateAsientoDetalleDto> Detalles);

// --- Provisiones (cuentas por pagar/cobrar documentarias, al estilo CONCAR) ---

/// <summary>
/// Registra una provisión: un asiento donde la contrapartida (gasto, activo, ingreso diferido,
/// etc.) se imputa contra una cuenta documentaria de control (<paramref name="CuentaControlId"/>,
/// p. ej. 42 - Cuentas por pagar comerciales), generando un <see cref="Models.DocumentoCtaCte"/>
/// pendiente de pago/cobro por el total de las contrapartidas.
/// </summary>
public record CreateProvisionDto(
    DateTime? Fecha, string Glosa, string Tipo /* CXC | CXP */, Guid CuentaControlId,
    string TipoDocumento, string? NumeroDocumento, DateTime FechaVencimiento,
    Guid? ClienteId, Guid? ProveedorId, Guid? CentroCostoId,
    List<CreateAsientoDetalleDto> Contrapartidas);

/// <summary>Cancela (total o parcialmente) un documento de cuenta corriente pendiente, registrando el asiento de pago/cobro contra la cuenta de caja/bancos indicada.</summary>
public record CancelarDocumentoCtaCteDto(DateTime? Fecha, decimal Monto, Guid CuentaCajaId, Guid? CentroCostoId, string? Glosa);

// --- Cierre y apertura de ejercicio contable ---

/// <summary>Abre un nuevo ejercicio contable (año fiscal); genera sus 12 periodos y, si corresponde, el asiento de apertura con los saldos arrastrados del ejercicio anterior.</summary>
public record AbrirEjercicioDto(int Anio);

// --- Presupuestos y Control (planificación por cuenta/centro de costo, comparativo Real vs. Presupuestado) ---

/// <summary>Crea un presupuesto anual (cabecera) en estado BORRADOR; sus líneas se agregan con <see cref="GuardarDetallesPresupuestoDto"/>.</summary>
public record CreatePresupuestoDto(string Nombre, int Anio);

/// <summary>Renombra un presupuesto (solo permitido mientras está en BORRADOR).</summary>
public record UpdatePresupuestoDto(string Nombre);

/// <param name="CuentaId">Cuenta contable presupuestada.</param>
/// <param name="CentroCostoId">Centro de costo al que se imputa la línea; null si el monto es a nivel de toda la empresa.</param>
/// <param name="Mes">Mes calendario presupuestado (1-12).</param>
/// <param name="Monto">Monto presupuestado para esa cuenta/centro de costo/mes.</param>
public record PresupuestoDetalleItemDto(Guid CuentaId, Guid? CentroCostoId, int Mes, decimal Monto, string? Glosa);

/// <summary>
/// Alta/edición masiva de líneas del presupuesto (al estilo de la grilla "cuenta x mes" de
/// CONCAR): cada línea se crea si no existe, o actualiza su monto si ya existía una para la misma
/// combinación de cuenta, centro de costo y mes.
/// </summary>
public record GuardarDetallesPresupuestoDto(List<PresupuestoDetalleItemDto> Detalles);

// --- Activos fijos / depreciación ---
public record CreateActivoFijoDto(
    string Codigo, string Nombre, string Categoria, DateTime FechaAdquisicion, decimal ValorAdquisicion,
    int VidaUtilMeses, Guid CuentaActivoId, Guid CuentaDepreciacionAcumuladaId, Guid CuentaGastoDepreciacionId,
    string? Descripcion = null, Guid? CentroCostoId = null, decimal? ValorResidual = null,
    DateTime? FechaInicioDepreciacion = null, string? MetodoDepreciacion = null);

public record UpdateActivoFijoDto(
    string? Nombre, string? Categoria, string? Descripcion, Guid? CentroCostoId,
    Guid? CuentaActivoId, Guid? CuentaDepreciacionAcumuladaId, Guid? CuentaGastoDepreciacionId);

public record DarBajaActivoFijoDto(DateTime Fecha, string Motivo, bool Vendido = false);

// --- Conciliación Bancaria ---

/// <summary>Calza manualmente un movimiento del extracto bancario contra una línea del Libro Caja y Bancos.</summary>
public record ConciliarManualDto(Guid AsientoDetalleId);


public record GenerarDepreciacionDto(string Periodo);

// --- RRHH / Planillas ---
public record CreateEmpleadoDto(string NumeroDocumento, string Nombres, string Apellidos, Guid CentroCostoId, string Cargo, decimal SalarioBase, string? Email, string? Telefono, string? CuentaBancaria, string? BancoNombre, string? CCI, DateTime? FechaIngreso, string? TipoContrato, string? RegimenPensionario, bool EsComprador = false);

public record UpdateEmpleadoDto(Guid? CentroCostoId, string? Cargo, decimal? SalarioBase, string? Email, string? Telefono, string? CuentaBancaria, string? BancoNombre, string? CCI, bool? Activo, string? TipoContrato, string? RegimenPensionario, bool? EsComprador);

public record DarBajaEmpleadoDto(string? Motivo, DateTime? Fecha);

/// <summary>Asigna un cargo adicional (o el principal) a un trabajador; <paramref name="EsPrincipal"/> reemplaza el cargo principal vigente.</summary>
public record AddCargoEmpleadoDto(string Cargo, bool EsPrincipal = false);

public record GenerarPlanillaDto(string Periodo);

public record AjustePlanillaDetalleDto(Guid EmpleadoId, decimal Bonificaciones, decimal Descuentos);

public record AjustarPlanillaDto(List<AjustePlanillaDetalleDto> Ajustes);

// --- Roles y permisos (seguridad granular por menú) ---
public record CreateRolDto(string Nombre, string? Descripcion);

public record UpdateRolDto(string? Nombre, string? Descripcion, bool? Activo);

public record PermisoMenuDto(Guid MenuId, bool Lectura, bool Escritura, bool ControlTotal);

public record SetPermisosRolDto(List<PermisoMenuDto> Permisos);

// --- Usuarios (maestro) ---
public record CreateUsuarioDto(string Email, string Nombre, string Password, List<Guid>? RolIds);

public record UpdateUsuarioDto(string? Nombre, string? Email, string? Password, bool? Activo);

// --- NIIF / Dinámica de Cuentas (plantillas configurables de reportes financieros y notas) ---

/// <summary>Crea una plantilla de reporte financiero (Balance General, Estado de Resultados o Nota a los EEFF) vacía, lista para armar sus secciones.</summary>
public record CreatePlantillaReporteDto(string Codigo, string Nombre, string Tipo, string? Descripcion, bool EsPredeterminada = false);

public record UpdatePlantillaReporteDto(string? Nombre, string? Descripcion, bool? EsPredeterminada, bool? Activa);

/// <param name="SeccionPadreId">Sección contenedora; null si es una línea/rubro raíz del reporte.</param>
/// <param name="Signo">+1/-1: cómo se acumula el total de esta sección dentro de su padre (o del total general del reporte si es raíz).</param>
/// <param name="EsResultadoDelEjercicio">Si es true, el total de la sección se calcula como Ingresos-Costos-Gastos acumulado a la fecha de corte, en vez de sumar cuentas/subsecciones (para insertar la utilidad del ejercicio dentro del Patrimonio).</param>
/// <param name="ReferenciaNota">Etiqueta de la nota NIIF que detalla esta línea (p. ej. "5"), solo informativa.</param>
/// <param name="PlantillaNotaVinculadaId">Plantilla de tipo NOTA_EEFF que desarrolla el detalle de esta línea.</param>
public record CreateSeccionReporteDto(
    Guid? SeccionPadreId, string Codigo, string Nombre, int Orden = 0, int Signo = 1,
    bool EsTotalDestacado = false, bool EsResultadoDelEjercicio = false,
    string? ReferenciaNota = null, Guid? PlantillaNotaVinculadaId = null);

public record UpdateSeccionReporteDto(
    string? Codigo, string? Nombre, int? Orden, int? Signo, bool? EsTotalDestacado,
    bool? EsResultadoDelEjercicio, string? ReferenciaNota, Guid? PlantillaNotaVinculadaId);

/// <summary>Mapea una cuenta puntual (<paramref name="CuentaId"/>) o un rango por prefijo de código (<paramref name="CodigoPrefijo"/>) hacia una sección del reporte: la "dinámica de cuentas" propiamente dicha.</summary>
public record CreateCuentaMapeadaDto(Guid? CuentaId, string? CodigoPrefijo, int Signo = 1);

/// <summary>Línea de cuenta ya resuelta dentro de un reporte financiero generado, con su importe calculado (saldo normalizado por naturaleza, multiplicado por el signo del mapeo).</summary>
public record CuentaReporteDto(Guid CuentaId, string Codigo, string Nombre, decimal Importe);

/// <summary>Sección ya calculada de un reporte financiero generado, con su total y el árbol de subsecciones.</summary>
public record SeccionReporteDto(
    Guid Id, string Codigo, string Nombre, int Signo, bool EsTotalDestacado,
    string? ReferenciaNota, Guid? PlantillaNotaVinculadaId,
    decimal Total, List<CuentaReporteDto> Cuentas, List<SeccionReporteDto> Hijas);

/// <summary>
/// Resultado de generar un reporte financiero a partir de una plantilla: árbol de secciones con
/// sus totales calculados a la fecha de corte, y <see cref="TotalGeneral"/> (suma de las secciones
/// raíz ponderadas por su signo) que para un Balance General debe ser 0 si todos los asientos
/// confirmados están correctamente cuadrados.
/// </summary>
public record ReporteFinancieroResultadoDto(
    Guid PlantillaId, string Codigo, string Nombre, string Tipo,
    DateTime? Desde, DateTime Hasta, List<SeccionReporteDto> Secciones, decimal TotalGeneral);

public record SetRolesUsuarioDto(List<Guid> RolIds);
