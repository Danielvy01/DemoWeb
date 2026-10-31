using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarMenuCentrosCosto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Activo", "Codigo", "Grupo", "Nombre", "Orden" },
                values: new object[] { new Guid("7a1c2e6f-9b3d-4a10-8f2e-5c6d7e8f9a0b"), true, "centros-costo", "RRHH", "Centros de Costo", 69 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("7a1c2e6f-9b3d-4a10-8f2e-5c6d7e8f9a0b"));
        }
    }
}
