using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCentroCostoIgvGlosasCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CentroCostoId",
                table: "Compras",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaTipoCambio",
                table: "Compras",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GlosaComprobante",
                table: "Compras",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GlosaMovimiento",
                table: "Compras",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Igv",
                table: "Compras",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Compras",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TasaIgv",
                table: "Compras",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 18m);

            // Asigna un centro de costo por defecto ("Operaciones") a las compras ya registradas
            // antes de exigir que el campo sea obligatorio, y desagrega Subtotal/IGV con la tasa por defecto.
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.CentroCostoId = (SELECT TOP 1 cc.Id FROM dbo.CentrosCosto cc WHERE cc.EmpresaId = c.EmpresaId AND cc.Nombre = N'Operaciones')
                FROM dbo.Compras c;
                UPDATE dbo.Compras SET Subtotal = ROUND(Total / (1 + TasaIgv / 100.0), 2), Igv = Total - ROUND(Total / (1 + TasaIgv / 100.0), 2);
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "CentroCostoId",
                table: "Compras",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_CentroCostoId",
                table: "Compras",
                column: "CentroCostoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Compras_CentrosCosto_CentroCostoId",
                table: "Compras",
                column: "CentroCostoId",
                principalTable: "CentrosCosto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compras_CentrosCosto_CentroCostoId",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_Compras_CentroCostoId",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "CentroCostoId",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "FechaTipoCambio",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "GlosaComprobante",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "GlosaMovimiento",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "Igv",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "TasaIgv",
                table: "Compras");
        }
    }
}
