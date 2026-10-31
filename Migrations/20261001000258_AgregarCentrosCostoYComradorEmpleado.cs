using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCentrosCostoYComradorEmpleado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesCompra_Usuarios_UsuarioCompradorId",
                table: "OrdenesCompra");

            migrationBuilder.RenameColumn(
                name: "UsuarioCompradorId",
                table: "OrdenesCompra",
                newName: "EmpleadoCompradorId");

            migrationBuilder.RenameIndex(
                name: "IX_OrdenesCompra_UsuarioCompradorId",
                table: "OrdenesCompra",
                newName: "IX_OrdenesCompra_EmpleadoCompradorId");

            // Nullable temporalmente: se completa con datos migrados desde "Area" antes de exigir NOT NULL.
            migrationBuilder.AddColumn<Guid>(
                name: "CentroCostoId",
                table: "Empleados",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsComprador",
                table: "Empleados",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CentrosCosto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CentrosCosto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CentrosCosto_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CentrosCosto_EmpresaId_Nombre",
                table: "CentrosCosto",
                columns: new[] { "EmpresaId", "Nombre" },
                unique: true);

            // Siembra los centros de costo base (Operaciones, Ventas, Sistemas) para cada empresa existente.
            migrationBuilder.Sql(@"
                INSERT INTO dbo.CentrosCosto (Id, EmpresaId, Nombre, Descripcion, Activo, CreatedAt, UpdatedAt)
                SELECT NEWID(), e.Id, v.Nombre, NULL, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM dbo.Empresas e
                CROSS JOIN (VALUES (N'Operaciones'), (N'Ventas'), (N'Sistemas')) AS v(Nombre)
                WHERE NOT EXISTS (SELECT 1 FROM dbo.CentrosCosto c WHERE c.EmpresaId = e.Id AND c.Nombre = v.Nombre);
            ");

            // Migra el personal existente: hace calzar el texto libre "Area" con el Centro de Costo
            // homónimo y, si no coincide ninguno, lo asigna por defecto a "Operaciones".
            migrationBuilder.Sql(@"
                UPDATE emp
                SET emp.CentroCostoId = COALESCE(
                    (SELECT TOP 1 c.Id FROM dbo.CentrosCosto c WHERE c.EmpresaId = emp.EmpresaId AND c.Nombre = emp.Area),
                    (SELECT TOP 1 c.Id FROM dbo.CentrosCosto c WHERE c.EmpresaId = emp.EmpresaId AND c.Nombre = N'Operaciones')
                )
                FROM dbo.Empleados emp;
            ");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "Empleados");

            migrationBuilder.AlterColumn<Guid>(
                name: "CentroCostoId",
                table: "Empleados",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_CentroCostoId",
                table: "Empleados",
                column: "CentroCostoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Empleados_CentrosCosto_CentroCostoId",
                table: "Empleados",
                column: "CentroCostoId",
                principalTable: "CentrosCosto",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Los valores existentes de este campo apuntaban a Usuarios (login), no a Personal;
            // se limpian porque ya no son compatibles con la nueva referencia a Empleados.
            migrationBuilder.Sql(@"UPDATE dbo.OrdenesCompra SET EmpleadoCompradorId = NULL WHERE EmpleadoCompradorId IS NOT NULL;");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesCompra_Empleados_EmpleadoCompradorId",
                table: "OrdenesCompra",
                column: "EmpleadoCompradorId",
                principalTable: "Empleados",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Empleados_CentrosCosto_CentroCostoId",
                table: "Empleados");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesCompra_Empleados_EmpleadoCompradorId",
                table: "OrdenesCompra");

            migrationBuilder.DropTable(
                name: "CentrosCosto");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_CentroCostoId",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "CentroCostoId",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "EsComprador",
                table: "Empleados");

            migrationBuilder.RenameColumn(
                name: "EmpleadoCompradorId",
                table: "OrdenesCompra",
                newName: "UsuarioCompradorId");

            migrationBuilder.RenameIndex(
                name: "IX_OrdenesCompra_EmpleadoCompradorId",
                table: "OrdenesCompra",
                newName: "IX_OrdenesCompra_UsuarioCompradorId");

            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "Empleados",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesCompra_Usuarios_UsuarioCompradorId",
                table: "OrdenesCompra",
                column: "UsuarioCompradorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
