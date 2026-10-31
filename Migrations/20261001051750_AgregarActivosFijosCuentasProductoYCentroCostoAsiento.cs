using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarActivosFijosCuentasProductoYCentroCostoAsiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CuentaCostoVentaId",
                table: "Productos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CuentaExistenciasId",
                table: "Productos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CuentaVentaId",
                table: "Productos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CentroCostoId",
                table: "Comprobantes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CentroCostoId",
                table: "AsientoDetalles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivosFijos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Categoria = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CentroCostoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaAdquisicion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaInicioDepreciacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValorAdquisicion = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ValorResidual = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    VidaUtilMeses = table.Column<int>(type: "int", nullable: false),
                    MetodoDepreciacion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CuentaActivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaDepreciacionAcumuladaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaGastoDepreciacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaBaja = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoBaja = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivosFijos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivosFijos_CentrosCosto_CentroCostoId",
                        column: x => x.CentroCostoId,
                        principalTable: "CentrosCosto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivosFijos_CuentasContables_CuentaActivoId",
                        column: x => x.CuentaActivoId,
                        principalTable: "CuentasContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivosFijos_CuentasContables_CuentaDepreciacionAcumuladaId",
                        column: x => x.CuentaDepreciacionAcumuladaId,
                        principalTable: "CuentasContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivosFijos_CuentasContables_CuentaGastoDepreciacionId",
                        column: x => x.CuentaGastoDepreciacionId,
                        principalTable: "CuentasContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivosFijos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DepreciacionesActivoFijo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivoFijoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    FechaProceso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoDepreciacion = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    DepreciacionAcumulada = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    AsientoContableId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepreciacionesActivoFijo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepreciacionesActivoFijo_ActivosFijos_ActivoFijoId",
                        column: x => x.ActivoFijoId,
                        principalTable: "ActivosFijos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DepreciacionesActivoFijo_AsientosContables_AsientoContableId",
                        column: x => x.AsientoContableId,
                        principalTable: "AsientosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Menus",
                columns: new[] { "Id", "Activo", "Codigo", "Grupo", "Nombre", "Orden" },
                values: new object[] { new Guid("b2e6a6a1-6c3a-4a5a-9f0a-1f6f0c6e7a21"), true, "activos-fijos", "Reportes Contables", "Activos Fijos", 62 });

            // Otorga, de forma retroactiva, Control Total sobre el nuevo menú "activos-fijos" a los
            // roles de sistema ya existentes (las empresas nuevas ya lo reciben automáticamente,
            // ver RolSeederService, que siembra todos los menús activos al crear la empresa).
            migrationBuilder.Sql(@"
                INSERT INTO dbo.RolMenuPermisos (Id, RolId, MenuId, Lectura, Escritura, ControlTotal)
                SELECT NEWID(), r.Id, m.Id, 1, 1, 1
                FROM dbo.Roles r
                CROSS JOIN dbo.Menus m
                WHERE r.EsSistema = 1
                  AND m.Codigo = N'activos-fijos'
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.RolMenuPermisos p WHERE p.RolId = r.Id AND p.MenuId = m.Id
                  );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CuentaCostoVentaId",
                table: "Productos",
                column: "CuentaCostoVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CuentaExistenciasId",
                table: "Productos",
                column: "CuentaExistenciasId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CuentaVentaId",
                table: "Productos",
                column: "CuentaVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_CentroCostoId",
                table: "Comprobantes",
                column: "CentroCostoId");

            migrationBuilder.CreateIndex(
                name: "IX_AsientoDetalles_CentroCostoId",
                table: "AsientoDetalles",
                column: "CentroCostoId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivosFijos_CentroCostoId",
                table: "ActivosFijos",
                column: "CentroCostoId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivosFijos_CuentaActivoId",
                table: "ActivosFijos",
                column: "CuentaActivoId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivosFijos_CuentaDepreciacionAcumuladaId",
                table: "ActivosFijos",
                column: "CuentaDepreciacionAcumuladaId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivosFijos_CuentaGastoDepreciacionId",
                table: "ActivosFijos",
                column: "CuentaGastoDepreciacionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivosFijos_EmpresaId_Codigo",
                table: "ActivosFijos",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepreciacionesActivoFijo_ActivoFijoId_Periodo",
                table: "DepreciacionesActivoFijo",
                columns: new[] { "ActivoFijoId", "Periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepreciacionesActivoFijo_AsientoContableId",
                table: "DepreciacionesActivoFijo",
                column: "AsientoContableId");

            migrationBuilder.AddForeignKey(
                name: "FK_AsientoDetalles_CentrosCosto_CentroCostoId",
                table: "AsientoDetalles",
                column: "CentroCostoId",
                principalTable: "CentrosCosto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Comprobantes_CentrosCosto_CentroCostoId",
                table: "Comprobantes",
                column: "CentroCostoId",
                principalTable: "CentrosCosto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CuentasContables_CuentaCostoVentaId",
                table: "Productos",
                column: "CuentaCostoVentaId",
                principalTable: "CuentasContables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CuentasContables_CuentaExistenciasId",
                table: "Productos",
                column: "CuentaExistenciasId",
                principalTable: "CuentasContables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CuentasContables_CuentaVentaId",
                table: "Productos",
                column: "CuentaVentaId",
                principalTable: "CuentasContables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AsientoDetalles_CentrosCosto_CentroCostoId",
                table: "AsientoDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_Comprobantes_CentrosCosto_CentroCostoId",
                table: "Comprobantes");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CuentasContables_CuentaCostoVentaId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CuentasContables_CuentaExistenciasId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CuentasContables_CuentaVentaId",
                table: "Productos");

            migrationBuilder.DropTable(
                name: "DepreciacionesActivoFijo");

            migrationBuilder.DropTable(
                name: "ActivosFijos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_CuentaCostoVentaId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_CuentaExistenciasId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_CuentaVentaId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Comprobantes_CentroCostoId",
                table: "Comprobantes");

            migrationBuilder.DropIndex(
                name: "IX_AsientoDetalles_CentroCostoId",
                table: "AsientoDetalles");

            migrationBuilder.DeleteData(
                table: "Menus",
                keyColumn: "Id",
                keyValue: new Guid("b2e6a6a1-6c3a-4a5a-9f0a-1f6f0c6e7a21"));

            migrationBuilder.Sql(@"
                DELETE p FROM dbo.RolMenuPermisos p
                INNER JOIN dbo.Roles r ON r.Id = p.RolId
                INNER JOIN dbo.Menus m ON m.Id = p.MenuId
                WHERE r.EsSistema = 1 AND m.Codigo = N'activos-fijos';
            ");

            migrationBuilder.DropColumn(
                name: "CuentaCostoVentaId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "CuentaExistenciasId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "CuentaVentaId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "CentroCostoId",
                table: "Comprobantes");

            migrationBuilder.DropColumn(
                name: "CentroCostoId",
                table: "AsientoDetalles");
        }
    }
}
