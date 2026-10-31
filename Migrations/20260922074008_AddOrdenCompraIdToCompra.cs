using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdenCompraIdToCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrdenCompraId",
                table: "Compras",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_OrdenCompraId",
                table: "Compras",
                column: "OrdenCompraId");

            migrationBuilder.AddForeignKey(
                name: "FK_Compras_OrdenesCompra_OrdenCompraId",
                table: "Compras",
                column: "OrdenCompraId",
                principalTable: "OrdenesCompra",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compras_OrdenesCompra_OrdenCompraId",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_Compras_OrdenCompraId",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "OrdenCompraId",
                table: "Compras");
        }
    }
}
