using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Dtos;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;

namespace Siserp.Api.Controllers;

/// <summary>Proveedores: se usan como destino de las órdenes de compra y las compras directas.</summary>
[Authorize]
[MenuPermiso("proveedores")]
[ApiController]
[Route("proveedores")]
public class ProveedoresController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly IProveedorRepository _proveedorRepository;

    public ProveedoresController(SiserpDbContext db, IProveedorRepository proveedorRepository)
    {
        _db = db;
        _proveedorRepository = proveedorRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProveedorDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var proveedor = new Proveedor
        {
            EmpresaId = empresaId,
            Ruc = dto.Ruc,
            RazonSocial = dto.RazonSocial,
            Direccion = dto.Direccion,
            Email = dto.Email,
            Telefono = dto.Telefono,
            DiasCredito = dto.DiasCredito ?? 0,
        };
        // Alta a través del stored procedure sp_Proveedor_Registrar.
        var creado = await _proveedorRepository.RegistrarAsync(proveedor);
        return Ok(creado);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        var proveedores = await _db.Proveedores
            .Where(p => p.EmpresaId == empresaId)
            .OrderBy(p => p.RazonSocial)
            .ToListAsync();
        return Ok(proveedores);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (proveedor is null) return NotFound(new { message = "Proveedor no encontrado" });
        return Ok(proveedor);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateProveedorDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var proveedor = await _db.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (proveedor is null) return NotFound(new { message = "Proveedor no encontrado" });

        if (dto.Ruc is not null) proveedor.Ruc = dto.Ruc;
        if (dto.RazonSocial is not null) proveedor.RazonSocial = dto.RazonSocial;
        if (dto.Direccion is not null) proveedor.Direccion = dto.Direccion;
        if (dto.Email is not null) proveedor.Email = dto.Email;
        if (dto.Telefono is not null) proveedor.Telefono = dto.Telefono;
        if (dto.DiasCredito.HasValue) proveedor.DiasCredito = dto.DiasCredito.Value;

        // Modificación a través del stored procedure sp_Proveedor_Modificar.
        var actualizado = await _proveedorRepository.ModificarAsync(proveedor);
        if (actualizado is null) return NotFound(new { message = "Proveedor no encontrado" });
        return Ok(actualizado);
    }

    /// <summary>Bloqueo temporal; impide registrar nuevas compras/órdenes de compra al proveedor.</summary>
    [HttpPut("{id:guid}/bloquear")]
    public async Task<IActionResult> Bloquear(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (proveedor is null) return NotFound(new { message = "Proveedor no encontrado" });
        proveedor.Bloqueado = true;
        proveedor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(proveedor);
    }

    [HttpPut("{id:guid}/desbloquear")]
    public async Task<IActionResult> Desbloquear(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (proveedor is null) return NotFound(new { message = "Proveedor no encontrado" });
        proveedor.Bloqueado = false;
        proveedor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(proveedor);
    }

    /// <summary>Inactivación (baja lógica) del proveedor.</summary>
    [HttpPut("{id:guid}/inactivar")]
    public async Task<IActionResult> Inactivar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        // Baja lógica a través de sp_Proveedor_EliminarLogico.
        var proveedor = await _proveedorRepository.EliminarLogicoAsync(id, empresaId);
        if (proveedor is null) return NotFound(new { message = "Proveedor no encontrado" });
        return Ok(proveedor);
    }

    [HttpPut("{id:guid}/activar")]
    public async Task<IActionResult> Activar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (proveedor is null) return NotFound(new { message = "Proveedor no encontrado" });
        proveedor.Activo = true;
        proveedor.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(proveedor);
    }
}
