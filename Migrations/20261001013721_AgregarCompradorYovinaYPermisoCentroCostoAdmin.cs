using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Siserp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCompradorYovinaYPermisoCentroCostoAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El menú "centros-costo" se agregó en una migración anterior (AgregarMenuCentrosCosto)
            // pero no se incluyó en el catálogo que siembra los permisos del rol Administrador al
            // registrar una empresa; esto otorga, de forma retroactiva, Control Total sobre ese menú
            // a los roles de sistema ya existentes (las empresas nuevas ya lo reciben automáticamente,
            // ver RolSeederService).
            migrationBuilder.Sql(@"
                INSERT INTO dbo.RolMenuPermisos (Id, RolId, MenuId, Lectura, Escritura, ControlTotal)
                SELECT NEWID(), r.Id, m.Id, 1, 1, 1
                FROM dbo.Roles r
                CROSS JOIN dbo.Menus m
                WHERE r.EsSistema = 1
                  AND m.Codigo = N'centros-costo'
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.RolMenuPermisos p WHERE p.RolId = r.Id AND p.MenuId = m.Id
                  );
            ");

            // Alta del comprador "Yovina Marin" para cada empresa existente (Personal con el rol
            // Comprador habilitado, seleccionable como responsable en las Órdenes de Compra).
            migrationBuilder.Sql(@"
                INSERT INTO dbo.Empleados
                    (Id, EmpresaId, NumeroDocumento, Nombres, Apellidos, CentroCostoId, Cargo,
                     EsComprador, SalarioBase, Email, Telefono, CuentaBancaria, BancoNombre, CCI,
                     TipoContrato, RegimenPensionario, FechaIngreso, Activo, FechaCese, MotivoCese,
                     CreatedAt, UpdatedAt)
                SELECT
                    NEWID(), e.Id, N'00000000', N'Yovina', N'Marin',
                    (SELECT TOP 1 cc.Id FROM dbo.CentrosCosto cc WHERE cc.EmpresaId = e.Id ORDER BY CASE WHEN cc.Nombre = N'Operaciones' THEN 0 ELSE 1 END, cc.Nombre),
                    N'Comprador', 1, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL,
                    SYSUTCDATETIME(), 1, NULL, NULL, SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM dbo.Empresas e
                WHERE EXISTS (SELECT 1 FROM dbo.CentrosCosto cc WHERE cc.EmpresaId = e.Id)
                  AND NOT EXISTS (
                      SELECT 1 FROM dbo.Empleados emp
                      WHERE emp.EmpresaId = e.Id AND emp.Nombres = N'Yovina' AND emp.Apellidos = N'Marin'
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE emp FROM dbo.Empleados emp
                WHERE emp.Nombres = N'Yovina' AND emp.Apellidos = N'Marin' AND emp.Cargo = N'Comprador';
            ");

            migrationBuilder.Sql(@"
                DELETE p FROM dbo.RolMenuPermisos p
                INNER JOIN dbo.Roles r ON r.Id = p.RolId
                INNER JOIN dbo.Menus m ON m.Id = p.MenuId
                WHERE r.EsSistema = 1 AND m.Codigo = N'centros-costo';
            ");
        }
    }
}
