using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCentrosCostoAlModelo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nota: el menú "centros-costo" ya existe en la base de datos desde la migración
            // 20261001000528_AgregarMenuCentrosCosto (InsertData directo); solo faltaba incluirlo
            // en MenuCatalogo.cs para que el modelo/snapshot de EF lo reconozca y el rol
            // Administrador lo reciba automáticamente vía RolSeederService. No se vuelve a
            // insertar aquí para no violar el índice único de Codigo en bases ya migradas.
            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("231bc972-5339-0627-bbce-1c416c85de5c"),
                column: "Orden",
                value: 72);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("231bc972-5339-0627-bbce-1c416c85de5c"),
                column: "Orden",
                value: 71);
        }
    }
}
