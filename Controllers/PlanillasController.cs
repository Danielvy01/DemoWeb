using System.Text;
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

/// <summary>
/// Planillas mensuales: se generan a partir de los empleados activos, se
/// pueden ajustar (bonificaciones/descuentos), aprobarse y finalmente
/// sincronizarse para el pago automático (genera un archivo de pagos masivos
/// compatible con el módulo de pagos por lote de los bancos y queda a la
/// espera de la confirmación de abono).
/// </summary>
[Authorize]
[MenuPermiso("planillas")]
[ApiController]
[Route("planillas")]
public class PlanillasController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly ContabilidadService _contabilidadService;

    public PlanillasController(SiserpDbContext db, ContabilidadService contabilidadService)
    {
        _db = db;
        _contabilidadService = contabilidadService;
    }

    /// <summary>Genera la planilla del período con el salario base de todos los empleados activos.</summary>
    [HttpPost("generar")]
    public async Task<IActionResult> Generar(GenerarPlanillaDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var yaExiste = await _db.Planillas.AnyAsync(p => p.EmpresaId == empresaId && p.Periodo == dto.Periodo);
        if (yaExiste) return BadRequest(new { message = $"Ya existe una planilla para el período {dto.Periodo}" });

        var empleados = await _db.Empleados.Where(e => e.EmpresaId == empresaId && e.Activo).ToListAsync();
        if (empleados.Count == 0) return BadRequest(new { message = "No hay empleados activos para generar la planilla" });

        var detalles = empleados.Select(e => new PlanillaDetalle
        {
            EmpleadoId = e.Id,
            SueldoBase = e.SalarioBase,
            Bonificaciones = 0,
            Descuentos = 0,
            Neto = e.SalarioBase,
        }).ToList();

        var planilla = new Planilla
        {
            EmpresaId = empresaId,
            Periodo = dto.Periodo,
            TotalBruto = detalles.Sum(d => d.SueldoBase + d.Bonificaciones),
            TotalDescuentos = detalles.Sum(d => d.Descuentos),
            TotalNeto = detalles.Sum(d => d.Neto),
            Estado = EstadoPlanilla.BORRADOR,
            Detalles = detalles,
        };
        _db.Planillas.Add(planilla);
        await _db.SaveChangesAsync();

        return Ok(await Obtener(planilla.Id, empresaId));
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        var planillas = await _db.Planillas
            .Where(p => p.EmpresaId == empresaId)
            .Include(p => p.Detalles)
            .OrderByDescending(p => p.Periodo)
            .ToListAsync();
        return Ok(planillas);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var planilla = await Obtener(id, empresaId);
        if (planilla is null) return NotFound(new { message = "Planilla no encontrada" });
        return Ok(planilla);
    }

    /// <summary>Ajusta bonificaciones/descuentos de empleados específicos antes de aprobar.</summary>
    [HttpPut("{id:guid}/ajustar")]
    public async Task<IActionResult> Ajustar(Guid id, AjustarPlanillaDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var planilla = await _db.Planillas.Include(p => p.Detalles).FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (planilla is null) return NotFound(new { message = "Planilla no encontrada" });
        if (planilla.Estado != EstadoPlanilla.BORRADOR) return BadRequest(new { message = "Solo se puede ajustar una planilla en BORRADOR" });

        foreach (var ajuste in dto.Ajustes)
        {
            var detalle = planilla.Detalles.FirstOrDefault(d => d.EmpleadoId == ajuste.EmpleadoId);
            if (detalle is null) continue;
            detalle.Bonificaciones = ajuste.Bonificaciones;
            detalle.Descuentos = ajuste.Descuentos;
            detalle.Neto = detalle.SueldoBase + detalle.Bonificaciones - detalle.Descuentos;
        }

        planilla.TotalBruto = planilla.Detalles.Sum(d => d.SueldoBase + d.Bonificaciones);
        planilla.TotalDescuentos = planilla.Detalles.Sum(d => d.Descuentos);
        planilla.TotalNeto = planilla.Detalles.Sum(d => d.Neto);
        planilla.Estado = EstadoPlanilla.CALCULADA;
        planilla.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await Obtener(planilla.Id, empresaId));
    }

    /// <summary>
    /// Aprueba la planilla y genera automáticamente su provisión contable (Debe: gasto de
    /// personal por centro de costo; Haber: remuneraciones por pagar + tributos por pagar),
    /// al estilo CONCAR. Si el plan de cuentas no tiene configuradas las cuentas 62/41, la
    /// planilla igual queda aprobada, sin generar el asiento.
    /// </summary>
    [HttpPost("{id:guid}/aprobar")]
    public async Task<IActionResult> Aprobar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var planilla = await _db.Planillas
            .Include(p => p.Detalles).ThenInclude(d => d.Empleado)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (planilla is null) return NotFound(new { message = "Planilla no encontrada" });
        if (planilla.Estado is not (EstadoPlanilla.BORRADOR or EstadoPlanilla.CALCULADA))
            return BadRequest(new { message = "La planilla debe estar en BORRADOR o CALCULADA para aprobarse" });
        planilla.Estado = EstadoPlanilla.APROBADA;
        planilla.UpdatedAt = DateTime.UtcNow;
        await _contabilidadService.RegistrarAsientoPlanillaAsync(planilla);
        await _db.SaveChangesAsync();
        return Ok(await Obtener(planilla.Id, empresaId));
    }

    /// <summary>
    /// Sincroniza la planilla aprobada generando el lote de pago (CCI, monto)
    /// para el pago automático vía el módulo de pagos masivos del banco.
    /// </summary>
    [HttpPost("{id:guid}/sincronizar")]
    public async Task<IActionResult> Sincronizar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var planilla = await _db.Planillas
            .Include(p => p.Detalles).ThenInclude(d => d.Empleado)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (planilla is null) return NotFound(new { message = "Planilla no encontrada" });
        if (planilla.Estado != EstadoPlanilla.APROBADA) return BadRequest(new { message = "Solo se puede sincronizar una planilla APROBADA" });

        var sinCuenta = planilla.Detalles.Where(d => string.IsNullOrWhiteSpace(d.Empleado.CCI) && string.IsNullOrWhiteSpace(d.Empleado.CuentaBancaria)).ToList();
        if (sinCuenta.Count > 0)
            return BadRequest(new { message = "Hay empleados sin cuenta bancaria/CCI registrada", empleados = sinCuenta.Select(d => d.Empleado.Id) });

        var referencia = $"LOTE-{planilla.Periodo}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        foreach (var detalle in planilla.Detalles)
        {
            detalle.EstadoPago = EstadoPagoPlanilla.SINCRONIZADO;
            detalle.ReferenciaPago = referencia;
        }
        planilla.Estado = EstadoPlanilla.SINCRONIZADA;
        planilla.FechaSincronizacion = DateTime.UtcNow;
        planilla.ReferenciaSincronizacion = referencia;
        planilla.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await Obtener(planilla.Id, empresaId));
    }

    /// <summary>Descarga el archivo CSV del lote de pagos para cargarlo al portal de pagos masivos del banco.</summary>
    [HttpGet("{id:guid}/archivo-pago")]
    public async Task<IActionResult> ArchivoPago(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var planilla = await _db.Planillas
            .Include(p => p.Detalles).ThenInclude(d => d.Empleado)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (planilla is null) return NotFound(new { message = "Planilla no encontrada" });

        var sb = new StringBuilder();
        sb.AppendLine("DocumentoEmpleado,Nombres,Apellidos,Banco,CuentaBancaria,CCI,Monto,Referencia");
        foreach (var d in planilla.Detalles)
        {
            sb.AppendLine($"{d.Empleado.NumeroDocumento},{d.Empleado.Nombres},{d.Empleado.Apellidos},{d.Empleado.BancoNombre},{d.Empleado.CuentaBancaria},{d.Empleado.CCI},{d.Neto:F2},{d.ReferenciaPago ?? planilla.ReferenciaSincronizacion}");
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"planilla-{planilla.Periodo}.csv");
    }

    /// <summary>
    /// Confirma que el banco realizó el abono (llamar desde el proceso/webhook de conciliación
    /// bancaria) y genera el asiento del pago efectivo (Debe: remuneraciones por pagar; Haber:
    /// Caja y bancos), cancelando la provisión generada al aprobar la planilla.
    /// </summary>
    [HttpPost("{id:guid}/confirmar-pago")]
    public async Task<IActionResult> ConfirmarPago(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var planilla = await _db.Planillas.Include(p => p.Detalles).FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (planilla is null) return NotFound(new { message = "Planilla no encontrada" });
        if (planilla.Estado != EstadoPlanilla.SINCRONIZADA) return BadRequest(new { message = "La planilla debe estar SINCRONIZADA" });

        foreach (var detalle in planilla.Detalles)
        {
            detalle.EstadoPago = EstadoPagoPlanilla.PAGADO;
            detalle.PagadoAt = DateTime.UtcNow;
        }
        planilla.Estado = EstadoPlanilla.PAGADA;
        planilla.UpdatedAt = DateTime.UtcNow;
        await _contabilidadService.RegistrarAsientoPagoPlanillaAsync(planilla);
        await _db.SaveChangesAsync();

        return Ok(await Obtener(planilla.Id, empresaId));
    }

    private async Task<Planilla?> Obtener(Guid id, Guid empresaId) =>
        await _db.Planillas
            .Where(p => p.Id == id && p.EmpresaId == empresaId)
            .Include(p => p.Detalles).ThenInclude(d => d.Empleado)
            .FirstOrDefaultAsync();
}
