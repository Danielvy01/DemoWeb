using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPermisoExportacionPleAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El menú "exportacion-ple" se agregó en AgregarMenuExportacionPle pero no se incluyó
            // en el catálogo que siembra los permisos del rol Administrador al registrar una
            // empresa; esto otorga, de forma retroactiva, Control Total sobre ese menú a los roles
            // de sistema ya existentes (las empresas nuevas ya lo reciben automáticamente, ver
            // RolSeederService).
            migrationBuilder.Sql(@"
                INSERT INTO dbo.RolMenuPermisos (Id, RolId, MenuId, Lectura, Escritura, ControlTotal)
                SELECT NEWID(), r.Id, m.Id, 1, 1, 1
                FROM dbo.Roles r
                CROSS JOIN dbo.Menus m
                WHERE r.EsSistema = 1
                  AND m.Codigo = N'exportacion-ple'
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.RolMenuPermisos p WHERE p.RolId = r.Id AND p.MenuId = m.Id
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE p FROM dbo.RolMenuPermisos p
                INNER JOIN dbo.Roles r ON r.Id = p.RolId
                INNER JOIN dbo.Menus m ON m.Id = p.MenuId
                WHERE r.EsSistema = 1 AND m.Codigo = N'exportacion-ple';
            ");
        }
    }
}
