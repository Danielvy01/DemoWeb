using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Models;

namespace Siserp.Api.Data;

public class SiserpDbContext : DbContext
{
    public SiserpDbContext(DbContextOptions<SiserpDbContext> options) : base(options) { }

    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Almacen> Almacenes => Set<Almacen>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<SerieComprobante> SeriesComprobante => Set<SerieComprobante>();
    public DbSet<Comprobante> Comprobantes => Set<Comprobante>();
    public DbSet<ComprobanteDetalle> ComprobanteDetalles => Set<ComprobanteDetalle>();
    public DbSet<ComprobantePago> ComprobantePagos => Set<ComprobantePago>();
    public DbSet<ProductoComponente> ProductoComponentes => Set<ProductoComponente>();
    public DbSet<SunatSyncQueue> SunatSyncQueue => Set<SunatSyncQueue>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<CompraDetalle> CompraDetalles => Set<CompraDetalle>();
    public DbSet<TipoCambio> TiposCambio => Set<TipoCambio>();
    public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
    public DbSet<OrdenCompraDetalle> OrdenCompraDetalles => Set<OrdenCompraDetalle>();
    public DbSet<OrdenTrabajo> OrdenesTrabajo => Set<OrdenTrabajo>();
    public DbSet<OrdenTrabajoInsumo> OrdenTrabajoInsumos => Set<OrdenTrabajoInsumo>();
    public DbSet<OrdenTrabajoPlantilla> PlantillasOrdenTrabajo => Set<OrdenTrabajoPlantilla>();
    public DbSet<OrdenTrabajoPlantillaInsumo> PlantillaOrdenTrabajoInsumos => Set<OrdenTrabajoPlantillaInsumo>();
    public DbSet<GuiaRemision> GuiasRemision => Set<GuiaRemision>();
    public DbSet<GuiaRemisionDetalle> GuiaRemisionDetalles => Set<GuiaRemisionDetalle>();
    public DbSet<OrdenSalidaAlmacen> OrdenesSalidaAlmacen => Set<OrdenSalidaAlmacen>();
    public DbSet<OrdenSalidaAlmacenDetalle> OrdenSalidaAlmacenDetalles => Set<OrdenSalidaAlmacenDetalle>();
    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();
    public DbSet<CotizacionDetalle> CotizacionDetalles => Set<CotizacionDetalle>();
    public DbSet<PedidoVenta> PedidosVenta => Set<PedidoVenta>();
    public DbSet<PedidoVentaDetalle> PedidoVentaDetalles => Set<PedidoVentaDetalle>();
    public DbSet<CampanaMarketing> Campanas => Set<CampanaMarketing>();
    public DbSet<MensajeCampana> MensajesCampana => Set<MensajeCampana>();
    public DbSet<CuentaContable> CuentasContables => Set<CuentaContable>();
    public DbSet<AsientoContable> AsientosContables => Set<AsientoContable>();
    public DbSet<AsientoDetalle> AsientoDetalles => Set<AsientoDetalle>();
    public DbSet<AsientoContableHistorial> AsientosContablesHistorial => Set<AsientoContableHistorial>();
    public DbSet<ConfiguracionCuentaOperacion> ConfiguracionesCuentaOperacion => Set<ConfiguracionCuentaOperacion>();
    public DbSet<DocumentoCtaCte> DocumentosCtaCte => Set<DocumentoCtaCte>();
    public DbSet<AplicacionDocumentoCtaCte> AplicacionesDocumentoCtaCte => Set<AplicacionDocumentoCtaCte>();
    public DbSet<EjercicioContable> EjerciciosContables => Set<EjercicioContable>();
    public DbSet<PeriodoContable> PeriodosContables => Set<PeriodoContable>();
    public DbSet<PresupuestoAnual> PresupuestosAnuales => Set<PresupuestoAnual>();
    public DbSet<PresupuestoDetalle> PresupuestoDetalles => Set<PresupuestoDetalle>();
    public DbSet<CentroCosto> CentrosCosto => Set<CentroCosto>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<EmpleadoCargo> EmpleadoCargos => Set<EmpleadoCargo>();
    public DbSet<Planilla> Planillas => Set<Planilla>();
    public DbSet<PlanillaDetalle> PlanillaDetalles => Set<PlanillaDetalle>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<RolMenuPermiso> RolMenuPermisos => Set<RolMenuPermiso>();
    public DbSet<UsuarioRol> UsuarioRoles => Set<UsuarioRol>();
    public DbSet<ActivoFijo> ActivosFijos => Set<ActivoFijo>();
    public DbSet<DepreciacionActivoFijo> DepreciacionesActivoFijo => Set<DepreciacionActivoFijo>();
    public DbSet<ConciliacionBancaria> ConciliacionesBancarias => Set<ConciliacionBancaria>();
    public DbSet<MovimientoExtractoBancario> MovimientosExtractoBancario => Set<MovimientoExtractoBancario>();
    public DbSet<ReporteFinancieroPlantilla> ReportesFinancierosPlantillas => Set<ReporteFinancieroPlantilla>();
    public DbSet<ReporteFinancieroSeccion> ReportesFinancierosSecciones => Set<ReporteFinancieroSeccion>();
    public DbSet<ReporteFinancieroCuenta> ReportesFinancierosCuentas => Set<ReporteFinancieroCuenta>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Empresa>().HasIndex(e => e.Ruc).IsUnique();

        modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Empresa).WithMany(e => e.Usuarios)
            .HasForeignKey(u => u.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        // --- Roles, menús y permisos (seguridad granular por menú) ---
        modelBuilder.Entity<Rol>()
            .HasIndex(r => new { r.EmpresaId, r.Nombre }).IsUnique();
        modelBuilder.Entity<Rol>()
            .HasOne(r => r.Empresa).WithMany(e => e.Roles)
            .HasForeignKey(r => r.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Menu>()
            .HasIndex(m => m.Codigo).IsUnique();
        modelBuilder.Entity<Menu>().HasData(MenuCatalogo.Menus.Select(m => new Menu
        {
            Id = m.Id,
            Codigo = m.Codigo,
            Nombre = m.Nombre,
            Grupo = m.Grupo,
            Orden = m.Orden,
            Activo = true,
        }));

        modelBuilder.Entity<RolMenuPermiso>()
            .HasIndex(p => new { p.RolId, p.MenuId }).IsUnique();
        modelBuilder.Entity<RolMenuPermiso>()
            .HasOne(p => p.Rol).WithMany(r => r.Permisos)
            .HasForeignKey(p => p.RolId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RolMenuPermiso>()
            .HasOne(p => p.Menu).WithMany(m => m.Permisos)
            .HasForeignKey(p => p.MenuId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UsuarioRol>().HasKey(ur => new { ur.UsuarioId, ur.RolId });
        modelBuilder.Entity<UsuarioRol>()
            .HasOne(ur => ur.Usuario).WithMany(u => u.UsuarioRoles)
            .HasForeignKey(ur => ur.UsuarioId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UsuarioRol>()
            .HasOne(ur => ur.Rol).WithMany(r => r.UsuarioRoles)
            .HasForeignKey(ur => ur.RolId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => new { c.EmpresaId, c.TipoDocumento, c.NumeroDocumento }).IsUnique();
        modelBuilder.Entity<Cliente>()
            .HasOne(c => c.Empresa).WithMany(e => e.Clientes)
            .HasForeignKey(c => c.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Proveedor>()
            .HasIndex(p => new { p.EmpresaId, p.Ruc }).IsUnique();
        modelBuilder.Entity<Proveedor>()
            .HasOne(p => p.Empresa).WithMany(e => e.Proveedores)
            .HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Producto>()
            .HasIndex(p => new { p.EmpresaId, p.Codigo }).IsUnique();
        modelBuilder.Entity<Producto>()
            .HasOne(p => p.Empresa).WithMany(e => e.Productos)
            .HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Producto>()
            .HasOne(p => p.Categoria).WithMany(c => c.Productos)
            .HasForeignKey(p => p.CategoriaId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Producto>().Property(p => p.PrecioVenta).HasPrecision(12, 2);
        modelBuilder.Entity<Producto>().Property(p => p.PrecioLista).HasPrecision(12, 2);
        modelBuilder.Entity<Producto>().Property(p => p.PrecioCosto).HasPrecision(12, 2);
        modelBuilder.Entity<Producto>().Property(p => p.StockMaximo).HasPrecision(12, 2);
        modelBuilder.Entity<Producto>().Property(p => p.DescuentoMayoristaPct).HasPrecision(5, 2);
        modelBuilder.Entity<Producto>().Property(p => p.DescuentoSuperMayoristaPct).HasPrecision(5, 2);
        modelBuilder.Entity<Producto>()
            .HasOne(p => p.CuentaVenta).WithMany()
            .HasForeignKey(p => p.CuentaVentaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Producto>()
            .HasOne(p => p.CuentaCostoVenta).WithMany()
            .HasForeignKey(p => p.CuentaCostoVentaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Producto>()
            .HasOne(p => p.CuentaExistencias).WithMany()
            .HasForeignKey(p => p.CuentaExistenciasId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductoComponente>()
            .HasOne(c => c.ProductoTerminado).WithMany(p => p.Componentes)
            .HasForeignKey(c => c.ProductoTerminadoId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductoComponente>()
            .HasOne(c => c.Insumo).WithMany()
            .HasForeignKey(c => c.InsumoId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Categoria>()
            .HasOne(c => c.Empresa).WithMany(e => e.Categorias)
            .HasForeignKey(c => c.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Categoria>()
            .HasIndex(c => new { c.EmpresaId, c.Nombre }).IsUnique();

        modelBuilder.Entity<Almacen>()
            .HasOne(a => a.Empresa).WithMany(e => e.Almacenes)
            .HasForeignKey(a => a.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Stock>()
            .HasIndex(s => new { s.ProductoId, s.AlmacenId }).IsUnique();
        modelBuilder.Entity<Stock>()
            .HasOne(s => s.Producto).WithMany(p => p.Stocks)
            .HasForeignKey(s => s.ProductoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Stock>()
            .HasOne(s => s.Almacen).WithMany(a => a.Stocks)
            .HasForeignKey(s => s.AlmacenId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MovimientoInventario>()
            .HasOne(m => m.Producto).WithMany(p => p.Movimientos)
            .HasForeignKey(m => m.ProductoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MovimientoInventario>()
            .HasOne(m => m.Almacen).WithMany(a => a.Movimientos)
            .HasForeignKey(m => m.AlmacenId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PushSubscription>()
            .HasIndex(s => s.Endpoint).IsUnique();
        modelBuilder.Entity<PushSubscription>()
            .HasOne(s => s.Empresa).WithMany()
            .HasForeignKey(s => s.EmpresaId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SerieComprobante>()
            .HasIndex(s => new { s.EmpresaId, s.TipoComprobante, s.Serie }).IsUnique();
        modelBuilder.Entity<SerieComprobante>()
            .HasOne(s => s.Empresa).WithMany(e => e.Series)
            .HasForeignKey(s => s.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Comprobante>()
            .HasIndex(c => new { c.EmpresaId, c.TipoComprobante, c.Serie, c.Correlativo }).IsUnique();
        modelBuilder.Entity<Comprobante>()
            .HasOne(c => c.Empresa).WithMany(e => e.Comprobantes)
            .HasForeignKey(c => c.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Comprobante>()
            .HasOne(c => c.Cliente).WithMany(cl => cl.Comprobantes)
            .HasForeignKey(c => c.ClienteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Comprobante>()
            .HasOne(c => c.SerieComprobante).WithMany(s => s.Comprobantes)
            .HasForeignKey(c => c.SerieId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Comprobante>()
            .HasOne(c => c.Almacen).WithMany()
            .HasForeignKey(c => c.AlmacenId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Comprobante>()
            .HasOne(c => c.Vendedor).WithMany()
            .HasForeignKey(c => c.VendedorId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Comprobante>()
            .HasOne(c => c.ComprobanteReferencia).WithMany()
            .HasForeignKey(c => c.ComprobanteReferenciaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Comprobante>()
            .HasOne(c => c.CentroCosto).WithMany()
            .HasForeignKey(c => c.CentroCostoId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ComprobantePago>()
            .HasOne(p => p.Comprobante).WithMany(c => c.Pagos)
            .HasForeignKey(p => p.ComprobanteId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ComprobanteDetalle>()
            .HasOne(d => d.Comprobante).WithMany(c => c.Detalles)
            .HasForeignKey(d => d.ComprobanteId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ComprobanteDetalle>()
            .HasOne(d => d.Producto).WithMany(p => p.DetallesVenta)
            .HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SunatSyncQueue>()
            .HasOne(s => s.Comprobante).WithMany(c => c.SyncQueue)
            .HasForeignKey(s => s.ComprobanteId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Compra>()
            .HasOne(c => c.Empresa).WithMany(e => e.Compras)
            .HasForeignKey(c => c.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Compra>()
            .HasOne(c => c.Proveedor).WithMany(p => p.Compras)
            .HasForeignKey(c => c.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Compra>()
            .HasOne(c => c.OrdenCompra).WithMany()
            .HasForeignKey(c => c.OrdenCompraId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Compra>()
            .HasOne(c => c.CentroCosto).WithMany()
            .HasForeignKey(c => c.CentroCostoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Compra>()
            .HasOne(c => c.Almacen).WithMany()
            .HasForeignKey(c => c.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Compra>()
            .HasOne(c => c.EmpleadoComprador).WithMany()
            .HasForeignKey(c => c.EmpleadoCompradorId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Compra>()
            .HasOne(c => c.AsientoContable).WithMany()
            .HasForeignKey(c => c.AsientoContableId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Compra>()
            .HasOne(c => c.ConfiguracionCuentaOperacion).WithMany()
            .HasForeignKey(c => c.ConfiguracionCuentaOperacionId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CompraDetalle>()
            .HasOne(d => d.Compra).WithMany(c => c.Detalles)
            .HasForeignKey(d => d.CompraId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CompraDetalle>()
            .HasOne(d => d.Producto).WithMany(p => p.DetallesCompra)
            .HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CompraDetalle>()
            .HasOne(d => d.CuentaContable).WithMany()
            .HasForeignKey(d => d.CuentaContableId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TipoCambio>()
            .HasOne(t => t.Empresa).WithMany()
            .HasForeignKey(t => t.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TipoCambio>()
            .HasIndex(t => new { t.EmpresaId, t.Fecha }).IsUnique();

        // --- Órdenes de compra ---
        modelBuilder.Entity<OrdenCompra>()
            .HasIndex(o => new { o.EmpresaId, o.Numero }).IsUnique();
        modelBuilder.Entity<OrdenCompra>()
            .HasOne(o => o.Empresa).WithMany()
            .HasForeignKey(o => o.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenCompra>()
            .HasOne(o => o.Proveedor).WithMany(p => p.OrdenesCompra)
            .HasForeignKey(o => o.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenCompra>()
            .HasOne(o => o.Comprobante).WithMany()
            .HasForeignKey(o => o.ComprobanteId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<OrdenCompra>()
            .HasOne(o => o.EmpleadoComprador).WithMany()
            .HasForeignKey(o => o.EmpleadoCompradorId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<OrdenCompraDetalle>()
            .HasOne(d => d.OrdenCompra).WithMany(o => o.Detalles)
            .HasForeignKey(d => d.OrdenCompraId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrdenCompraDetalle>()
            .HasOne(d => d.Producto).WithMany()
            .HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenCompraDetalle>()
            .HasOne(d => d.CuentaContable).WithMany()
            .HasForeignKey(d => d.CuentaContableId).OnDelete(DeleteBehavior.Restrict);

        // --- Órdenes de trabajo / producción ---
        modelBuilder.Entity<OrdenTrabajo>()
            .HasIndex(o => new { o.EmpresaId, o.Numero }).IsUnique();
        modelBuilder.Entity<OrdenTrabajo>()
            .HasOne(o => o.Empresa).WithMany(e => e.OrdenesTrabajo)
            .HasForeignKey(o => o.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenTrabajo>()
            .HasOne(o => o.Producto).WithMany()
            .HasForeignKey(o => o.ProductoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenTrabajo>()
            .HasOne(o => o.Almacen).WithMany()
            .HasForeignKey(o => o.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenTrabajo>()
            .HasOne(o => o.OrdenCompra).WithMany()
            .HasForeignKey(o => o.OrdenCompraId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<OrdenTrabajoInsumo>()
            .HasOne(i => i.OrdenTrabajo).WithMany(o => o.Insumos)
            .HasForeignKey(i => i.OrdenTrabajoId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrdenTrabajoInsumo>()
            .HasOne(i => i.Producto).WithMany()
            .HasForeignKey(i => i.ProductoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenTrabajo>()
            .HasOne(o => o.Plantilla).WithMany()
            .HasForeignKey(o => o.PlantillaId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<OrdenTrabajo>()
            .HasOne(o => o.OrdenSalidaAlmacen).WithMany()
            .HasForeignKey(o => o.OrdenSalidaAlmacenId).OnDelete(DeleteBehavior.SetNull);

        // --- Plantillas de orden de trabajo (receta/BOM) ---
        modelBuilder.Entity<OrdenTrabajoPlantilla>()
            .HasIndex(p => new { p.EmpresaId, p.Nombre }).IsUnique();
        modelBuilder.Entity<OrdenTrabajoPlantilla>()
            .HasOne(p => p.Empresa).WithMany()
            .HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenTrabajoPlantilla>()
            .HasOne(p => p.Producto).WithMany()
            .HasForeignKey(p => p.ProductoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenTrabajoPlantillaInsumo>()
            .HasOne(i => i.Plantilla).WithMany(p => p.Insumos)
            .HasForeignKey(i => i.PlantillaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrdenTrabajoPlantillaInsumo>()
            .HasOne(i => i.Producto).WithMany()
            .HasForeignKey(i => i.ProductoId).OnDelete(DeleteBehavior.Restrict);

        // --- Guías de remisión ---
        modelBuilder.Entity<GuiaRemision>()
            .HasIndex(g => new { g.EmpresaId, g.Numero }).IsUnique();
        modelBuilder.Entity<GuiaRemision>()
            .HasOne(g => g.Empresa).WithMany()
            .HasForeignKey(g => g.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GuiaRemision>()
            .HasOne(g => g.AlmacenOrigen).WithMany()
            .HasForeignKey(g => g.AlmacenOrigenId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GuiaRemision>()
            .HasOne(g => g.AlmacenDestino).WithMany()
            .HasForeignKey(g => g.AlmacenDestinoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GuiaRemision>()
            .HasOne(g => g.Cliente).WithMany()
            .HasForeignKey(g => g.ClienteId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<GuiaRemision>()
            .HasOne(g => g.Comprobante).WithMany()
            .HasForeignKey(g => g.ComprobanteId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<GuiaRemisionDetalle>()
            .HasOne(d => d.GuiaRemision).WithMany(g => g.Detalles)
            .HasForeignKey(d => d.GuiaRemisionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<GuiaRemisionDetalle>()
            .HasOne(d => d.Producto).WithMany()
            .HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);

        // --- Órdenes de salida de almacén ---
        modelBuilder.Entity<OrdenSalidaAlmacen>()
            .HasIndex(o => new { o.EmpresaId, o.Numero }).IsUnique();
        modelBuilder.Entity<OrdenSalidaAlmacen>()
            .HasOne(o => o.Empresa).WithMany()
            .HasForeignKey(o => o.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenSalidaAlmacen>()
            .HasOne(o => o.Almacen).WithMany()
            .HasForeignKey(o => o.AlmacenId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrdenSalidaAlmacen>()
            .HasOne(o => o.OrdenTrabajo).WithMany()
            .HasForeignKey(o => o.OrdenTrabajoId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<OrdenSalidaAlmacenDetalle>()
            .HasOne(d => d.OrdenSalidaAlmacen).WithMany(o => o.Detalles)
            .HasForeignKey(d => d.OrdenSalidaAlmacenId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrdenSalidaAlmacenDetalle>()
            .HasOne(d => d.Producto).WithMany()
            .HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);

        // --- Cotizaciones ---
        modelBuilder.Entity<Cotizacion>()
            .HasIndex(c => new { c.EmpresaId, c.Numero }).IsUnique();
        modelBuilder.Entity<Cotizacion>()
            .HasOne(c => c.Empresa).WithMany()
            .HasForeignKey(c => c.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Cotizacion>()
            .HasOne(c => c.Cliente).WithMany(cl => cl.Cotizaciones)
            .HasForeignKey(c => c.ClienteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Cotizacion>()
            .HasOne(c => c.Comprobante).WithMany()
            .HasForeignKey(c => c.ComprobanteId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<CotizacionDetalle>()
            .HasOne(d => d.Cotizacion).WithMany(c => c.Detalles)
            .HasForeignKey(d => d.CotizacionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CotizacionDetalle>()
            .HasOne(d => d.Producto).WithMany()
            .HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Cotizacion>()
            .HasOne(c => c.PedidoVenta).WithOne(p => p.Cotizacion)
            .HasForeignKey<PedidoVenta>(p => p.CotizacionId).OnDelete(DeleteBehavior.SetNull);

        // --- Pedidos de venta ---
        modelBuilder.Entity<PedidoVenta>()
            .HasIndex(p => new { p.EmpresaId, p.Numero }).IsUnique();
        modelBuilder.Entity<PedidoVenta>()
            .HasOne(p => p.Empresa).WithMany()
            .HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PedidoVenta>()
            .HasOne(p => p.Cliente).WithMany()
            .HasForeignKey(p => p.ClienteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PedidoVenta>()
            .HasOne(p => p.Comprobante).WithMany()
            .HasForeignKey(p => p.ComprobanteId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<PedidoVentaDetalle>()
            .HasOne(d => d.PedidoVenta).WithMany(p => p.Detalles)
            .HasForeignKey(d => d.PedidoVentaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PedidoVentaDetalle>()
            .HasOne(d => d.Producto).WithMany()
            .HasForeignKey(d => d.ProductoId).OnDelete(DeleteBehavior.Restrict);

        // --- Marketing ---
        modelBuilder.Entity<CampanaMarketing>()
            .HasOne(c => c.Empresa).WithMany(e => e.Campanas)
            .HasForeignKey(c => c.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<MensajeCampana>()
            .HasOne(m => m.Campana).WithMany(c => c.Mensajes)
            .HasForeignKey(m => m.CampanaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MensajeCampana>()
            .HasOne(m => m.Cliente).WithMany()
            .HasForeignKey(m => m.ClienteId).OnDelete(DeleteBehavior.Restrict);

        // --- Contabilidad ---
        modelBuilder.Entity<CuentaContable>()
            .HasIndex(c => new { c.EmpresaId, c.Codigo }).IsUnique();
        modelBuilder.Entity<CuentaContable>()
            .HasOne(c => c.Empresa).WithMany(e => e.CuentasContables)
            .HasForeignKey(c => c.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CuentaContable>()
            .HasOne(c => c.CuentaPadre).WithMany()
            .HasForeignKey(c => c.CuentaPadreId).OnDelete(DeleteBehavior.Restrict);

        // Catálogo de "códigos de operación" (cuenta completa + subdiario por tipo de
        // documento/moneda), al estilo CONCAR.
        modelBuilder.Entity<ConfiguracionCuentaOperacion>()
            .HasIndex(c => new { c.EmpresaId, c.Movimiento, c.TipoDocumento, c.Moneda }).IsUnique();
        modelBuilder.Entity<ConfiguracionCuentaOperacion>()
            .HasOne(c => c.Empresa).WithMany()
            .HasForeignKey(c => c.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ConfiguracionCuentaOperacion>()
            .HasOne(c => c.CuentaContable).WithMany()
            .HasForeignKey(c => c.CuentaContableId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AsientoContable>()
            .HasIndex(a => new { a.EmpresaId, a.Numero }).IsUnique();
        modelBuilder.Entity<AsientoContable>()
            .HasOne(a => a.Empresa).WithMany(e => e.AsientosContables)
            .HasForeignKey(a => a.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AsientoDetalle>()
            .HasOne(d => d.Asiento).WithMany(a => a.Detalles)
            .HasForeignKey(d => d.AsientoId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AsientoDetalle>()
            .HasOne(d => d.Cuenta).WithMany(c => c.Movimientos)
            .HasForeignKey(d => d.CuentaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AsientoDetalle>()
            .HasOne(d => d.CentroCosto).WithMany()
            .HasForeignKey(d => d.CentroCostoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AsientoDetalle>()
            .HasOne(d => d.Cliente).WithMany()
            .HasForeignKey(d => d.ClienteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AsientoDetalle>()
            .HasOne(d => d.Proveedor).WithMany()
            .HasForeignKey(d => d.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AsientoContable>()
            .HasOne(a => a.UsuarioCreacion).WithMany()
            .HasForeignKey(a => a.UsuarioCreacionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AsientoContable>()
            .HasOne(a => a.UsuarioModificacion).WithMany()
            .HasForeignKey(a => a.UsuarioModificacionId).OnDelete(DeleteBehavior.Restrict);

        // --- Auditoría y Trazabilidad: historial de cambios sobre asientos (al estilo CONCAR) ---
        modelBuilder.Entity<AsientoContableHistorial>()
            .HasIndex(h => new { h.AsientoId, h.Version });
        modelBuilder.Entity<AsientoContableHistorial>()
            .HasOne(h => h.Asiento).WithMany(a => a.Historial)
            .HasForeignKey(h => h.AsientoId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AsientoContableHistorial>()
            .HasOne(h => h.Usuario).WithMany()
            .HasForeignKey(h => h.UsuarioId).OnDelete(DeleteBehavior.Restrict);

        // --- Análisis de Cuentas / Provisiones (cuenta corriente documentaria) ---
        modelBuilder.Entity<DocumentoCtaCte>()
            .HasOne(d => d.Empresa).WithMany()
            .HasForeignKey(d => d.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DocumentoCtaCte>()
            .HasOne(d => d.AsientoDetalle).WithOne(ad => ad.DocumentoCtaCte)
            .HasForeignKey<DocumentoCtaCte>(d => d.AsientoDetalleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DocumentoCtaCte>()
            .HasOne(d => d.Cuenta).WithMany()
            .HasForeignKey(d => d.CuentaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DocumentoCtaCte>()
            .HasOne(d => d.Cliente).WithMany()
            .HasForeignKey(d => d.ClienteId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DocumentoCtaCte>()
            .HasOne(d => d.Proveedor).WithMany()
            .HasForeignKey(d => d.ProveedorId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DocumentoCtaCte>()
            .HasOne(d => d.CentroCosto).WithMany()
            .HasForeignKey(d => d.CentroCostoId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AplicacionDocumentoCtaCte>()
            .HasOne(a => a.DocumentoCtaCte).WithMany(d => d.Aplicaciones)
            .HasForeignKey(a => a.DocumentoCtaCteId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AplicacionDocumentoCtaCte>()
            .HasOne(a => a.Asiento).WithMany()
            .HasForeignKey(a => a.AsientoId).OnDelete(DeleteBehavior.Restrict);

        // --- Cierre y apertura de ejercicio (ejercicios/periodos contables) ---
        modelBuilder.Entity<EjercicioContable>()
            .HasIndex(e => new { e.EmpresaId, e.Anio }).IsUnique();
        modelBuilder.Entity<EjercicioContable>()
            .HasOne(e => e.Empresa).WithMany()
            .HasForeignKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EjercicioContable>()
            .HasOne(e => e.AsientoApertura).WithMany()
            .HasForeignKey(e => e.AsientoAperturaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<EjercicioContable>()
            .HasOne(e => e.AsientoCierre).WithMany()
            .HasForeignKey(e => e.AsientoCierreId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PeriodoContable>()
            .HasIndex(p => new { p.EmpresaId, p.Anio, p.Mes }).IsUnique();
        modelBuilder.Entity<PeriodoContable>()
            .HasOne(p => p.Empresa).WithMany()
            .HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PeriodoContable>()
            .HasOne(p => p.Ejercicio).WithMany(e => e.Periodos)
            .HasForeignKey(p => p.EjercicioId).OnDelete(DeleteBehavior.Cascade);

        // --- Presupuestos y Control (planificación por cuenta/centro de costo, comparativo Real vs. Presupuestado) ---
        modelBuilder.Entity<PresupuestoAnual>()
            .HasIndex(p => new { p.EmpresaId, p.Anio });
        modelBuilder.Entity<PresupuestoAnual>()
            .HasOne(p => p.Empresa).WithMany()
            .HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PresupuestoDetalle>()
            .HasIndex(d => new { d.PresupuestoId, d.CuentaId, d.CentroCostoId, d.Mes }).IsUnique();
        modelBuilder.Entity<PresupuestoDetalle>()
            .HasOne(d => d.Presupuesto).WithMany(p => p.Detalles)
            .HasForeignKey(d => d.PresupuestoId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PresupuestoDetalle>()
            .HasOne(d => d.Cuenta).WithMany()
            .HasForeignKey(d => d.CuentaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PresupuestoDetalle>()
            .HasOne(d => d.CentroCosto).WithMany()
            .HasForeignKey(d => d.CentroCostoId).OnDelete(DeleteBehavior.Restrict);

        // --- Activos fijos / depreciación ---
        modelBuilder.Entity<ActivoFijo>()
            .HasIndex(a => new { a.EmpresaId, a.Codigo }).IsUnique();
        modelBuilder.Entity<ActivoFijo>()
            .HasOne(a => a.Empresa).WithMany()
            .HasForeignKey(a => a.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ActivoFijo>()
            .HasOne(a => a.CentroCosto).WithMany()
            .HasForeignKey(a => a.CentroCostoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ActivoFijo>()
            .HasOne(a => a.CuentaActivo).WithMany()
            .HasForeignKey(a => a.CuentaActivoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ActivoFijo>()
            .HasOne(a => a.CuentaDepreciacionAcumulada).WithMany()
            .HasForeignKey(a => a.CuentaDepreciacionAcumuladaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ActivoFijo>()
            .HasOne(a => a.CuentaGastoDepreciacion).WithMany()
            .HasForeignKey(a => a.CuentaGastoDepreciacionId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DepreciacionActivoFijo>()
            .HasIndex(d => new { d.ActivoFijoId, d.Periodo }).IsUnique();
        modelBuilder.Entity<DepreciacionActivoFijo>()
            .HasOne(d => d.ActivoFijo).WithMany(a => a.Depreciaciones)
            .HasForeignKey(d => d.ActivoFijoId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DepreciacionActivoFijo>()
            .HasOne(d => d.AsientoContable).WithMany()
            .HasForeignKey(d => d.AsientoContableId).OnDelete(DeleteBehavior.Restrict);

        // --- RRHH / Planillas ---
        modelBuilder.Entity<CentroCosto>()
            .HasIndex(c => new { c.EmpresaId, c.Nombre }).IsUnique();
        modelBuilder.Entity<CentroCosto>()
            .HasOne(c => c.Empresa).WithMany()
            .HasForeignKey(c => c.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Empleado>()
            .HasIndex(e => new { e.EmpresaId, e.NumeroDocumento }).IsUnique();
        modelBuilder.Entity<Empleado>()
            .HasOne(e => e.CentroCosto).WithMany(c => c.Empleados)
            .HasForeignKey(e => e.CentroCostoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Empleado>()
            .HasOne(e => e.Empresa).WithMany(e => e.Empleados)
            .HasForeignKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmpleadoCargo>()
            .HasIndex(c => new { c.EmpleadoId, c.Cargo }).IsUnique();
        modelBuilder.Entity<EmpleadoCargo>()
            .HasOne(c => c.Empleado).WithMany(e => e.Cargos)
            .HasForeignKey(c => c.EmpleadoId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Planilla>()
            .HasIndex(p => new { p.EmpresaId, p.Periodo }).IsUnique();
        modelBuilder.Entity<Planilla>()
            .HasOne(p => p.Empresa).WithMany(e => e.Planillas)
            .HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Planilla>()
            .HasOne(p => p.AsientoProvision).WithMany()
            .HasForeignKey(p => p.AsientoProvisionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Planilla>()
            .HasOne(p => p.AsientoPago).WithMany()
            .HasForeignKey(p => p.AsientoPagoId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PlanillaDetalle>()
            .HasOne(d => d.Planilla).WithMany(p => p.Detalles)
            .HasForeignKey(d => d.PlanillaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlanillaDetalle>()
            .HasOne(d => d.Empleado).WithMany(e => e.DetallesPlanilla)
            .HasForeignKey(d => d.EmpleadoId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ConciliacionBancaria>()
            .HasOne(c => c.Empresa).WithMany()
            .HasForeignKey(c => c.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ConciliacionBancaria>()
            .HasOne(c => c.Cuenta).WithMany()
            .HasForeignKey(c => c.CuentaId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ConciliacionBancaria>()
            .HasIndex(c => new { c.EmpresaId, c.CuentaId, c.Anio, c.Mes }).IsUnique();
        modelBuilder.Entity<MovimientoExtractoBancario>()
            .HasOne(m => m.Conciliacion).WithMany(c => c.Movimientos)
            .HasForeignKey(m => m.ConciliacionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MovimientoExtractoBancario>()
            .HasOne(m => m.AsientoDetalle).WithMany()
            .HasForeignKey(m => m.AsientoDetalleId).OnDelete(DeleteBehavior.Restrict);

        // --- NIIF / Dinámica de Cuentas: plantillas configurables de reportes financieros y notas ---
        modelBuilder.Entity<ReporteFinancieroPlantilla>()
            .HasIndex(p => new { p.EmpresaId, p.Codigo }).IsUnique();
        modelBuilder.Entity<ReporteFinancieroPlantilla>()
            .HasOne(p => p.Empresa).WithMany()
            .HasForeignKey(p => p.EmpresaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReporteFinancieroSeccion>()
            .HasIndex(s => new { s.PlantillaId, s.SeccionPadreId, s.Orden });
        modelBuilder.Entity<ReporteFinancieroSeccion>()
            .HasOne(s => s.Plantilla).WithMany(p => p.Secciones)
            .HasForeignKey(s => s.PlantillaId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ReporteFinancieroSeccion>()
            .HasOne(s => s.SeccionPadre).WithMany(s => s.Hijas)
            .HasForeignKey(s => s.SeccionPadreId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ReporteFinancieroSeccion>()
            .HasOne(s => s.PlantillaNotaVinculada).WithMany()
            .HasForeignKey(s => s.PlantillaNotaVinculadaId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ReporteFinancieroCuenta>()
            .HasOne(c => c.Seccion).WithMany(s => s.Cuentas)
            .HasForeignKey(c => c.SeccionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ReporteFinancieroCuenta>()
            .HasOne(c => c.Cuenta).WithMany()
            .HasForeignKey(c => c.CuentaId).OnDelete(DeleteBehavior.Restrict);

        // Enums almacenados como string legible en SQL Server
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.ClrType.GetProperties())
            {
                if (property.PropertyType.IsEnum)
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(property.PropertyType, property.Name)
                        .HasConversion<string>()
                        .HasMaxLength(30);
                }
            }
        }
    }
}
