using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAlmacenIdToCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AlmacenId",
                table: "Compras",
                type: "uniqueidentifier",
                nullable: true);

            // Backfill: las compras ya registradas no guardaban el almacén de destino, así que se
            // les asigna el primer almacén activo de su misma empresa (dato informativo/histórico;
            // no vuelve a afectar el stock ya ingresado).
            migrationBuilder.Sql(@"
                UPDATE c
                SET c.AlmacenId = (
                    SELECT TOP 1 a.Id FROM Almacenes a WHERE a.EmpresaId = c.EmpresaId ORDER BY a.Principal DESC, a.Id
                )
                FROM Compras c
                WHERE c.AlmacenId IS NULL;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "AlmacenId",
                table: "Compras",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true,
                defaultValue: Guid.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_Compras_AlmacenId",
                table: "Compras",
                column: "AlmacenId");

            migrationBuilder.AddForeignKey(
                name: "FK_Compras_Almacenes_AlmacenId",
                table: "Compras",
                column: "AlmacenId",
                principalTable: "Almacenes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Compras_Almacenes_AlmacenId",
                table: "Compras");

            migrationBuilder.DropIndex(
                name: "IX_Compras_AlmacenId",
                table: "Compras");

            migrationBuilder.DropColumn(
                name: "AlmacenId",
                table: "Compras");
        }
    }
}
