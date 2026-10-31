using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCuentaContableItemOrdenCompraYProductoServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsServicio",
                table: "Productos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CuentaContableId",
                table: "OrdenCompraDetalles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenCompraDetalles_CuentaContableId",
                table: "OrdenCompraDetalles",
                column: "CuentaContableId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenCompraDetalles_CuentasContables_CuentaContableId",
                table: "OrdenCompraDetalles",
                column: "CuentaContableId",
                principalTable: "CuentasContables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenCompraDetalles_CuentasContables_CuentaContableId",
                table: "OrdenCompraDetalles");

            migrationBuilder.DropIndex(
                name: "IX_OrdenCompraDetalles_CuentaContableId",
                table: "OrdenCompraDetalles");

            migrationBuilder.DropColumn(
                name: "EsServicio",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "CuentaContableId",
                table: "OrdenCompraDetalles");
        }
    }
}
