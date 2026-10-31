using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMenuExportacionPle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("b2e6a6a1-6c3a-4a5a-9f0a-1f6f0c6e7a21"),
                column: "Orden",
                value: 63);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d10fd02b-a320-2ef3-bcb8-5c7c8657d8f5"),
                column: "Orden",
                value: 62);

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Activo", "Codigo", "Grupo", "Nombre", "Orden" },
                values: new object[] { new Guid("a3f6e1c4-7b2a-4e9d-8c51-2d6f0a9b1e34"), true, "exportacion-ple", "Reportes Contables", "Exportación PLE (SUNAT)", 61 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("a3f6e1c4-7b2a-4e9d-8c51-2d6f0a9b1e34"));

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("b2e6a6a1-6c3a-4a5a-9f0a-1f6f0c6e7a21"),
                column: "Orden",
                value: 62);

            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("d10fd02b-a320-2ef3-bcb8-5c7c8657d8f5"),
                column: "Orden",
                value: 61);
        }
    }
}
