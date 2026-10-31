using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

public class Empresa
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(20)] public string Ruc { get; set; } = null!;
    [MaxLength(255)] public string RazonSocial { get; set; } = null!;
    [MaxLength(255)] public string? NombreComercial { get; set; }
    [MaxLength(255)] public string? Direccion { get; set; }
    [MaxLength(10)] public string? Ubigeo { get; set; }
    /// <summary>Ruta relativa (bajo wwwroot) del logo de la empresa, usado en la cabecera de los PDF generados.</summary>
    [MaxLength(255)] public string? LogoUrl { get; set; }
    public SunatModo ModoSunat { get; set; } = SunatModo.LOCAL;
    public string? SolUsuario { get; set; }
    public string? SolClaveEnc { get; set; }
    public string? CertPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    public ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();
    public ICollection<Proveedor> Proveedores { get; set; } = new List<Proveedor>();
    public ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    public ICollection<Almacen> Almacenes { get; set; } = new List<Almacen>();
    public ICollection<SerieComprobante> Series { get; set; } = new List<SerieComprobante>();
    public ICollection<Comprobante> Comprobantes { get; set; } = new List<Comprobante>();
    public ICollection<Compra> Compras { get; set; } = new List<Compra>();
    public ICollection<OrdenTrabajo> OrdenesTrabajo { get; set; } = new List<OrdenTrabajo>();
    public ICollection<CampanaMarketing> Campanas { get; set; } = new List<CampanaMarketing>();
    public ICollection<CuentaContable> CuentasContables { get; set; } = new List<CuentaContable>();
    public ICollection<AsientoContable> AsientosContables { get; set; } = new List<AsientoContable>();
    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
    public ICollection<Planilla> Planillas { get; set; } = new List<Planilla>();
    public ICollection<Rol> Roles { get; set; } = new List<Rol>();
}

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(255)] public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    [MaxLength(255)] public string Nombre { get; set; } = null!;
    public RolUsuario Rol { get; set; } = RolUsuario.VENDEDOR;
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Roles de seguridad asignados al usuario (ver <see cref="UsuarioRol"/>). Un usuario puede
    /// tener varios roles a la vez; su acceso efectivo a cada menú es el máximo nivel entre todos
    /// ellos. El campo <see cref="Rol"/> (enum legado) se mantiene solo para compatibilidad.
    /// </summary>
    public ICollection<UsuarioRol> UsuarioRoles { get; set; } = new List<UsuarioRol>();
}

public class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public TipoDocumentoIdentidad TipoDocumento { get; set; } = TipoDocumentoIdentidad.DNI;
    [MaxLength(20)] public string NumeroDocumento { get; set; } = null!;
    [MaxLength(255)] public string NombreORazonSocial { get; set; } = null!;
    [MaxLength(255)] public string? Direccion { get; set; }
    [MaxLength(255)] public string? Email { get; set; }
    [MaxLength(30)] public string? Telefono { get; set; }
    /// <summary>Días de crédito otorgados al cliente; se usa para calcular la fecha de vencimiento de sus comprobantes por cobrar.</summary>
    public int DiasCredito { get; set; } = 0;
    /// <summary>Clasificación comercial del cliente; define el descuento por producto (Mayorista/Super Mayorista) que se le aplica en ventas.</summary>
    public ClasificacionCliente Clasificacion { get; set; } = ClasificacionCliente.NORMAL;
    /// <summary>Inactivación del cliente (baja lógica), distinta del bloqueo temporal.</summary>
    public bool Activo { get; set; } = true;
    /// <summary>Bloqueo temporal (p. ej. por mora); impide emitir nuevas ventas al cliente sin darlo de baja.</summary>
    public bool Bloqueado { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Comprobante> Comprobantes { get; set; } = new List<Comprobante>();
    public ICollection<Cotizacion> Cotizaciones { get; set; } = new List<Cotizacion>();
}

public class Proveedor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(20)] public string Ruc { get; set; } = null!;
    [MaxLength(255)] public string RazonSocial { get; set; } = null!;
    [MaxLength(255)] public string? Direccion { get; set; }
    [MaxLength(255)] public string? Email { get; set; }
    [MaxLength(30)] public string? Telefono { get; set; }
    /// <summary>Días de crédito que otorga el proveedor; se usa para calcular la fecha de vencimiento de las cuentas por pagar.</summary>
    public int DiasCredito { get; set; } = 0;
    /// <summary>Inactivación del proveedor (baja lógica), distinta del bloqueo temporal.</summary>
    public bool Activo { get; set; } = true;
    /// <summary>Bloqueo temporal; impide registrar nuevas compras/órdenes de compra al proveedor sin darlo de baja.</summary>
    public bool Bloqueado { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Compra> Compras { get; set; } = new List<Compra>();
    public ICollection<OrdenCompra> OrdenesCompra { get; set; } = new List<OrdenCompra>();
}

/// <summary>Categoría de familia usada para agrupar/clasificar productos (p. ej. "Bebidas", "Abarrotes").</summary>
public class Categoria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(255)] public string Nombre { get; set; } = null!;
    /// <summary>Baja lógica de la categoría; se conserva el histórico de productos ya asociados.</summary>
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}

public class Producto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Guid? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    [MaxLength(50)] public string Codigo { get; set; } = null!;
    /// <summary>Código adicional/interno (p. ej. del fabricante o de un sistema anterior); opcional.</summary>
    [MaxLength(50)] public string? CodigoAlternativo { get; set; }
    [MaxLength(500)] public string Descripcion { get; set; } = null!;
    /// <summary>Descripción adicional (p. ej. nombre comercial o de catálogo del proveedor); opcional.</summary>
    [MaxLength(500)] public string? DescripcionAlternativa { get; set; }
    [MaxLength(10)] public string UnidadMedida { get; set; } = "NIU";
    /// <summary>Precio de venta al público.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal PrecioVenta { get; set; }
    /// <summary>
    /// Precio de lista (catálogo), de referencia. Puede diferir del <see cref="PrecioVenta"/>
    /// real que se cobra (p. ej. por promociones); se muestra junto al precio de venta al
    /// agregar el producto en una venta. Si no se registra, se asume igual al precio de venta.
    /// </summary>
    [Column(TypeName = "decimal(12,2)")] public decimal? PrecioLista { get; set; }
    /// <summary>Precio de costo (compra); es el valor que se usa para la contabilización de existencias/costo de ventas.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal PrecioCosto { get; set; }
    /// <summary>Moneda en la que se compra habitualmente el producto (código ISO, p. ej. PEN/USD).</summary>
    [MaxLength(5)] public string MonedaCompra { get; set; } = "PEN";
    public bool AfectoIGV { get; set; } = true;
    /// <summary>Tipo de existencia según el Catálogo N° 09 de SUNAT (01 Mercaderías, 02 Productos terminados, 03 Materias primas, etc.).</summary>
    [MaxLength(2)] public string TipoExistencia { get; set; } = "01";
    /// <summary>
    /// Código de bien o servicio (Tabla 17 "Clasificación de Bienes y Servicios" de SUNAT, o el
    /// código UNSPSC/catálogo interno que la empresa use para clasificarlo), requerido por el
    /// campo homónimo del PLE Registro de Compras (8.1) y Registro de Ventas e Ingresos (14.1).
    /// Opcional: si no se indica, el campo se exporta en blanco (SUNAT lo acepta vacío salvo que
    /// la empresa esté obligada a detallarlo).
    /// </summary>
    [MaxLength(10)] public string? CodigoBienServicio { get; set; }
    /// <summary>
    /// Indica si el ítem representa un servicio (p. ej. mantenimiento, transporte, asesoría)
    /// en lugar de un bien físico. Los servicios no controlan stock/Kardex al comprarse.
    /// </summary>
    public bool EsServicio { get; set; } = false;
    [Column(TypeName = "decimal(12,2)")] public decimal StockMinimo { get; set; }
    /// <summary>Nivel máximo de stock recomendado (referencial, para planificación de compras).</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal StockMaximo { get; set; }
    /// <summary>Porcentaje (0-100) de descuento aplicado sobre el precio de venta a clientes clasificados como Mayorista.</summary>
    [Column(TypeName = "decimal(5,2)")] public decimal DescuentoMayoristaPct { get; set; } = 0;
    /// <summary>Porcentaje (0-100) de descuento aplicado sobre el precio de venta a clientes clasificados como Super Mayorista.</summary>
    [Column(TypeName = "decimal(5,2)")] public decimal DescuentoSuperMayoristaPct { get; set; } = 0;
    /// <summary>Ruta relativa (servida como archivo estático) a la foto del producto.</summary>
    [MaxLength(500)] public string? FotoUrl { get; set; }
    /// <summary>Observaciones libres del producto; opcional.</summary>
    [MaxLength(1000)] public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
    /// <summary>
    /// Cuenta de ventas (p. ej. 701x del PCGE) a la que se abona el valor de venta de este
    /// producto al facturarlo. Si no se configura, <see cref="Services.ContabilidadService"/>
    /// usa la primera cuenta "70" del plan de cuentas (comportamiento anterior, sin detalle
    /// por producto).
    /// </summary>
    public Guid? CuentaVentaId { get; set; }
    public CuentaContable? CuentaVenta { get; set; }
    /// <summary>
    /// Cuenta de costo de ventas (p. ej. 69) a la que se carga el costo de este producto al
    /// venderlo. Solo aplica a productos que no son servicio (<see cref="EsServicio"/>).
    /// </summary>
    public Guid? CuentaCostoVentaId { get; set; }
    public CuentaContable? CuentaCostoVenta { get; set; }
    /// <summary>Cuenta de existencias/mercaderías (p. ej. 20) que se abona como contrapartida del costo de venta.</summary>
    public Guid? CuentaExistenciasId { get; set; }
    public CuentaContable? CuentaExistencias { get; set; }
    /// <summary>Marca de tiempo de la última alerta de stock bajo enviada (para no spamear notificaciones push).</summary>
    public DateTime? UltimaAlertaStockBajo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
    public ICollection<ComprobanteDetalle> DetallesVenta { get; set; } = new List<ComprobanteDetalle>();
    public ICollection<CompraDetalle> DetallesCompra { get; set; } = new List<CompraDetalle>();
    public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
    /// <summary>
    /// Composición (lista de materiales/BOM) del producto terminado: insumos o servicios que
    /// se descuentan automáticamente del stock cuando este producto se vende.
    /// </summary>
    public ICollection<ProductoComponente> Componentes { get; set; } = new List<ProductoComponente>();

    /// <summary>Stock actual (suma en todos los almacenes); calculado a partir de <see cref="Stocks"/>, no se persiste.</summary>
    [NotMapped]
    public decimal StockActual => Stocks?.Sum(s => s.Cantidad) ?? 0;
}

/// <summary>
/// Línea de la composición (BOM) de un producto terminado: indica qué insumo/servicio y en
/// qué cantidad se requiere para "fabricar"/entregar una unidad del producto terminado.
/// </summary>
public class ProductoComponente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductoTerminadoId { get; set; }
    public Producto ProductoTerminado { get; set; } = null!;
    public Guid InsumoId { get; set; }
    public Producto Insumo { get; set; } = null!;
    /// <summary>Cantidad del insumo requerida por cada unidad vendida del producto terminado.</summary>
    [Column(TypeName = "decimal(12,4)")] public decimal Cantidad { get; set; }
}

public class Almacen
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(255)] public string Nombre { get; set; } = null!;
    [MaxLength(255)] public string? Direccion { get; set; }
    public bool Principal { get; set; }

    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
    public ICollection<MovimientoInventario> Movimientos { get; set; } = new List<MovimientoInventario>();
}

public class Stock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public Guid AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;
    [Column(TypeName = "decimal(12,2)")] public decimal Cantidad { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class MovimientoInventario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    public Guid AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;
    public TipoMovimiento Tipo { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Cantidad { get; set; }
    /// <summary>Costo unitario en el momento del movimiento (usado para el Kardex valorizado, promedio ponderado).</summary>
    [Column(TypeName = "decimal(12,4)")] public decimal? CostoUnitario { get; set; }
    [MaxLength(255)] public string? Referencia { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Suscripción a notificaciones push del navegador (Web Push / VAPID) para un usuario/dispositivo.</summary>
public class PushSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Guid? UsuarioId { get; set; }
    [MaxLength(1000)] public string Endpoint { get; set; } = null!;
    [MaxLength(255)] public string P256dh { get; set; } = null!;
    [MaxLength(255)] public string Auth { get; set; } = null!;
    [MaxLength(255)] public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SerieComprobante
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public TipoComprobante TipoComprobante { get; set; }
    [MaxLength(10)] public string Serie { get; set; } = null!;
    public int CorrelativoActual { get; set; }

    public ICollection<Comprobante> Comprobantes { get; set; } = new List<Comprobante>();
}

public class Comprobante
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public Guid SerieId { get; set; }
    public SerieComprobante SerieComprobante { get; set; } = null!;
    /// <summary>Almacén desde el cual se descontó la mercadería vendida.</summary>
    public Guid? AlmacenId { get; set; }
    public Almacen? Almacen { get; set; }
    /// <summary>Vendedor (usuario) que realizó la venta.</summary>
    public Guid? VendedorId { get; set; }
    public Usuario? Vendedor { get; set; }
    /// <summary>
    /// Comprobante original al que afecta esta Nota de Crédito/Débito (catálogo 10 de SUNAT);
    /// null cuando el documento no es una nota de ajuste de otro comprobante.
    /// </summary>
    public Guid? ComprobanteReferenciaId { get; set; }
    public Comprobante? ComprobanteReferencia { get; set; }
    /// <summary>Motivo de la Nota de Crédito/Débito (p. ej. "Anulación de la operación", "Devolución").</summary>
    [MaxLength(255)] public string? MotivoNota { get; set; }

    /// <summary>
    /// Centro de costo/área al que se imputa la venta (opcional, usado para reportes por área
    /// y propagado al asiento contable generado automáticamente); si no se indica, el asiento
    /// se genera sin centro de costo.
    /// </summary>
    public Guid? CentroCostoId { get; set; }
    public CentroCosto? CentroCosto { get; set; }

    public TipoComprobante TipoComprobante { get; set; }
    public TipoOperacionVenta TipoOperacion { get; set; } = TipoOperacionVenta.VENTA_INTERNA;
    public CondicionPago CondicionPago { get; set; } = CondicionPago.CONTADO;
    /// <summary>Canal por el que se originó la venta (local físico, web, corporativo, Pedidos Ya, Rappi).</summary>
    public CanalVenta CanalVenta { get; set; } = CanalVenta.LOCAL;
    [MaxLength(10)] public string Serie { get; set; } = null!;
    public int Correlativo { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    [MaxLength(5)] public string Moneda { get; set; } = "PEN";

    [Column(TypeName = "decimal(12,2)")] public decimal OpGravada { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal OpExonerada { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal OpInafecta { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Igv { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Total { get; set; }

    public EstadoComprobante Estado { get; set; } = EstadoComprobante.BORRADOR;

    /// <summary>Fecha límite de pago, calculada con los días de crédito del cliente al emitir el comprobante (null = al contado).</summary>
    public DateTime? FechaVencimiento { get; set; }
    /// <summary>Indica si la cuenta por cobrar ya fue cancelada por el cliente.</summary>
    public bool Pagado { get; set; } = false;
    public DateTime? FechaPago { get; set; }

    public string? XmlSinFirma { get; set; }
    public string? XmlFirmado { get; set; }
    public string? Hash { get; set; }
    public string? TicketSunat { get; set; }
    public string? CodigoRespuestaSunat { get; set; }
    public string? DescripcionRespuestaSunat { get; set; }
    public string? CdrXml { get; set; }
    public string? Observaciones { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Asiento del Libro Diario generado automáticamente al emitir este comprobante (vinculación
    /// asiento ↔ comprobante SUNAT, al estilo CONCAR); null si el plan de cuentas no estaba
    /// configurado al momento de emitir la venta. Al anular/eliminar el comprobante, el asiento
    /// vinculado se anula también para que el Libro Diario/Mayor y el PLE queden consistentes.
    /// </summary>
    public Guid? AsientoContableId { get; set; }
    public AsientoContable? AsientoContable { get; set; }

    public ICollection<ComprobanteDetalle> Detalles { get; set; } = new List<ComprobanteDetalle>();
    public ICollection<SunatSyncQueue> SyncQueue { get; set; } = new List<SunatSyncQueue>();
    /// <summary>Métodos de pago usados para cancelar la venta al contado (vacío si es al crédito).</summary>
    public ICollection<ComprobantePago> Pagos { get; set; } = new List<ComprobantePago>();
}

/// <summary>Uno de los medios de pago con los que se canceló una venta al contado (puede haber varios).</summary>
public class ComprobantePago
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ComprobanteId { get; set; }
    public Comprobante Comprobante { get; set; } = null!;
    public MetodoPago MetodoPago { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Monto { get; set; }
    [MaxLength(255)] public string? Referencia { get; set; }
}

public class ComprobanteDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ComprobanteId { get; set; }
    public Comprobante Comprobante { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [Column(TypeName = "decimal(12,2)")] public decimal Cantidad { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal PrecioUnitario { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal ValorVenta { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Igv { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Total { get; set; }
}

public class SunatSyncQueue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ComprobanteId { get; set; }
    public Comprobante Comprobante { get; set; } = null!;
    public int Intentos { get; set; }
    public int MaxIntentos { get; set; } = 5;
    public EstadoCola Estado { get; set; } = EstadoCola.PENDIENTE;
    public string? UltimoError { get; set; }
    public DateTime ProximoIntento { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Compra
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public Guid ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;
    /// <summary>
    /// Centro de costo (= Área) al que se imputa el gasto de la compra, es decir a quién
    /// pertenece/beneficia. Se usa tanto para clasificar el gasto como para reportes por área.
    /// </summary>
    public Guid CentroCostoId { get; set; }
    public CentroCosto CentroCosto { get; set; } = null!;
    /// <summary>Almacén de destino en el que se ingresó el stock de esta compra; se usa para revertir/reaplicar el Kardex al editarla.</summary>
    public Guid AlmacenId { get; set; }
    public Almacen Almacen { get; set; } = null!;
    /// <summary>
    /// Orden de compra de origen: es obligatoria para registrar una compra, ya que toda
    /// compra debe partir de una orden de compra previamente registrada y aprobada.
    /// </summary>
    public Guid? OrdenCompraId { get; set; }
    public OrdenCompra? OrdenCompra { get; set; }
    /// <summary>Personal con rol de Comprador que se reconfirma al registrar la compra (puede diferir del indicado en la orden de compra).</summary>
    public Guid? EmpleadoCompradorId { get; set; }
    public Empleado? EmpleadoComprador { get; set; }
    [MaxLength(10)] public string? Serie { get; set; }
    [MaxLength(20)] public string? Numero { get; set; }
    /// <summary>Tipo de documento (catálogo 01 SUNAT) con el que el proveedor emitió el comprobante recibido.</summary>
    public TipoDocumentoCompra TipoDocumento { get; set; } = TipoDocumentoCompra.FACTURA;
    /// <summary>Fecha de emisión del documento/comprobante recibido del proveedor.</summary>
    public DateTime FechaEmision { get; set; } = DateTime.UtcNow;
    /// <summary>Número de la guía de remisión con la que el proveedor despachó la mercadería, si aplica.</summary>
    [MaxLength(20)] public string? NumeroGuiaRemision { get; set; }
    /// <summary>Referencia libre de la compra (p. ej. número de orden de compra o pedido interno).</summary>
    [MaxLength(255)] public string? Referencia { get; set; }
    /// <summary>Local/establecimiento del proveedor desde el cual se despachó/facturó la compra.</summary>
    [MaxLength(255)] public string? LocalProveedor { get; set; }
    /// <summary>Tipo de operación (catálogo 51 SUNAT) que clasifica la compra para el RCE.</summary>
    public TipoOperacionSunat TipoOperacionSunat { get; set; } = TipoOperacionSunat.COMPRA_INTERNA;
    [MaxLength(5)] public string Moneda { get; set; } = "PEN";
    /// <summary>Tasa de IGV (%) aplicada a la compra (p. ej. 18.00); se usa para desagregar el Importe de los ítems en Subtotal + IGV.</summary>
    [Column(TypeName = "decimal(5,2)")] public decimal TasaIgv { get; set; } = 18m;
    /// <summary>Importe de los ítems sin IGV (valor de venta), calculado a partir del Importe Total y la Tasa IGV.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal Subtotal { get; set; }
    /// <summary>Monto de IGV calculado (Total - Subtotal) según la Tasa IGV indicada.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal Igv { get; set; }
    /// <summary>
    /// Fecha del tipo de cambio aplicado cuando la compra es en moneda distinta a PEN; por
    /// defecto es la fecha del día en que se registra la compra, pero puede indicarse otra.
    /// </summary>
    public DateTime? FechaTipoCambio { get; set; }
    /// <summary>Tipo de cambio (venta) vigente a <see cref="FechaTipoCambio"/>, usado para expresar la compra en la moneda alterna en los reportes.</summary>
    [Column(TypeName = "decimal(8,4)")] public decimal? TipoCambio { get; set; }
    /// <summary>Importe Total de la compra (Subtotal + IGV), calculado automáticamente a partir de los ítems.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal Total { get; set; }
    /// <summary>Glosa/descripción del comprobante recibido del proveedor (detalle de lo que ampara el documento).</summary>
    [MaxLength(500)] public string? GlosaComprobante { get; set; }
    /// <summary>Glosa/descripción del movimiento de ingreso de stock (Kardex) generado por esta compra.</summary>
    [MaxLength(500)] public string? GlosaMovimiento { get; set; }
    /// <summary>Fecha límite de pago al proveedor, calculada con sus días de crédito (null = al contado).</summary>
    public DateTime? FechaVencimiento { get; set; }
    /// <summary>Indica si la cuenta por pagar ya fue cancelada al proveedor.</summary>
    public bool Pagado { get; set; } = false;
    public DateTime? FechaPago { get; set; }
    /// <summary>
    /// Ruta relativa (bajo wwwroot) de la foto del comprobante físico recibido del proveedor,
    /// adjuntada opcionalmente como respaldo adicional al registro digital de la compra.
    /// </summary>
    [MaxLength(255)] public string? ComprobanteFotoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Fecha/hora de la última edición de la compra (null si nunca fue editada desde su registro); sirve de huella de auditoría para detectar ediciones.</summary>
    public DateTime? UpdatedAt { get; set; }
    /// <summary>
    /// Asiento contable (Libro Diario) generado automáticamente al registrar la compra,
    /// imputando cada ítem a la cuenta contable indicada (Existencias, Activo Fijo, Gasto,
    /// etc.) contra la Cuenta por Pagar/Caja-Bancos, al estilo de la contabilización
    /// automática de compras de CONCAR. Null si el plan de cuentas no está configurado.
    /// </summary>
    public Guid? AsientoContableId { get; set; }
    public AsientoContable? AsientoContable { get; set; }

    /// <summary>
    /// Código de operación (al estilo CONCAR, p. ej. "FT. Recibidas M.N. Tercero") elegido
    /// explícitamente al registrar la compra, que determina la cuenta por pagar/caja completa
    /// y el subdiario del asiento generado. Opcional: si no se indica, se contabiliza igual
    /// buscando automáticamente la configuración por (RECIBIDO, TipoDocumento, Moneda).
    /// </summary>
    public Guid? ConfiguracionCuentaOperacionId { get; set; }
    public ConfiguracionCuentaOperacion? ConfiguracionCuentaOperacion { get; set; }

    /// <summary>
    /// Indica si la compra fue anulada (baja lógica, al estilo CONCAR): no se elimina el
    /// registro para conservar el histórico/correlativo, revierte el stock/Kardex ingresado
    /// y anula el asiento contable generado. Requiere autorización de un usuario Administrador.
    /// </summary>
    public bool Anulado { get; set; } = false;
    /// <summary>Motivo indicado al anular la compra.</summary>
    [MaxLength(500)] public string? MotivoAnulacion { get; set; }
    /// <summary>Fecha/hora en que se anuló la compra.</summary>
    public DateTime? FechaAnulacion { get; set; }

    public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
}

public class CompraDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompraId { get; set; }
    public Compra Compra { get; set; } = null!;
    public Guid ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;
    [Column(TypeName = "decimal(12,2)")] public decimal Cantidad { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal PrecioUnitario { get; set; }
    /// <summary>Porcentaje de descuento (0-100) otorgado por el proveedor en este ítem, si tuvo.</summary>
    [Column(TypeName = "decimal(5,2)")] public decimal PorcentajeDescuento { get; set; } = 0;
    [Column(TypeName = "decimal(12,2)")] public decimal Total { get; set; }
    /// <summary>
    /// Cuenta contable (del plan de cuentas) a la que se imputa este ítem de la compra
    /// según su destino/naturaleza: Existencias/Mercaderías (20), Activo Fijo (33/36),
    /// Servicios prestados por terceros (63), etc. Opcional: si no se indica, se usa por
    /// defecto Existencias (o Servicios si el producto está marcado como servicio).
    /// </summary>
    public Guid? CuentaContableId { get; set; }
    public CuentaContable? CuentaContable { get; set; }
}

/// <summary>
/// Tipo de cambio (USD/PEN) mantenido día a día para poder expresar en soles o en
/// dólares el importe pendiente de pago de las compras registradas en moneda distinta.
/// </summary>
public class TipoCambio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    public DateTime Fecha { get; set; }
    /// <summary>Tipo de cambio compra (referencial, informativo).</summary>
    [Column(TypeName = "decimal(8,4)")] public decimal Compra { get; set; }
    /// <summary>Tipo de cambio venta; es el que se usa para convertir importes en USD a PEN.</summary>
    [Column(TypeName = "decimal(8,4)")] public decimal Venta { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
