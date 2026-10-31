using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentoSunatCompraYTipoCambio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocalProveedor",
                table: "Compras",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroGuiaRemision",
                table: "Compras",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Referencia",
                table: "Compras",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TipoCambio",
                table: "Compras",
                type: "decimal(8,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento",
                table: "Compras",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoOperacionSunat",
                table: "Compras",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeDescuento",
                table: "CompraDetalles",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "TiposCambio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Compra = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    Venta = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposCambio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TiposCambio_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TiposCambio_EmpresaId_Fecha",
                table: "TiposCambio",
                columns: new[] { "EmpresaId", "Fecha" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TiposCambio");

            migrationBuilder.DropColumn(
                name: "LocalProveedor",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "NumeroGuiaRemision",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "Referencia",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "TipoCambio",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "TipoOperacionSunat",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "PorcentajeDescuento",
                table: "CompraDetalles");
        }
    }
}
