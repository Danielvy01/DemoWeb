using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoUrlEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Empresas",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Empresas");
        }
    }
}
