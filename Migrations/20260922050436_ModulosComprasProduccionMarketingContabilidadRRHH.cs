using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class ModulosComprasProduccionMarketingContabilidadRRHH : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AsientosContables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Glosa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Referencia = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsientosContables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsientosContables_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Campanas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Asunto = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Mensaje = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaProgramada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campanas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campanas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CuentasContables",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Naturaleza = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CuentaPadreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuentasContables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuentasContables_CuentasContables_CuentaPadreId",
                        column: x => x.CuentaPadreId,
                        principalTable: "CuentasContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuentasContables_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Area = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SalarioBase = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CuentaBancaria = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    BancoNombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CCI = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empleados_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesCompra",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEntregaEsperada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Moneda = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesCompra_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesCompra_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesTrabajo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlmacenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CantidadPlanificada = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    CantidadProducida = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesTrabajo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajo_Almacenes_AlmacenId",
                        column: x => x.AlmacenId,
                        principalTable: "Almacenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajo_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesTrabajo_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Planillas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Periodo = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    TotalBruto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    TotalDescuentos = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    TotalNeto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FechaSincronizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReferenciaSincronizacion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planillas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Planillas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MensajesCampana",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampanaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Destino = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EnviadoAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensajesCampana", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensajesCampana_Campanas_CampanaId",
                        column: x => x.CampanaId,
                        principalTable: "Campanas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MensajesCampana_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AsientoDetalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Debe = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Haber = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Glosa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsientoDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsientoDetalles_AsientosContables_AsientoId",
                        column: x => x.AsientoId,
                        principalTable: "AsientosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsientoDetalles_CuentasContables_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "CuentasContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenCompraDetalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenCompraId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    CantidadRecibida = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenCompraDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenCompraDetalles_OrdenesCompra_OrdenCompraId",
                        column: x => x.OrdenCompraId,
                        principalTable: "OrdenesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenCompraDetalles_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenTrabajoInsumos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrdenTrabajoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CantidadRequerida = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    CantidadConsumida = table.Column<decimal>(type: "decimal(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenTrabajoInsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenTrabajoInsumos_OrdenesTrabajo_OrdenTrabajoId",
                        column: x => x.OrdenTrabajoId,
                        principalTable: "OrdenesTrabajo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenTrabajoInsumos_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanillaDetalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanillaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpleadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SueldoBase = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Bonificaciones = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Descuentos = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Neto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    EstadoPago = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReferenciaPago = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PagadoAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanillaDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanillaDetalles_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanillaDetalles_Planillas_PlanillaId",
                        column: x => x.PlanillaId,
                        principalTable: "Planillas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsientoDetalles_AsientoId",
                table: "AsientoDetalles",
                column: "AsientoId");

            migrationBuilder.CreateIndex(
                name: "IX_AsientoDetalles_CuentaId",
                table: "AsientoDetalles",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContables_EmpresaId_Numero",
                table: "AsientosContables",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Campanas_EmpresaId",
                table: "Campanas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasContables_CuentaPadreId",
                table: "CuentasContables",
                column: "CuentaPadreId");

            migrationBuilder.CreateIndex(
                name: "IX_CuentasContables_EmpresaId_Codigo",
                table: "CuentasContables",
                columns: new[] { "EmpresaId", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_EmpresaId_NumeroDocumento",
                table: "Empleados",
                columns: new[] { "EmpresaId", "NumeroDocumento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MensajesCampana_CampanaId",
                table: "MensajesCampana",
                column: "CampanaId");

            migrationBuilder.CreateIndex(
                name: "IX_MensajesCampana_ClienteId",
                table: "MensajesCampana",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenCompraDetalles_OrdenCompraId",
                table: "OrdenCompraDetalles",
                column: "OrdenCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenCompraDetalles_ProductoId",
                table: "OrdenCompraDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_EmpresaId_Numero",
                table: "OrdenesCompra",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_ProveedorId",
                table: "OrdenesCompra",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_AlmacenId",
                table: "OrdenesTrabajo",
                column: "AlmacenId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_EmpresaId_Numero",
                table: "OrdenesTrabajo",
                columns: new[] { "EmpresaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesTrabajo_ProductoId",
                table: "OrdenesTrabajo",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenTrabajoInsumos_OrdenTrabajoId",
                table: "OrdenTrabajoInsumos",
                column: "OrdenTrabajoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenTrabajoInsumos_ProductoId",
                table: "OrdenTrabajoInsumos",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanillaDetalles_EmpleadoId",
                table: "PlanillaDetalles",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanillaDetalles_PlanillaId",
                table: "PlanillaDetalles",
                column: "PlanillaId");

            migrationBuilder.CreateIndex(
                name: "IX_Planillas_EmpresaId_Periodo",
                table: "Planillas",
                columns: new[] { "EmpresaId", "Periodo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsientoDetalles");

            migrationBuilder.DropTable(
                name: "MensajesCampana");

            migrationBuilder.DropTable(
                name: "OrdenCompraDetalles");

            migrationBuilder.DropTable(
                name: "OrdenTrabajoInsumos");

            migrationBuilder.DropTable(
                name: "PlanillaDetalles");

            migrationBuilder.DropTable(
                name: "AsientosContables");

            migrationBuilder.DropTable(
                name: "CuentasContables");

            migrationBuilder.DropTable(
                name: "Campanas");

            migrationBuilder.DropTable(
                name: "OrdenesCompra");

            migrationBuilder.DropTable(
                name: "OrdenesTrabajo");

            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "Planillas");
        }
    }
}
