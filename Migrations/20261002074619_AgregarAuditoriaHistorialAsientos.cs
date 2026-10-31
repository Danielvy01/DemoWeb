using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarAuditoriaHistorialAsientos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioCreacionId",
                table: "AsientosContables",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UsuarioModificacionId",
                table: "AsientosContables",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "AsientosContables",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Las filas existentes (creadas antes de este módulo de auditoría) arrancan en
            // versión 1, igual que un asiento recién creado.
            migrationBuilder.Sql("UPDATE AsientosContables SET Version = 1 WHERE Version = 0;");

            migrationBuilder.CreateTable(
                name: "AsientosContablesHistorial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstadoAnterior = table.Column<int>(type: "int", nullable: true),
                    EstadoNuevo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SnapshotAnterior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SnapshotNuevo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsientosContablesHistorial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsientosContablesHistorial_AsientosContables_AsientoId",
                        column: x => x.AsientoId,
                        principalTable: "AsientosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsientosContablesHistorial_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContables_UsuarioCreacionId",
                table: "AsientosContables",
                column: "UsuarioCreacionId");

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContables_UsuarioModificacionId",
                table: "AsientosContables",
                column: "UsuarioModificacionId");

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContablesHistorial_AsientoId_Version",
                table: "AsientosContablesHistorial",
                columns: new[] { "AsientoId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContablesHistorial_UsuarioId",
                table: "AsientosContablesHistorial",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_AsientosContables_Usuarios_UsuarioCreacionId",
                table: "AsientosContables",
                column: "UsuarioCreacionId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AsientosContables_Usuarios_UsuarioModificacionId",
                table: "AsientosContables",
                column: "UsuarioModificacionId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AsientosContables_Usuarios_UsuarioCreacionId",
                table: "AsientosContables");

            migrationBuilder.DropForeignKey(
                name: "FK_AsientosContables_Usuarios_UsuarioModificacionId",
                table: "AsientosContables");

            migrationBuilder.DropTable(
                name: "AsientosContablesHistorial");

            migrationBuilder.DropIndex(
                name: "IX_AsientosContables_UsuarioCreacionId",
                table: "AsientosContables");

            migrationBuilder.DropIndex(
                name: "IX_AsientosContables_UsuarioModificacionId",
                table: "AsientosContables");

            migrationBuilder.DropColumn(
                name: "UsuarioCreacionId",
                table: "AsientosContables");

            migrationBuilder.DropColumn(
                name: "UsuarioModificacionId",
                table: "AsientosContables");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "AsientosContables");
        }
    }
}
