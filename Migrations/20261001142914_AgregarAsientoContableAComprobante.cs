using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAsientoContableAComprobante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AsientoContableId",
                table: "Comprobantes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_AsientoContableId",
                table: "Comprobantes",
                column: "AsientoContableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comprobantes_AsientosContables_AsientoContableId",
                table: "Comprobantes",
                column: "AsientoContableId",
                principalTable: "AsientosContables",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comprobantes_AsientosContables_AsientoContableId",
                table: "Comprobantes");

            migrationBuilder.DropIndex(
                name: "IX_Comprobantes_AsientoContableId",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "AsientoContableId",
                table: "Comprobantes");
        }
    }
}
