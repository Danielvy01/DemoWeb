using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditoBloqueoBajaCotizaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Proveedores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Bloqueado",
                table: "Proveedores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DiasCredito",
                table: "Proveedores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OrdenCompraId",
                table: "OrdenesTrabajo",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ComprobanteId",
                table: "OrdenesCompra",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCese",
                table: "Empleados",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCese",
                table: "Empleados",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegimenPensionario",
                table: "Empleados",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoContrato",
                table: "Empleados",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPago",
                table: "Comprobantes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "Comprobantes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Pagado",
                table: "Comprobantes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPago",
                table: "Compras",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "Compras",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Pagado",
                table: "Compras",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Clientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Bloqueado",
                table: "Clientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DiasCredito",
                table: "Clientes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Cotizaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaValidez = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Moneda = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ComprobanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cotizaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cotizaciones_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cotizaciones_Comprobantes_ComprobanteId",
                        column: x => x.ComprobanteId,
                        principalTable: "Comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Cotizaciones_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CotizacionDetalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CotizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CotizacionDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CotizacionDetalles_Cotizaciones_CotizacionId",
                        column: x => x.CotizacionId,
                        principalTable: "Cotizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CotizacionDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_OrdenCompraId",
                table: "OrdenesTrabajo",
                column: "OrdenCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_ComprobanteId",
                table: "OrdenesCompra",
                column: "ComprobanteId");

            migrationBuilder.CreateIndex(
                name: "IX_CotizacionDetalles_CotizacionId",
                table: "CotizacionDetalles",
                column: "CotizacionId");

            migrationBuilder.CreateIndex(
                name: "IX_CotizacionDetalles_ProductoId",
                table: "CotizacionDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotizaciones_ClienteId",
                table: "Cotizaciones",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotizaciones_ComprobanteId",
                table: "Cotizaciones",
                column: "ComprobanteId");

            migrationBuilder.CreateIndex(
                name: "IX_Cotizaciones_EmpresaId_Numero",
                table: "Cotizaciones",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesCompra_Comprobantes_ComprobanteId",
                table: "OrdenesCompra",
                column: "ComprobanteId",
                principalTable: "Comprobantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesTrabajo_OrdenesCompra_OrdenCompraId",
                table: "OrdenesTrabajo",
                column: "OrdenCompraId",
                principalTable: "OrdenesCompra",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesCompra_Comprobantes_ComprobanteId",
                table: "OrdenesCompra");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesTrabajo_OrdenesCompra_OrdenCompraId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropTable(
                name: "CotizacionDetalles");

            migrationBuilder.DropTable(
                name: "Cotizaciones");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesTrabajo_OrdenCompraId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesCompra_ComprobanteId",
                table: "OrdenesCompra");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "Bloqueado",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "DiasCredito",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "OrdenCompraId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropColumn(
                name: "ComprobanteId",
                table: "OrdenesCompra");

            migrationBuilder.DropColumn(
                name: "FechaCese",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "MotivoCese",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "RegimenPensionario",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "TipoContrato",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "FechaPago",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "Pagado",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "FechaPago",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "Pagado",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Bloqueado",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "DiasCredito",
                table: "Clientes");
        }
    }
}
