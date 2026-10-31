using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class GuiasRemisionOrdenesSalidaYPlantillasOT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrdenSalidaAlmacenId",
                table: "OrdenesTrabajo",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PlantillaId",
                table: "OrdenesTrabajo",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GuiasRemision",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AlmacenOrigenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlmacenDestinoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComprobanteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DireccionPartida = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DireccionLlegada = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Transportista = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PlacaVehiculo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MotivoTraslado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuiasRemision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuiasRemision_Almacenes_AlmacenDestinoId",
                        column: x => x.AlmacenDestinoId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuiasRemision_Almacenes_AlmacenOrigenId",
                        column: x => x.AlmacenOrigenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GuiasRemision_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GuiasRemision_Comprobantes_ComprobanteId",
                        column: x => x.ComprobanteId,
                        principalTable: "Comprobantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GuiasRemision_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesSalidaAlmacen",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AlmacenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrdenTrabajoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesSalidaAlmacen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesSalidaAlmacen_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesSalidaAlmacen_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesSalidaAlmacen_OrdenesTrabajo_OrdenTrabajoId",
                        column: x => x.OrdenTrabajoId,
                        principalTable: "OrdenesTrabajo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PlantillasOrdenTrabajo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillasOrdenTrabajo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantillasOrdenTrabajo_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlantillasOrdenTrabajo_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GuiaRemisionDetalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuiaRemisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuiaRemisionDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuiaRemisionDetalles_GuiasRemision_GuiaRemisionId",
                        column: x => x.GuiaRemisionId,
                        principalTable: "GuiasRemision",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuiaRemisionDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenSalidaAlmacenDetalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenSalidaAlmacenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenSalidaAlmacenDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenSalidaAlmacenDetalles_OrdenesSalidaAlmacen_OrdenSalidaAlmacenId",
                        column: x => x.OrdenSalidaAlmacenId,
                        principalTable: "OrdenesSalidaAlmacen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenSalidaAlmacenDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlantillaOrdenTrabajoInsumos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlantillaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CantidadPorUnidad = table.Column<decimal>(type: "decimal(12,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantillaOrdenTrabajoInsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantillaOrdenTrabajoInsumos_PlantillasOrdenTrabajo_PlantillaId",
                        column: x => x.PlantillaId,
                        principalTable: "PlantillasOrdenTrabajo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlantillaOrdenTrabajoInsumos_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_OrdenSalidaAlmacenId",
                table: "OrdenesTrabajo",
                column: "OrdenSalidaAlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_PlantillaId",
                table: "OrdenesTrabajo",
                column: "PlantillaId");

            migrationBuilder.CreateIndex(
                name: "IX_GuiaRemisionDetalles_GuiaRemisionId",
                table: "GuiaRemisionDetalles",
                column: "GuiaRemisionId");

            migrationBuilder.CreateIndex(
                name: "IX_GuiaRemisionDetalles_ProductoId",
                table: "GuiaRemisionDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_GuiasRemision_AlmacenDestinoId",
                table: "GuiasRemision",
                column: "AlmacenDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_GuiasRemision_AlmacenOrigenId",
                table: "GuiasRemision",
                column: "AlmacenOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_GuiasRemision_ClienteId",
                table: "GuiasRemision",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_GuiasRemision_ComprobanteId",
                table: "GuiasRemision",
                column: "ComprobanteId");

            migrationBuilder.CreateIndex(
                name: "IX_GuiasRemision_EmpresaId_Numero",
                table: "GuiasRemision",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesSalidaAlmacen_AlmacenId",
                table: "OrdenesSalidaAlmacen",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesSalidaAlmacen_EmpresaId_Numero",
                table: "OrdenesSalidaAlmacen",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesSalidaAlmacen_OrdenTrabajoId",
                table: "OrdenesSalidaAlmacen",
                column: "OrdenTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenSalidaAlmacenDetalles_OrdenSalidaAlmacenId",
                table: "OrdenSalidaAlmacenDetalles",
                column: "OrdenSalidaAlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenSalidaAlmacenDetalles_ProductoId",
                table: "OrdenSalidaAlmacenDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillaOrdenTrabajoInsumos_PlantillaId",
                table: "PlantillaOrdenTrabajoInsumos",
                column: "PlantillaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillaOrdenTrabajoInsumos_ProductoId",
                table: "PlantillaOrdenTrabajoInsumos",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasOrdenTrabajo_EmpresaId_Nombre",
                table: "PlantillasOrdenTrabajo",
                columns: new[] { "EmpresaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlantillasOrdenTrabajo_ProductoId",
                table: "PlantillasOrdenTrabajo",
                column: "ProductoId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesTrabajo_OrdenesSalidaAlmacen_OrdenSalidaAlmacenId",
                table: "OrdenesTrabajo",
                column: "OrdenSalidaAlmacenId",
                principalTable: "OrdenesSalidaAlmacen",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesTrabajo_PlantillasOrdenTrabajo_PlantillaId",
                table: "OrdenesTrabajo",
                column: "PlantillaId",
                principalTable: "PlantillasOrdenTrabajo",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesTrabajo_OrdenesSalidaAlmacen_OrdenSalidaAlmacenId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesTrabajo_PlantillasOrdenTrabajo_PlantillaId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropTable(
                name: "GuiaRemisionDetalles");

            migrationBuilder.DropTable(
                name: "OrdenSalidaAlmacenDetalles");

            migrationBuilder.DropTable(
                name: "PlantillaOrdenTrabajoInsumos");

            migrationBuilder.DropTable(
                name: "GuiasRemision");

            migrationBuilder.DropTable(
                name: "OrdenesSalidaAlmacen");

            migrationBuilder.DropTable(
                name: "PlantillasOrdenTrabajo");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesTrabajo_OrdenSalidaAlmacenId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesTrabajo_PlantillaId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropColumn(
                name: "OrdenSalidaAlmacenId",
                table: "OrdenesTrabajo");

            migrationBuilder.DropColumn(
                name: "PlantillaId",
                table: "OrdenesTrabajo");
        }
    }
}
