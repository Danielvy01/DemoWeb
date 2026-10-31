using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDescuentosClasificacionYCategoriaEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoMayoristaPct",
                table: "Productos",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoSuperMayoristaPct",
                table: "Productos",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FotoUrl",
                table: "Productos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Clasificacion",
                table: "Clientes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "NORMAL");

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Categorias",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Categorias",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "EmpresaId",
                table: "Categorias",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("FA2DCF17-A3D0-4724-B41D-7F2C153588E6"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Categorias",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_EmpresaId_Nombre",
                table: "Categorias",
                columns: new[] { "EmpresaId", "Nombre" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Categorias_Empresas_EmpresaId",
                table: "Categorias",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categorias_Empresas_EmpresaId",
                table: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_EmpresaId_Nombre",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "DescuentoMayoristaPct",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "DescuentoSuperMayoristaPct",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "FotoUrl",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Clasificacion",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Categorias");
        }
    }
}
