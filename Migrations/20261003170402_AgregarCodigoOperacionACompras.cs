using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCodigoOperacionACompras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConfiguracionCuentaOperacionId",
                table: "Compras",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_ConfiguracionCuentaOperacionId",
                table: "Compras",
                column: "ConfiguracionCuentaOperacionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Compras_ConfiguracionesCuentaOperacion_ConfiguracionCuentaOperacionId",
                table: "Compras",
                column: "ConfiguracionCuentaOperacionId",
                principalTable: "ConfiguracionesCuentaOperacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compras_ConfiguracionesCuentaOperacion_ConfiguracionCuentaOperacionId",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_Compras_ConfiguracionCuentaOperacionId",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "ConfiguracionCuentaOperacionId",
                table: "Compras");
        }
    }
}
