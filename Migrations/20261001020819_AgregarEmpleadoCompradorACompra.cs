using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEmpleadoCompradorACompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmpleadoCompradorId",
                table: "Compras",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_EmpleadoCompradorId",
                table: "Compras",
                column: "EmpleadoCompradorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Compras_Empleados_EmpleadoCompradorId",
                table: "Compras",
                column: "EmpleadoCompradorId",
                principalTable: "Empleados",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compras_Empleados_EmpleadoCompradorId",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_Compras_EmpleadoCompradorId",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "EmpleadoCompradorId",
                table: "Compras");
        }
    }
}
