using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Infrastructure.Authorization;

namespace Siserp.Api.Controllers;

/// <summary>Catálogo (solo lectura) de menús/módulos, usado por el maestro de Roles para armar la matriz de permisos.</summary>
[Authorize]
[MenuPermiso("roles", Models.NivelAcceso.LECTURA)]
[ApiController]
[Route("menus")]
public class MenusController : ControllerBase
{
    private readonly SiserpDbContext _db;

    public MenusController(SiserpDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var menus = await _db.Menus
            .Where(m => m.Activo)
            .OrderBy(m => m.Orden)
            .Select(m => new { m.Id, m.Codigo, m.Nombre, m.Grupo, m.Orden })
            .ToListAsync();
        return Ok(menus);
    }
}
