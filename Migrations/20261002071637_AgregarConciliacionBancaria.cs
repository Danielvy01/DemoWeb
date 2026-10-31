using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConciliacionBancaria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConciliacionesBancarias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    SaldoInicialBanco = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    SaldoFinalBanco = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    NombreArchivoImportado = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FechaImportacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConciliacionesBancarias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConciliacionesBancarias_CuentasContables_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "CuentasContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConciliacionesBancarias_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosExtractoBancario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConciliacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NumeroOperacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Cargo = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Abono = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    SaldoExtracto = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AsientoDetalleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosExtractoBancario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosExtractoBancario_AsientoDetalles_AsientoDetalleId",
                        column: x => x.AsientoDetalleId,
                        principalTable: "AsientoDetalles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosExtractoBancario_ConciliacionesBancarias_ConciliacionId",
                        column: x => x.ConciliacionId,
                        principalTable: "ConciliacionesBancarias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Activo", "Codigo", "Grupo", "Nombre", "Orden" },
                values: new object[] { new Guid("7b1e4a9d-3c5f-4a8e-9d0b-6e2c1f7a8d35"), true, "conciliacion-bancaria", "Reportes Contables", "Conciliación Bancaria", 65 });

            migrationBuilder.CreateIndex(
                name: "IX_ConciliacionesBancarias_CuentaId",
                table: "ConciliacionesBancarias",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConciliacionesBancarias_EmpresaId_CuentaId_Anio_Mes",
                table: "ConciliacionesBancarias",
                columns: new[] { "EmpresaId", "CuentaId", "Anio", "Mes" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosExtractoBancario_AsientoDetalleId",
                table: "MovimientosExtractoBancario",
                column: "AsientoDetalleId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosExtractoBancario_ConciliacionId",
                table: "MovimientosExtractoBancario",
                column: "ConciliacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MovimientosExtractoBancario");

            migrationBuilder.DropTable(
                name: "ConciliacionesBancarias");

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("7b1e4a9d-3c5f-4a8e-9d0b-6e2c1f7a8d35"));
        }
    }
}
