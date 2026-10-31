using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCanalVentaComprobante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioLista",
                table: "Productos",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AlmacenId",
                table: "Comprobantes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanalVenta",
                table: "Comprobantes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "LOCAL");

            migrationBuilder.AddColumn<Guid>(
                name: "ComprobanteReferenciaId",
                table: "Comprobantes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CondicionPago",
                table: "Comprobantes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MotivoNota",
                table: "Comprobantes",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoOperacion",
                table: "Comprobantes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "VendedorId",
                table: "Comprobantes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ComprobantePagos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComprobanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComprobantePagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComprobantePagos_Comprobantes_ComprobanteId",
                        column: x => x.ComprobanteId,
                        principalTable: "Comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductoComponentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoTerminadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsumoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(12,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductoComponentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductoComponentes_Productos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductoComponentes_Productos_ProductoTerminadoId",
                        column: x => x.ProductoTerminadoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_AlmacenId",
                table: "Comprobantes",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_ComprobanteReferenciaId",
                table: "Comprobantes",
                column: "ComprobanteReferenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_VendedorId",
                table: "Comprobantes",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_ComprobantePagos_ComprobanteId",
                table: "ComprobantePagos",
                column: "ComprobanteId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoComponentes_InsumoId",
                table: "ProductoComponentes",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductoComponentes_ProductoTerminadoId",
                table: "ProductoComponentes",
                column: "ProductoTerminadoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comprobantes_Almacenes_AlmacenId",
                table: "Comprobantes",
                column: "AlmacenId",
                principalTable: "Almacenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Comprobantes_Comprobantes_ComprobanteReferenciaId",
                table: "Comprobantes",
                column: "ComprobanteReferenciaId",
                principalTable: "Comprobantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comprobantes_Usuarios_VendedorId",
                table: "Comprobantes",
                column: "VendedorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comprobantes_Almacenes_AlmacenId",
                table: "Comprobantes");

            migrationBuilder.DropForeignKey(
                name: "FK_Comprobantes_Comprobantes_ComprobanteReferenciaId",
                table: "Comprobantes");

            migrationBuilder.DropForeignKey(
                name: "FK_Comprobantes_Usuarios_VendedorId",
                table: "Comprobantes");

            migrationBuilder.DropTable(
                name: "ComprobantePagos");

            migrationBuilder.DropTable(
                name: "ProductoComponentes");

            migrationBuilder.DropIndex(
                name: "IX_Comprobantes_AlmacenId",
                table: "Comprobantes");

            migrationBuilder.DropIndex(
                name: "IX_Comprobantes_ComprobanteReferenciaId",
                table: "Comprobantes");

            migrationBuilder.DropIndex(
                name: "IX_Comprobantes_VendedorId",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "PrecioLista",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "AlmacenId",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "CanalVenta",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "ComprobanteReferenciaId",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "CondicionPago",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "MotivoNota",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "TipoOperacion",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "VendedorId",
                table: "Comprobantes");
        }
    }
}
