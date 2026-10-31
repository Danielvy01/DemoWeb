using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Dtos;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;
using Siserp.Api.Services;

namespace Siserp.Api.Controllers;

// Nota: el registro de empresa/tenant no requiere JWT porque es el primer
// paso de onboarding (crear la empresa antes de poder crear usuarios).
[ApiController]
[Route("empresas")]
public class EmpresasController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly RolSeederService _rolSeeder;

    public EmpresasController(SiserpDbContext db, IWebHostEnvironment env, RolSeederService rolSeeder)
    {
        _db = db;
        _env = env;
        _rolSeeder = rolSeeder;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEmpresaDto dto)
    {
        var empresa = new Empresa
        {
            Ruc = dto.Ruc,
            RazonSocial = dto.RazonSocial,
            NombreComercial = dto.NombreComercial,
            Direccion = dto.Direccion,
            Ubigeo = dto.Ubigeo,
        };
        _db.Empresas.Add(empresa);
        await _db.SaveChangesAsync();

        // Toda empresa nace con un rol "Administrador" (Control Total en todos los menús).
        await _rolSeeder.CrearRolAdministradorAsync(empresa.Id);

        return Ok(empresa);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll() => Ok(await _db.Empresas.ToListAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresa = await _db.Empresas.FindAsync(id);
        if (empresa is null) return NotFound(new { message = "Empresa no encontrada" });
        return Ok(empresa);
    }

    /// <summary>Datos de la empresa del usuario autenticado (para la pantalla de configuración).</summary>
    [Authorize]
    [MenuPermiso("empresa")]
    [HttpGet("actual")]
    public async Task<IActionResult> Actual()
    {
        var empresa = await _db.Empresas.FindAsync(User.GetEmpresaId());
        if (empresa is null) return NotFound(new { message = "Empresa no encontrada" });
        return Ok(empresa);
    }

    /// <summary>Actualiza los datos generales de la empresa del usuario autenticado.</summary>
    [Authorize]
    [MenuPermiso("empresa")]
    [HttpPut("actual")]
    public async Task<IActionResult> Actualizar(UpdateEmpresaDto dto)
    {
        var empresa = await _db.Empresas.FindAsync(User.GetEmpresaId());
        if (empresa is null) return NotFound(new { message = "Empresa no encontrada" });

        if (dto.RazonSocial is not null) empresa.RazonSocial = dto.RazonSocial;
        if (dto.NombreComercial is not null) empresa.NombreComercial = dto.NombreComercial;
        if (dto.Direccion is not null) empresa.Direccion = dto.Direccion;
        if (dto.Ubigeo is not null) empresa.Ubigeo = dto.Ubigeo;
        empresa.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(empresa);
    }

    /// <summary>Sube/reemplaza el logo de la empresa; se guarda en wwwroot/uploads/empresas y se usa en la cabecera de los PDF generados.</summary>
    [Authorize]
    [MenuPermiso("empresa")]
    [HttpPost("actual/logo")]
    public async Task<IActionResult> SubirLogo(IFormFile logo)
    {
        var empresa = await _db.Empresas.FindAsync(User.GetEmpresaId());
        if (empresa is null) return NotFound(new { message = "Empresa no encontrada" });
        if (logo is null || logo.Length == 0) return BadRequest(new { message = "Debe adjuntar un archivo de imagen" });

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(logo.FileName).ToLowerInvariant();
        if (!extensionesPermitidas.Contains(extension))
        {
            return BadRequest(new { message = "Formato de imagen no soportado (use jpg, png, webp o gif)" });
        }

        var carpeta = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "empresas");
        Directory.CreateDirectory(carpeta);
        var nombreArchivo = $"{empresa.Id}{extension}";
        var rutaFisica = Path.Combine(carpeta, nombreArchivo);
        await using (var stream = System.IO.File.Create(rutaFisica))
        {
            await logo.CopyToAsync(stream);
        }

        empresa.LogoUrl = $"/uploads/empresas/{nombreArchivo}";
        empresa.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(empresa);
    }
}
