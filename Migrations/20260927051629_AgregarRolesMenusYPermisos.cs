using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRolesMenusYPermisos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Menus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Grupo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Menus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EsSistema = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolMenuPermisos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MenuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Lectura = table.Column<bool>(type: "bit", nullable: false),
                    Escritura = table.Column<bool>(type: "bit", nullable: false),
                    ControlTotal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolMenuPermisos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolMenuPermisos_Menus_MenuId",
                        column: x => x.MenuId,
                        principalTable: "Menus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolMenuPermisos_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioRoles",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRoles", x => new { x.UsuarioId, x.RolId });
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Activo", "Codigo", "Grupo", "Nombre", "Orden" },
                values: new object[,]
                {
                    { new Guid("0719223a-b4c0-d383-22aa-c2ef6d179610"), true, "clientes", "Ventas", "Clientes", 10 },
                    { new Guid("10b25bb9-541c-8d1f-42ad-6e8e93f8bde0"), true, "ordenes-compra", "Compras", "Órdenes de Compra", 21 },
                    { new Guid("18164ac4-8694-c02b-c5fc-c8ad35723a41"), true, "reporte-planilla-pagos", "Reportes Financieros", "Planilla de Pagos (Proveedores)", 53 },
                    { new Guid("1a3002db-7b19-ed97-9293-e347f5e05a92"), true, "reporte-ordenes-compra", "Reportes Financieros", "Reporte de Órdenes de Compra", 52 },
                    { new Guid("1a614359-f10e-e00d-fce3-778cfcd2d2fa"), true, "dashboard", "General", "Dashboard", 0 },
                    { new Guid("231bc972-5339-0627-bbce-1c416c85de5c"), true, "planillas", "RRHH", "Planillas", 71 },
                    { new Guid("307209ac-d786-314e-ffa8-0f5b12192b96"), true, "ordenes-trabajo", "Almacén e Inventario", "Órdenes de Trabajo", 33 },
                    { new Guid("3e2474a7-9424-ed0b-4904-7b645ca07ec4"), true, "guias-remision", "Ventas", "Guías de Remisión", 14 },
                    { new Guid("3eee5260-64fc-809f-006a-c97bb8673c91"), true, "kardex", "Almacén e Inventario", "Kardex", 31 },
                    { new Guid("455d5338-5d04-d198-90f4-fe569694669a"), true, "marketing", "Marketing", "Marketing", 80 },
                    { new Guid("516d4853-b9f3-cc99-53f2-ee98182bdfec"), true, "reporte-ventas-detalladas", "Reportes de Ventas", "Ventas Detalladas", 43 },
                    { new Guid("70795886-d16a-b5bd-fe78-beac901ac528"), true, "roles", "Configuración", "Roles y Permisos", 92 },
                    { new Guid("74d98d3e-e011-58f0-e212-f0238db8b9f2"), true, "productos", "Almacén e Inventario", "Productos", 30 },
                    { new Guid("755970c3-2278-9162-c305-b1293c2b60ad"), true, "ventas", "Ventas", "Ventas (Facturación)", 13 },
                    { new Guid("9206712e-f963-db80-ed48-8c591a170fe6"), true, "reporte-registro-ventas", "Reportes de Ventas", "Registro de Ventas", 44 },
                    { new Guid("9254fbaa-643f-4eab-bc2f-efa66cdaba2f"), true, "empleados", "RRHH", "Personal", 70 },
                    { new Guid("98663326-dc75-2d5a-382f-ddcbd064a009"), true, "reporte-compras", "Reportes Financieros", "Reporte de Compras", 51 },
                    { new Guid("a1ec9f74-4a61-d74a-2d97-010c1000fd0a"), true, "reporte-estado-cuenta-cliente", "Reportes de Ventas", "Estado de Cuenta Cliente", 42 },
                    { new Guid("a62c0357-b7a2-1a98-cdd6-5421b29a0b56"), true, "pedidos-venta", "Ventas", "Pedidos de Venta", 12 },
                    { new Guid("ab067c75-2225-92ba-d279-0850e71a9ac6"), true, "cotizaciones", "Ventas", "Cotizaciones", 11 },
                    { new Guid("c1d47172-cf01-6ba2-95d4-0b0e43f02c93"), true, "reporte-canal-ventas", "Reportes de Ventas", "Canal de Ventas", 45 },
                    { new Guid("c2a4597c-61fc-67c0-fae2-3baff4e96f06"), true, "proveedores", "Compras", "Proveedores", 20 },
                    { new Guid("c594c67c-f6ca-4f11-149d-a95d2bec8041"), true, "contabilidad", "Reportes Contables", "Contabilidad", 60 },
                    { new Guid("d0617353-d508-381d-49c8-4623450e13dd"), true, "compras", "Compras", "Registro de Compras", 22 },
                    { new Guid("d10fd02b-a320-2ef3-bcb8-5c7c8657d8f5"), true, "sunat", "Reportes Contables", "Sincronización SUNAT", 61 },
                    { new Guid("d233958a-e3bd-96fd-e344-dd212c628539"), true, "empresa", "Configuración", "Mi Empresa", 90 },
                    { new Guid("d2dead41-6a62-2a05-501c-54fcdae5678e"), true, "reporte-comprobantes-anulados", "Reportes de Ventas", "Comprobantes Anulados", 41 },
                    { new Guid("d445c72d-0924-aa09-cd41-df6cfc2ef145"), true, "inventario-movil", "Almacén e Inventario", "Inventario Móvil", 32 },
                    { new Guid("d8842f43-8882-91bd-0300-c792ee84337b"), true, "ordenes-salida-almacen", "Almacén e Inventario", "Órdenes de Salida", 35 },
                    { new Guid("d8a80161-4052-8707-2565-8a4d7632b2da"), true, "reporte-ventas", "Reportes Financieros", "Reporte de Ventas", 50 },
                    { new Guid("daedf062-a103-e548-a665-95129ba1bd71"), true, "reporte-comprobantes-vigentes", "Reportes de Ventas", "Comprobantes Vigentes", 40 },
                    { new Guid("efb35471-405e-455a-863a-45d436186d74"), true, "reporte-cuenta-corriente-proveedor", "Reportes Financieros", "Cuenta Corriente Proveedor", 54 },
                    { new Guid("f6467005-cbd9-7b0d-8a00-13d545880824"), true, "usuarios", "Configuración", "Usuarios", 91 },
                    { new Guid("fe523557-c416-313c-8eaf-c33e547a9611"), true, "plantillas-orden-trabajo", "Almacén e Inventario", "Plantillas de OT", 34 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Menus_Codigo",
                table: "Menus",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_EmpresaId_Nombre",
                table: "Roles",
                columns: new[] { "EmpresaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolMenuPermisos_MenuId",
                table: "RolMenuPermisos",
                column: "MenuId");

            migrationBuilder.CreateIndex(
                name: "IX_RolMenuPermisos_RolId_MenuId",
                table: "RolMenuPermisos",
                columns: new[] { "RolId", "MenuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_RolId",
                table: "UsuarioRoles",
                column: "RolId");

            // --- Datos: sembrar el rol de sistema "Administrador" para las empresas ya existentes ---
            // (las empresas nuevas lo reciben automáticamente desde EmpresasController/RolSeederService).
            migrationBuilder.Sql(@"
                INSERT INTO Roles (Id, EmpresaId, Nombre, Descripcion, EsSistema, Activo, CreatedAt, UpdatedAt)
                SELECT NEWID(), e.Id, 'Administrador', 'Rol de sistema con acceso total a todas las opciones del ERP.', 1, 1, GETUTCDATE(), GETUTCDATE()
                FROM Empresas e
                WHERE NOT EXISTS (SELECT 1 FROM Roles r WHERE r.EmpresaId = e.Id AND r.EsSistema = 1);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO RolMenuPermisos (Id, RolId, MenuId, Lectura, Escritura, ControlTotal)
                SELECT NEWID(), r.Id, m.Id, 1, 1, 1
                FROM Roles r
                CROSS JOIN Menus m
                WHERE r.EsSistema = 1
                AND NOT EXISTS (SELECT 1 FROM RolMenuPermisos p WHERE p.RolId = r.Id AND p.MenuId = m.Id);
            ");

            // Los usuarios que ya tenían el rol legado ADMIN quedan asignados automáticamente al nuevo rol Administrador.
            migrationBuilder.Sql(@"
                INSERT INTO UsuarioRoles (UsuarioId, RolId, CreatedAt)
                SELECT u.Id, r.Id, GETUTCDATE()
                FROM Usuarios u
                JOIN Roles r ON r.EmpresaId = u.EmpresaId AND r.EsSistema = 1
                WHERE u.Rol = 'ADMIN'
                AND NOT EXISTS (SELECT 1 FROM UsuarioRoles ur WHERE ur.UsuarioId = u.Id AND ur.RolId = r.Id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolMenuPermisos");

            migrationBuilder.DropTable(
                name: "UsuarioRoles");

            migrationBuilder.DropTable(
                name: "Menus");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
