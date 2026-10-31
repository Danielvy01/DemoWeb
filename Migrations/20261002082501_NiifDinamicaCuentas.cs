using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class NiifDinamicaCuentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AsientoPagoId",
                table: "Planillas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AsientoProvisionId",
                table: "Planillas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReportesFinancierosPlantillas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EsPredeterminada = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportesFinancierosPlantillas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportesFinancierosPlantillas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportesFinancierosSecciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlantillaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeccionPadreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Signo = table.Column<int>(type: "int", nullable: false),
                    EsTotalDestacado = table.Column<bool>(type: "bit", nullable: false),
                    EsResultadoDelEjercicio = table.Column<bool>(type: "bit", nullable: false),
                    ReferenciaNota = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PlantillaNotaVinculadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportesFinancierosSecciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportesFinancierosSecciones_ReportesFinancierosPlantillas_PlantillaId",
                        column: x => x.PlantillaId,
                        principalTable: "ReportesFinancierosPlantillas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportesFinancierosSecciones_ReportesFinancierosPlantillas_PlantillaNotaVinculadaId",
                        column: x => x.PlantillaNotaVinculadaId,
                        principalTable: "ReportesFinancierosPlantillas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportesFinancierosSecciones_ReportesFinancierosSecciones_SeccionPadreId",
                        column: x => x.SeccionPadreId,
                        principalTable: "ReportesFinancierosSecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReportesFinancierosCuentas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeccionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CodigoPrefijo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Signo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportesFinancierosCuentas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportesFinancierosCuentas_CuentasContables_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "CuentasContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportesFinancierosCuentas_ReportesFinancierosSecciones_SeccionId",
                        column: x => x.SeccionId,
                        principalTable: "ReportesFinancierosSecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Activo", "Codigo", "Grupo", "Nombre", "Orden" },
                values: new object[] { new Guid("2e7a9c15-4f8b-4d3a-b6e2-9a1c5f7d0e48"), true, "dinamica-cuentas", "Reportes Contables", "NIIF / Dinámica de Cuentas", 66 });

            migrationBuilder.CreateIndex(
                name: "IX_Planillas_AsientoPagoId",
                table: "Planillas",
                column: "AsientoPagoId");

            migrationBuilder.CreateIndex(
                name: "IX_Planillas_AsientoProvisionId",
                table: "Planillas",
                column: "AsientoProvisionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportesFinancierosCuentas_CuentaId",
                table: "ReportesFinancierosCuentas",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportesFinancierosCuentas_SeccionId",
                table: "ReportesFinancierosCuentas",
                column: "SeccionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportesFinancierosPlantillas_EmpresaId_Codigo",
                table: "ReportesFinancierosPlantillas",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportesFinancierosSecciones_PlantillaId_SeccionPadreId_Orden",
                table: "ReportesFinancierosSecciones",
                columns: new[] { "PlantillaId", "SeccionPadreId", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportesFinancierosSecciones_PlantillaNotaVinculadaId",
                table: "ReportesFinancierosSecciones",
                column: "PlantillaNotaVinculadaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportesFinancierosSecciones_SeccionPadreId",
                table: "ReportesFinancierosSecciones",
                column: "SeccionPadreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Planillas_AsientosContables_AsientoPagoId",
                table: "Planillas",
                column: "AsientoPagoId",
                principalTable: "AsientosContables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Planillas_AsientosContables_AsientoProvisionId",
                table: "Planillas",
                column: "AsientoProvisionId",
                principalTable: "AsientosContables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Planillas_AsientosContables_AsientoPagoId",
                table: "Planillas");

            migrationBuilder.DropForeignKey(
                name: "FK_Planillas_AsientosContables_AsientoProvisionId",
                table: "Planillas");

            migrationBuilder.DropTable(
                name: "ReportesFinancierosCuentas");

            migrationBuilder.DropTable(
                name: "ReportesFinancierosSecciones");

            migrationBuilder.DropTable(
                name: "ReportesFinancierosPlantillas");

            migrationBuilder.DropIndex(
                name: "IX_Planillas_AsientoPagoId",
                table: "Planillas");

            migrationBuilder.DropIndex(
                name: "IX_Planillas_AsientoProvisionId",
                table: "Planillas");

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("2e7a9c15-4f8b-4d3a-b6e2-9a1c5f7d0e48"));

            migrationBuilder.DropColumn(
                name: "AsientoPagoId",
                table: "Planillas");

            migrationBuilder.DropColumn(
                name: "AsientoProvisionId",
                table: "Planillas");
        }
    }
}
