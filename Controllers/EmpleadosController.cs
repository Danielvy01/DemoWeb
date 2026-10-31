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

/// <summary>Registro del personal por área, cargo y salario para la gestión de planillas.</summary>
[Authorize]
[MenuPermiso("empleados")]
[ApiController]
[Route("empleados")]
public class EmpleadosController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly IEmpleadoRepository _empleadoRepository;

    public EmpleadosController(SiserpDbContext db, IEmpleadoRepository empleadoRepository)
    {
        _db = db;
        _empleadoRepository = empleadoRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEmpleadoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var centroCostoValido = await _db.CentrosCosto.AnyAsync(c => c.Id == dto.CentroCostoId && c.EmpresaId == empresaId && c.Activo);
        if (!centroCostoValido) return BadRequest(new { message = "El centro de costo indicado no existe o está inactivo" });

        var empleado = new Empleado
        {
            EmpresaId = empresaId,
            NumeroDocumento = dto.NumeroDocumento,
            Nombres = dto.Nombres,
            Apellidos = dto.Apellidos,
            CentroCostoId = dto.CentroCostoId,
            Cargo = dto.Cargo,
            EsComprador = dto.EsComprador,
            SalarioBase = dto.SalarioBase,
            Email = dto.Email,
            Telefono = dto.Telefono,
            CuentaBancaria = dto.CuentaBancaria,
            BancoNombre = dto.BancoNombre,
            CCI = dto.CCI,
            FechaIngreso = dto.FechaIngreso ?? DateTime.UtcNow,
            TipoContrato = dto.TipoContrato,
            RegimenPensionario = dto.RegimenPensionario,
        };
        // Alta a través del stored procedure sp_Empleado_Registrar.
        var creado = await _empleadoRepository.RegistrarAsync(empleado);
        if (creado is not null)
        {
            // Registra el cargo informado como el cargo principal del trabajador.
            _db.EmpleadoCargos.Add(new EmpleadoCargo { EmpleadoId = creado.Id, Cargo = creado.Cargo, EsPrincipal = true });
            await _db.SaveChangesAsync();
        }
        return Ok(creado);
    }

    /// <summary>Lista los cargos (principal y adicionales) vigentes de un trabajador.</summary>
    [HttpGet("{id:guid}/cargos")]
    public async Task<IActionResult> ListarCargos(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var existe = await _db.Empleados.AnyAsync(e => e.Id == id && e.EmpresaId == empresaId);
        if (!existe) return NotFound(new { message = "Empleado no encontrado" });
        var cargos = await _db.EmpleadoCargos.Where(c => c.EmpleadoId == id && c.Activo)
            .OrderByDescending(c => c.EsPrincipal).ThenBy(c => c.Cargo).ToListAsync();
        return Ok(cargos);
    }

    /// <summary>Asigna un cargo adicional a un trabajador. Si se marca como principal, reemplaza al cargo principal vigente.</summary>
    [HttpPost("{id:guid}/cargos")]
    public async Task<IActionResult> AgregarCargo(Guid id, AddCargoEmpleadoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId);
        if (empleado is null) return NotFound(new { message = "Empleado no encontrado" });
        if (string.IsNullOrWhiteSpace(dto.Cargo)) return BadRequest(new { message = "El cargo es obligatorio" });

        var yaAsignado = await _db.EmpleadoCargos.AnyAsync(c => c.EmpleadoId == id && c.Activo && c.Cargo == dto.Cargo);
        if (yaAsignado) return BadRequest(new { message = "El trabajador ya tiene asignado ese cargo" });

        if (dto.EsPrincipal)
        {
            var principalesVigentes = await _db.EmpleadoCargos.Where(c => c.EmpleadoId == id && c.Activo && c.EsPrincipal).ToListAsync();
            foreach (var p in principalesVigentes) p.EsPrincipal = false;
            empleado.Cargo = dto.Cargo;
            empleado.UpdatedAt = DateTime.UtcNow;
        }

        var cargo = new EmpleadoCargo { EmpleadoId = id, Cargo = dto.Cargo, EsPrincipal = dto.EsPrincipal };
        _db.EmpleadoCargos.Add(cargo);
        await _db.SaveChangesAsync();
        return Ok(cargo);
    }

    /// <summary>Da de baja lógica a un cargo del trabajador (no se permite eliminar el único cargo vigente).</summary>
    [HttpDelete("{id:guid}/cargos/{cargoId:guid}")]
    public async Task<IActionResult> QuitarCargo(Guid id, Guid cargoId)
    {
        var empresaId = User.GetEmpresaId();
        var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId);
        if (empleado is null) return NotFound(new { message = "Empleado no encontrado" });
        var cargo = await _db.EmpleadoCargos.FirstOrDefaultAsync(c => c.Id == cargoId && c.EmpleadoId == id && c.Activo);
        if (cargo is null) return NotFound(new { message = "Cargo no encontrado" });

        var cantidadVigentes = await _db.EmpleadoCargos.CountAsync(c => c.EmpleadoId == id && c.Activo);
        if (cantidadVigentes <= 1) return BadRequest(new { message = "El trabajador debe mantener al menos un cargo" });
        if (cargo.EsPrincipal) return BadRequest(new { message = "No se puede quitar el cargo principal; asigne otro cargo como principal primero" });

        cargo.Activo = false;
        cargo.FechaFin = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(cargo);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll([FromQuery] Guid? centroCostoId, [FromQuery] bool? esComprador)
    {
        var empresaId = User.GetEmpresaId();
        var query = _db.Empleados.Include(e => e.CentroCosto).Include(e => e.Cargos.Where(c => c.Activo)).Where(e => e.EmpresaId == empresaId);
        if (centroCostoId.HasValue) query = query.Where(e => e.CentroCostoId == centroCostoId);
        if (esComprador.HasValue) query = query.Where(e => e.EsComprador == esComprador);
        var empleados = await query.OrderBy(e => e.CentroCosto.Nombre).ThenBy(e => e.Apellidos).ToListAsync();
        return Ok(empleados);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var empleado = await _db.Empleados.Include(e => e.CentroCosto).Include(e => e.Cargos.Where(c => c.Activo)).FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId);
        if (empleado is null) return NotFound(new { message = "Empleado no encontrado" });
        return Ok(empleado);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateEmpleadoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var empleado = await _db.Empleados.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId);
        if (empleado is null) return NotFound(new { message = "Empleado no encontrado" });

        if (dto.CentroCostoId.HasValue)
        {
            var centroCostoValido = await _db.CentrosCosto.AnyAsync(c => c.Id == dto.CentroCostoId && c.EmpresaId == empresaId && c.Activo);
            if (!centroCostoValido) return BadRequest(new { message = "El centro de costo indicado no existe o está inactivo" });
            empleado.CentroCostoId = dto.CentroCostoId.Value;
        }
        if (dto.Cargo is not null) empleado.Cargo = dto.Cargo;
        if (dto.EsComprador.HasValue) empleado.EsComprador = dto.EsComprador.Value;
        if (dto.SalarioBase.HasValue) empleado.SalarioBase = dto.SalarioBase.Value;
        if (dto.Email is not null) empleado.Email = dto.Email;
        if (dto.Telefono is not null) empleado.Telefono = dto.Telefono;
        if (dto.CuentaBancaria is not null) empleado.CuentaBancaria = dto.CuentaBancaria;
        if (dto.BancoNombre is not null) empleado.BancoNombre = dto.BancoNombre;
        if (dto.CCI is not null) empleado.CCI = dto.CCI;
        if (dto.TipoContrato is not null) empleado.TipoContrato = dto.TipoContrato;
        if (dto.RegimenPensionario is not null) empleado.RegimenPensionario = dto.RegimenPensionario;
        if (dto.Activo.HasValue) empleado.Activo = dto.Activo.Value;

        // Modificación a través del stored procedure sp_Empleado_Modificar.
        var actualizado = await _empleadoRepository.ModificarAsync(empleado);
        if (actualizado is not null && dto.Cargo is not null)
        {
            // Mantiene sincronizado el cargo principal en la tabla de cargos múltiples.
            var cargosVigentes = await _db.EmpleadoCargos.Where(c => c.EmpleadoId == id && c.Activo).ToListAsync();
            var principal = cargosVigentes.FirstOrDefault(c => c.EsPrincipal);
            if (principal is null)
            {
                _db.EmpleadoCargos.Add(new EmpleadoCargo { EmpleadoId = id, Cargo = dto.Cargo, EsPrincipal = true });
            }
            else if (principal.Cargo != dto.Cargo)
            {
                var yaExiste = cargosVigentes.Any(c => c.Id != principal.Id && c.Cargo == dto.Cargo);
                if (!yaExiste) principal.Cargo = dto.Cargo;
            }
            await _db.SaveChangesAsync();
        }
        if (actualizado is null) return NotFound(new { message = "Empleado no encontrado" });
        return Ok(actualizado);
    }

    /// <summary>Da de baja al trabajador: lo marca inactivo y registra fecha/motivo de cese.</summary>
    [HttpPut("{id:guid}/baja")]
    public async Task<IActionResult> DarBaja(Guid id, [FromBody] DarBajaEmpleadoDto? dto)
    {
        var empresaId = User.GetEmpresaId();
        // Baja lógica a través del stored procedure sp_Empleado_EliminarLogico.
        var empleado = await _empleadoRepository.EliminarLogicoAsync(id, empresaId, dto?.Motivo, dto?.Fecha);
        if (empleado is null) return NotFound(new { message = "Empleado no encontrado" });
        return Ok(empleado);
    }

    /// <summary>Reincorpora a un trabajador dado de baja.</summary>
    [HttpPut("{id:guid}/activar")]
    public async Task<IActionResult> Activar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var empleado = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == id && e.EmpresaId == empresaId);
        if (empleado is null) return NotFound(new { message = "Empleado no encontrado" });
        empleado.Activo = true;
        empleado.FechaCese = null;
        empleado.MotivoCese = null;
        empleado.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(empleado);
    }
}
