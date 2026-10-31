using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarProvisionesYAnalisisCuentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiereAnalisisDocumentario",
                table: "CuentasContables",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "AsientoDetalles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "AsientoDetalles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumento",
                table: "AsientoDetalles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProveedorId",
                table: "AsientoDetalles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentosCtaCte",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsientoDetalleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProveedorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TipoDocumento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Glosa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CentroCostoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MontoOriginal = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    MontoCancelado = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosCtaCte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosCtaCte_AsientoDetalles_AsientoDetalleId",
                        column: x => x.AsientoDetalleId,
                        principalTable: "AsientoDetalles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosCtaCte_CentrosCosto_CentroCostoId",
                        column: x => x.CentroCostoId,
                        principalTable: "CentrosCosto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosCtaCte_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosCtaCte_CuentasContables_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "CuentasContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosCtaCte_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentosCtaCte_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AplicacionesDocumentoCtaCte",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentoCtaCteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Glosa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AplicacionesDocumentoCtaCte", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AplicacionesDocumentoCtaCte_AsientosContables_AsientoId",
                        column: x => x.AsientoId,
                        principalTable: "AsientosContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AplicacionesDocumentoCtaCte_DocumentosCtaCte_DocumentoCtaCteId",
                        column: x => x.DocumentoCtaCteId,
                        principalTable: "DocumentosCtaCte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsientoDetalles_ClienteId",
                table: "AsientoDetalles",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_AsientoDetalles_ProveedorId",
                table: "AsientoDetalles",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesDocumentoCtaCte_AsientoId",
                table: "AplicacionesDocumentoCtaCte",
                column: "AsientoId");

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesDocumentoCtaCte_DocumentoCtaCteId",
                table: "AplicacionesDocumentoCtaCte",
                column: "DocumentoCtaCteId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosCtaCte_AsientoDetalleId",
                table: "DocumentosCtaCte",
                column: "AsientoDetalleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosCtaCte_CentroCostoId",
                table: "DocumentosCtaCte",
                column: "CentroCostoId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosCtaCte_ClienteId",
                table: "DocumentosCtaCte",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosCtaCte_CuentaId",
                table: "DocumentosCtaCte",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosCtaCte_EmpresaId",
                table: "DocumentosCtaCte",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosCtaCte_ProveedorId",
                table: "DocumentosCtaCte",
                column: "ProveedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_AsientoDetalles_Clientes_ClienteId",
                table: "AsientoDetalles",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AsientoDetalles_Proveedores_ProveedorId",
                table: "AsientoDetalles",
                column: "ProveedorId",
                principalTable: "Proveedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AsientoDetalles_Clientes_ClienteId",
                table: "AsientoDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_AsientoDetalles_Proveedores_ProveedorId",
                table: "AsientoDetalles");

            migrationBuilder.DropTable(
                name: "AplicacionesDocumentoCtaCte");

            migrationBuilder.DropTable(
                name: "DocumentosCtaCte");

            migrationBuilder.DropIndex(
                name: "IX_AsientoDetalles_ClienteId",
                table: "AsientoDetalles");

            migrationBuilder.DropIndex(
                name: "IX_AsientoDetalles_ProveedorId",
                table: "AsientoDetalles");

            migrationBuilder.DropColumn(
                name: "RequiereAnalisisDocumentario",
                table: "CuentasContables");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "AsientoDetalles");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "AsientoDetalles");

            migrationBuilder.DropColumn(
                name: "NumeroDocumento",
                table: "AsientoDetalles");

            migrationBuilder.DropColumn(
                name: "ProveedorId",
                table: "AsientoDetalles");
        }
    }
}
