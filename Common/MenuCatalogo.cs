using Siserp.Api.Models;

namespace Siserp.Api.Common;

/// <summary>
/// Catálogo estático de los menús/módulos de la aplicación (uno por pantalla del sidebar).
/// Se usa para sembrar la tabla <see cref="Menu"/> (vía HasData, con Ids fijos y deterministas)
/// y como referencia única de los códigos válidos para el atributo <c>MenuPermisoAttribute</c>.
/// El código de cada entrada coincide con la ruta del frontend (p. ej. "clientes" → /clientes).
/// </summary>
public static class MenuCatalogo
{
    public sealed record Definicion(Guid Id, string Codigo, string Nombre, string Grupo, int Orden);

    public static readonly IReadOnlyList<Definicion> Menus = new List<Definicion>
    {
        new(Guid.Parse("1a614359-f10e-e00d-fce3-778cfcd2d2fa"), "dashboard", "Dashboard", "General", 0),

        new(Guid.Parse("0719223a-b4c0-d383-22aa-c2ef6d179610"), "clientes", "Clientes", "Ventas", 10),
        new(Guid.Parse("ab067c75-2225-92ba-d279-0850e71a9ac6"), "cotizaciones", "Cotizaciones", "Ventas", 11),
        new(Guid.Parse("a62c0357-b7a2-1a98-cdd6-5421b29a0b56"), "pedidos-venta", "Pedidos de Venta", "Ventas", 12),
        new(Guid.Parse("755970c3-2278-9162-c305-b1293c2b60ad"), "ventas", "Ventas (Facturación)", "Ventas", 13),
        new(Guid.Parse("3e2474a7-9424-ed0b-4904-7b645ca07ec4"), "guias-remision", "Guías de Remisión", "Ventas", 14),

        new(Guid.Parse("c2a4597c-61fc-67c0-fae2-3baff4e96f06"), "proveedores", "Proveedores", "Compras", 20),
        new(Guid.Parse("10b25bb9-541c-8d1f-42ad-6e8e93f8bde0"), "ordenes-compra", "Órdenes de Compra", "Compras", 21),
        new(Guid.Parse("d0617353-d508-381d-49c8-4623450e13dd"), "compras", "Registro de Compras", "Compras", 22),

        new(Guid.Parse("74d98d3e-e011-58f0-e212-f0238db8b9f2"), "productos", "Productos", "Almacén e Inventario", 30),
        new(Guid.Parse("3eee5260-64fc-809f-006a-c97bb8673c91"), "kardex", "Kardex", "Almacén e Inventario", 31),
        new(Guid.Parse("d445c72d-0924-aa09-cd41-df6cfc2ef145"), "inventario-movil", "Inventario Móvil", "Almacén e Inventario", 32),
        new(Guid.Parse("307209ac-d786-314e-ffa8-0f5b12192b96"), "ordenes-trabajo", "Órdenes de Trabajo", "Almacén e Inventario", 33),
        new(Guid.Parse("fe523557-c416-313c-8eaf-c33e547a9611"), "plantillas-orden-trabajo", "Plantillas de OT", "Almacén e Inventario", 34),
        new(Guid.Parse("d8842f43-8882-91bd-0300-c792ee84337b"), "ordenes-salida-almacen", "Órdenes de Salida", "Almacén e Inventario", 35),

        new(Guid.Parse("daedf062-a103-e548-a665-95129ba1bd71"), "reporte-comprobantes-vigentes", "Comprobantes Vigentes", "Reportes de Ventas", 40),
        new(Guid.Parse("d2dead41-6a62-2a05-501c-54fcdae5678e"), "reporte-comprobantes-anulados", "Comprobantes Anulados", "Reportes de Ventas", 41),
        new(Guid.Parse("a1ec9f74-4a61-d74a-2d97-010c1000fd0a"), "reporte-estado-cuenta-cliente", "Estado de Cuenta Cliente", "Reportes de Ventas", 42),
        new(Guid.Parse("516d4853-b9f3-cc99-53f2-ee98182bdfec"), "reporte-ventas-detalladas", "Ventas Detalladas", "Reportes de Ventas", 43),
        new(Guid.Parse("9206712e-f963-db80-ed48-8c591a170fe6"), "reporte-registro-ventas", "Registro de Ventas", "Reportes de Ventas", 44),
        new(Guid.Parse("c1d47172-cf01-6ba2-95d4-0b0e43f02c93"), "reporte-canal-ventas", "Canal de Ventas", "Reportes de Ventas", 45),

        new(Guid.Parse("d8a80161-4052-8707-2565-8a4d7632b2da"), "reporte-ventas", "Reporte de Ventas", "Reportes Financieros", 50),
        new(Guid.Parse("98663326-dc75-2d5a-382f-ddcbd064a009"), "reporte-compras", "Reporte de Compras", "Reportes Financieros", 51),
        new(Guid.Parse("1a3002db-7b19-ed97-9293-e347f5e05a92"), "reporte-ordenes-compra", "Reporte de Órdenes de Compra", "Reportes Financieros", 52),
        new(Guid.Parse("18164ac4-8694-c02b-c5fc-c8ad35723a41"), "reporte-planilla-pagos", "Planilla de Pagos (Proveedores)", "Reportes Financieros", 53),
        new(Guid.Parse("efb35471-405e-455a-863a-45d436186d74"), "reporte-cuenta-corriente-proveedor", "Cuenta Corriente Proveedor", "Reportes Financieros", 54),

        new(Guid.Parse("c594c67c-f6ca-4f11-149d-a95d2bec8041"), "contabilidad", "Contabilidad", "Reportes Contables", 60),
        new(Guid.Parse("a3f6e1c4-7b2a-4e9d-8c51-2d6f0a9b1e34"), "exportacion-ple", "Exportación PLE (SUNAT)", "Reportes Contables", 61),
        new(Guid.Parse("d10fd02b-a320-2ef3-bcb8-5c7c8657d8f5"), "sunat", "Sincronización SUNAT", "Reportes Contables", 62),
        new(Guid.Parse("b2e6a6a1-6c3a-4a5a-9f0a-1f6f0c6e7a21"), "activos-fijos", "Activos Fijos", "Reportes Contables", 63),
        new(Guid.Parse("f3a9c8e2-6d4b-4c1a-9e7f-2b8d5a0c1f64"), "presupuestos", "Presupuestos y Control", "Reportes Contables", 64),
        new(Guid.Parse("7b1e4a9d-3c5f-4a8e-9d0b-6e2c1f7a8d35"), "conciliacion-bancaria", "Conciliación Bancaria", "Reportes Contables", 65),
        new(Guid.Parse("2e7a9c15-4f8b-4d3a-b6e2-9a1c5f7d0e48"), "dinamica-cuentas", "NIIF / Dinámica de Cuentas", "Reportes Contables", 66),

        new(Guid.Parse("9254fbaa-643f-4eab-bc2f-efa66cdaba2f"), "empleados", "Personal", "RRHH", 70),
        new(Guid.Parse("7a1c2e6f-9b3d-4a10-8f2e-5c6d7e8f9a0b"), "centros-costo", "Centros de Costo", "RRHH", 69),
        new(Guid.Parse("231bc972-5339-0627-bbce-1c416c85de5c"), "planillas", "Planillas", "RRHH", 72),

        new(Guid.Parse("455d5338-5d04-d198-90f4-fe569694669a"), "marketing", "Marketing", "Marketing", 80),

        new(Guid.Parse("d233958a-e3bd-96fd-e344-dd212c628539"), "empresa", "Mi Empresa", "Configuración", 90),
        new(Guid.Parse("f6467005-cbd9-7b0d-8a00-13d545880824"), "usuarios", "Usuarios", "Configuración", 91),
        new(Guid.Parse("70795886-d16a-b5bd-fe78-beac901ac528"), "roles", "Roles y Permisos", "Configuración", 92),
    };

    public static Guid IdDe(string codigo) => Menus.First(m => m.Codigo == codigo).Id;
}
