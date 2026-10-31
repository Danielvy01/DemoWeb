using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEjerciciosYPeriodosContables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EjerciciosContables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AsientoAperturaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AsientoCierreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Resultado = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EjerciciosContables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EjerciciosContables_AsientosContables_AsientoAperturaId",
                        column: x => x.AsientoAperturaId,
                        principalTable: "AsientosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EjerciciosContables_AsientosContables_AsientoCierreId",
                        column: x => x.AsientoCierreId,
                        principalTable: "AsientosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EjerciciosContables_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PeriodosContables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EjercicioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosContables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodosContables_EjerciciosContables_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "EjerciciosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PeriodosContables_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EjerciciosContables_AsientoAperturaId",
                table: "EjerciciosContables",
                column: "AsientoAperturaId");

            migrationBuilder.CreateIndex(
                name: "IX_EjerciciosContables_AsientoCierreId",
                table: "EjerciciosContables",
                column: "AsientoCierreId");

            migrationBuilder.CreateIndex(
                name: "IX_EjerciciosContables_EmpresaId_Anio",
                table: "EjerciciosContables",
                columns: new[] { "EmpresaId", "Anio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosContables_EjercicioId",
                table: "PeriodosContables",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosContables_EmpresaId_Anio_Mes",
                table: "PeriodosContables",
                columns: new[] { "EmpresaId", "Anio", "Mes" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PeriodosContables");

            migrationBuilder.DropTable(
                name: "EjerciciosContables");
        }
    }
}
