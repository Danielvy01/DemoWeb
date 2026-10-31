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

[Authorize]
[MenuPermiso("clientes")]
[ApiController]
[Route("clientes")]
public class ClientesController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly IClienteRepository _clienteRepository;

    public ClientesController(SiserpDbContext db, IClienteRepository clienteRepository)
    {
        _db = db;
        _clienteRepository = clienteRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateClienteDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var cliente = new Cliente
        {
            EmpresaId = empresaId,
            TipoDocumento = Enum.Parse<TipoDocumentoIdentidad>(dto.TipoDocumento),
            NumeroDocumento = dto.NumeroDocumento,
            NombreORazonSocial = dto.NombreORazonSocial,
            Direccion = dto.Direccion,
            Email = dto.Email,
            Telefono = dto.Telefono,
            DiasCredito = dto.DiasCredito ?? 0,
            Clasificacion = dto.Clasificacion is not null ? Enum.Parse<ClasificacionCliente>(dto.Clasificacion) : ClasificacionCliente.NORMAL,
        };
        // Alta a través del stored procedure sp_Cliente_Registrar (adaptador de infraestructura).
        var creado = await _clienteRepository.RegistrarAsync(cliente);
        return Ok(creado);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        var clientes = await _db.Clientes.Where(c => c.EmpresaId == empresaId)
            .OrderByDescending(c => c.CreatedAt).ToListAsync();
        return Ok(clientes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (cliente is null) return NotFound(new { message = "Cliente no encontrado" });
        return Ok(cliente);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateClienteDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (cliente is null) return NotFound(new { message = "Cliente no encontrado" });

        if (dto.TipoDocumento is not null) cliente.TipoDocumento = Enum.Parse<TipoDocumentoIdentidad>(dto.TipoDocumento);
        if (dto.NumeroDocumento is not null) cliente.NumeroDocumento = dto.NumeroDocumento;
        if (dto.NombreORazonSocial is not null) cliente.NombreORazonSocial = dto.NombreORazonSocial;
        if (dto.Direccion is not null) cliente.Direccion = dto.Direccion;
        if (dto.Email is not null) cliente.Email = dto.Email;
        if (dto.Telefono is not null) cliente.Telefono = dto.Telefono;
        if (dto.DiasCredito.HasValue) cliente.DiasCredito = dto.DiasCredito.Value;
        if (dto.Clasificacion is not null) cliente.Clasificacion = Enum.Parse<ClasificacionCliente>(dto.Clasificacion);

        // Modificación a través del stored procedure sp_Cliente_Modificar.
        var actualizado = await _clienteRepository.ModificarAsync(cliente);
        if (actualizado is null) return NotFound(new { message = "Cliente no encontrado" });
        return Ok(actualizado);
    }

    /// <summary>Bloqueo temporal (p. ej. por mora); no permite emitir nuevas ventas al cliente.</summary>
    [HttpPut("{id:guid}/bloquear")]
    public async Task<IActionResult> Bloquear(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (cliente is null) return NotFound(new { message = "Cliente no encontrado" });
        cliente.Bloqueado = true;
        cliente.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(cliente);
    }

    [HttpPut("{id:guid}/desbloquear")]
    public async Task<IActionResult> Desbloquear(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (cliente is null) return NotFound(new { message = "Cliente no encontrado" });
        cliente.Bloqueado = false;
        cliente.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(cliente);
    }

    /// <summary>Inactivación (baja lógica) del cliente; deja de aparecer como disponible para nuevas operaciones.</summary>
    [HttpPut("{id:guid}/inactivar")]
    public async Task<IActionResult> Inactivar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (cliente is null) return NotFound(new { message = "Cliente no encontrado" });
        cliente.Activo = false;
        cliente.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(cliente);
    }

    [HttpPut("{id:guid}/activar")]
    public async Task<IActionResult> Activar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (cliente is null) return NotFound(new { message = "Cliente no encontrado" });
        cliente.Activo = true;
        cliente.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(cliente);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        // Baja logica a través de sp_Cliente_EliminarLogico: se conserva el histórico de
        // cotizaciones/comprobantes asociado.
        var cliente = await _clienteRepository.EliminarLogicoAsync(id, empresaId);
        if (cliente is null) return NotFound(new { message = "Cliente no encontrado" });
        return Ok(cliente);
    }
}
