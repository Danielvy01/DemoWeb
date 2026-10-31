using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCuentaContableYAsientoACompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AsientoContableId",
                table: "Compras",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CuentaContableId",
                table: "CompraDetalles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_AsientoContableId",
                table: "Compras",
                column: "AsientoContableId");

            migrationBuilder.CreateIndex(
                name: "IX_CompraDetalles_CuentaContableId",
                table: "CompraDetalles",
                column: "CuentaContableId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompraDetalles_CuentasContables_CuentaContableId",
                table: "CompraDetalles",
                column: "CuentaContableId",
                principalTable: "CuentasContables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Compras_AsientosContables_AsientoContableId",
                table: "Compras",
                column: "AsientoContableId",
                principalTable: "AsientosContables",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompraDetalles_CuentasContables_CuentaContableId",
                table: "CompraDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_Compras_AsientosContables_AsientoContableId",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_Compras_AsientoContableId",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_CompraDetalles_CuentaContableId",
                table: "CompraDetalles");

            migrationBuilder.DropColumn(
                name: "AsientoContableId",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "CuentaContableId",
                table: "CompraDetalles");
        }
    }
}
