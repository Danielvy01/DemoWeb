using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCodigoOperacionYSubdiario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Subdiario",
                table: "AsientosContables",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ConfiguracionesCuentaOperacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Movimiento = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TipoDocumento = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Moneda = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    CuentaContableId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Subdiario = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesCuentaOperacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesCuentaOperacion_CuentasContables_CuentaContableId",
                        column: x => x.CuentaContableId,
                        principalTable: "CuentasContables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesCuentaOperacion_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesCuentaOperacion_CuentaContableId",
                table: "ConfiguracionesCuentaOperacion",
                column: "CuentaContableId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesCuentaOperacion_EmpresaId_Movimiento_TipoDocumento_Moneda",
                table: "ConfiguracionesCuentaOperacion",
                columns: new[] { "EmpresaId", "Movimiento", "TipoDocumento", "Moneda" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionesCuentaOperacion");

            migrationBuilder.DropColumn(
                name: "Subdiario",
                table: "AsientosContables");
        }
    }
}
