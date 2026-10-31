using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposExtendidosProductoYOrdenCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoAlternativo",
                table: "Productos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescripcionAlternativa",
                table: "Productos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MonedaCompra",
                table: "Productos",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "PEN");

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "Productos",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioCosto",
                table: "Productos",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StockMaximo",
                table: "Productos",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TipoExistencia",
                table: "Productos",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "01");

            migrationBuilder.AddColumn<int>(
                name: "PlazoEntregaDias",
                table: "OrdenesCompra",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoEntrega",
                table: "OrdenesCompra",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "INMEDIATA");

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioCompradorId",
                table: "OrdenesCompra",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_UsuarioCompradorId",
                table: "OrdenesCompra",
                column: "UsuarioCompradorId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesCompra_Usuarios_UsuarioCompradorId",
                table: "OrdenesCompra",
                column: "UsuarioCompradorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesCompra_Usuarios_UsuarioCompradorId",
                table: "OrdenesCompra");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesCompra_UsuarioCompradorId",
                table: "OrdenesCompra");

            migrationBuilder.DropColumn(
                name: "CodigoAlternativo",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "DescripcionAlternativa",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "MonedaCompra",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "PrecioCosto",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "StockMaximo",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "TipoExistencia",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "PlazoEntregaDias",
                table: "OrdenesCompra");

            migrationBuilder.DropColumn(
                name: "TipoEntrega",
                table: "OrdenesCompra");

            migrationBuilder.DropColumn(
                name: "UsuarioCompradorId",
                table: "OrdenesCompra");
        }
    }
}
