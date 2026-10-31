using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMultimonedaAsientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DebeMe",
                table: "AsientoDetalles",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HaberMe",
                table: "AsientoDetalles",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Moneda",
                table: "AsientoDetalles",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "PEN");

            migrationBuilder.AddColumn<decimal>(
                name: "TipoCambio",
                table: "AsientoDetalles",
                type: "decimal(8,4)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumento",
                table: "AsientoDetalles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Las líneas de asientos ya existentes se consideran en soles (PEN) 1:1; y su
            // DebeMe/HaberMe (importe en moneda de origen) se iguala a su Debe/Haber en soles.
            migrationBuilder.Sql("UPDATE AsientoDetalles SET DebeMe = Debe, HaberMe = Haber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebeMe",
                table: "AsientoDetalles");

            migrationBuilder.DropColumn(
                name: "HaberMe",
                table: "AsientoDetalles");

            migrationBuilder.DropColumn(
                name: "Moneda",
                table: "AsientoDetalles");

            migrationBuilder.DropColumn(
                name: "TipoCambio",
                table: "AsientoDetalles");

            migrationBuilder.DropColumn(
                name: "TipoDocumento",
                table: "AsientoDetalles");
        }
    }
}
