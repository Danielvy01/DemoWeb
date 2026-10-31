using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Dtos;
using Siserp.Api.Models;

namespace Siserp.Api.Controllers;

[Authorize]
[ApiController]
[Route("almacenes")]
public class AlmacenesController : ControllerBase
{
    private readonly SiserpDbContext _db;

    public AlmacenesController(SiserpDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAlmacenDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var almacen = new Almacen
        {
            EmpresaId = empresaId,
            Nombre = dto.Nombre,
            Direccion = dto.Direccion,
            Principal = dto.Principal ?? false,
        };
        _db.Almacenes.Add(almacen);
        await _db.SaveChangesAsync();
        return Ok(almacen);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        return Ok(await _db.Almacenes.Where(a => a.EmpresaId == empresaId).ToListAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var almacen = await _db.Almacenes.FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);
        if (almacen is null) return NotFound(new { message = "Almacen no encontrado" });
        return Ok(almacen);
    }
}
