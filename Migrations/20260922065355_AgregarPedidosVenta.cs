using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPedidosVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PedidoVentaId",
                table: "Cotizaciones",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PedidosVenta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEntrega = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Moneda = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CotizacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComprobanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosVenta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosVenta_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidosVenta_Comprobantes_ComprobanteId",
                        column: x => x.ComprobanteId,
                        principalTable: "Comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PedidosVenta_Cotizaciones_CotizacionId",
                        column: x => x.CotizacionId,
                        principalTable: "Cotizaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PedidosVenta_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidoVentaDetalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PedidoVentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoVentaDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoVentaDetalles_PedidosVenta_PedidoVentaId",
                        column: x => x.PedidoVentaId,
                        principalTable: "PedidosVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PedidoVentaDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenta_ClienteId",
                table: "PedidosVenta",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenta_ComprobanteId",
                table: "PedidosVenta",
                column: "ComprobanteId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenta_CotizacionId",
                table: "PedidosVenta",
                column: "CotizacionId",
                unique: true,
                filter: "[CotizacionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenta_EmpresaId_Numero",
                table: "PedidosVenta",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidoVentaDetalles_PedidoVentaId",
                table: "PedidoVentaDetalles",
                column: "PedidoVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoVentaDetalles_ProductoId",
                table: "PedidoVentaDetalles",
                column: "ProductoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PedidoVentaDetalles");

            migrationBuilder.DropTable(
                name: "PedidosVenta");

            migrationBuilder.DropColumn(
                name: "PedidoVentaId",
                table: "Cotizaciones");
        }
    }
}
