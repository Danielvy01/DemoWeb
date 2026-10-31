using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Dtos;
using Siserp.Api.Models;

namespace Siserp.Api.Controllers;

/// <summary>
/// Mantenimiento del tipo de cambio (USD/PEN) día a día, usado para expresar en soles
/// o en dólares el importe pendiente de pago de las compras registradas en moneda
/// distinta a la consultada (p. ej. reporte de cuentas por pagar / planilla de pagos).
/// </summary>
[Authorize]
[ApiController]
[Route("tipos-cambio")]
public class TiposCambioController : ControllerBase
{
    private readonly SiserpDbContext _db;

    public TiposCambioController(SiserpDbContext db)
    {
        _db = db;
    }

    /// <summary>Registra o actualiza el tipo de cambio de una fecha (una fila por día y empresa).</summary>
    [HttpPost]
    public async Task<IActionResult> Upsert(CreateTipoCambioDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var fecha = dto.Fecha.Date;
        var existente = await _db.TiposCambio.FirstOrDefaultAsync(t => t.EmpresaId == empresaId && t.Fecha == fecha);
        if (existente is null)
        {
            existente = new TipoCambio { EmpresaId = empresaId, Fecha = fecha };
            _db.TiposCambio.Add(existente);
        }
        existente.Compra = dto.Compra;
        existente.Venta = dto.Venta;
        existente.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(existente);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var query = _db.TiposCambio.Where(t => t.EmpresaId == empresaId);
        if (desde is not null) query = query.Where(t => t.Fecha >= desde.Value.Date);
        if (hasta is not null) query = query.Where(t => t.Fecha <= hasta.Value.Date);
        var lista = await query.OrderByDescending(t => t.Fecha).ToListAsync();
        return Ok(lista);
    }

    /// <summary>Tipo de cambio vigente a una fecha: el más reciente registrado hasta ese día (por defecto, hoy).</summary>
    [HttpGet("vigente")]
    public async Task<IActionResult> Vigente([FromQuery] DateTime? fecha)
    {
        var empresaId = User.GetEmpresaId();
        var fechaConsulta = (fecha ?? DateTime.UtcNow).Date;
        var tipoCambio = await _db.TiposCambio
            .Where(t => t.EmpresaId == empresaId && t.Fecha <= fechaConsulta)
            .OrderByDescending(t => t.Fecha)
            .FirstOrDefaultAsync();
        if (tipoCambio is null) return NotFound(new { message = "No hay tipo de cambio registrado hasta esa fecha" });
        return Ok(tipoCambio);
    }
}
