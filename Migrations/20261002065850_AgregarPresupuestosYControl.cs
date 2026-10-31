using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPresupuestosYControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PresupuestosAnuales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresupuestosAnuales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PresupuestosAnuales_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PresupuestoDetalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PresupuestoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CentroCostoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Mes = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Glosa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PresupuestoDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PresupuestoDetalles_CentrosCosto_CentroCostoId",
                        column: x => x.CentroCostoId,
                        principalTable: "CentrosCosto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresupuestoDetalles_CuentasContables_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "CuentasContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PresupuestoDetalles_PresupuestosAnuales_PresupuestoId",
                        column: x => x.PresupuestoId,
                        principalTable: "PresupuestosAnuales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Activo", "Codigo", "Grupo", "Nombre", "Orden" },
                values: new object[] { new Guid("f3a9c8e2-6d4b-4c1a-9e7f-2b8d5a0c1f64"), true, "presupuestos", "Reportes Contables", "Presupuestos y Control", 64 });

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestoDetalles_CentroCostoId",
                table: "PresupuestoDetalles",
                column: "CentroCostoId");

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestoDetalles_CuentaId",
                table: "PresupuestoDetalles",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestoDetalles_PresupuestoId_CuentaId_CentroCostoId_Mes",
                table: "PresupuestoDetalles",
                columns: new[] { "PresupuestoId", "CuentaId", "CentroCostoId", "Mes" },
                unique: true,
                filter: "[CentroCostoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PresupuestosAnuales_EmpresaId_Anio",
                table: "PresupuestosAnuales",
                columns: new[] { "EmpresaId", "Anio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PresupuestoDetalles");

            migrationBuilder.DropTable(
                name: "PresupuestosAnuales");

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("f3a9c8e2-6d4b-4c1a-9e7f-2b8d5a0c1f64"));
        }
    }
}
