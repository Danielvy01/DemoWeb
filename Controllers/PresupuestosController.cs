using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Dtos;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;

namespace Siserp.Api.Controllers;

/// <summary>
/// Presupuestos y Control (módulo estándar de CONCAR): planificación de montos por cuenta
/// contable y centro de costo, mes a mes dentro de un año fiscal, con un reporte comparativo de
/// ejecución Real (desde los asientos CONFIRMADOs) vs. Presupuestado para medir desviaciones.
/// </summary>
[Authorize]
[MenuPermiso("presupuestos")]
[ApiController]
[Route("presupuestos")]
public class PresupuestosController : ControllerBase
{
    private readonly SiserpDbContext _db;

    public PresupuestosController(SiserpDbContext db)
    {
        _db = db;
    }

    // --- Cabecera del presupuesto ---

    [HttpPost]
    public async Task<IActionResult> Create(CreatePresupuestoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        if (dto.Anio < 2000 || dto.Anio > 2100) return BadRequest(new { message = "Año de presupuesto inválido" });
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return BadRequest(new { message = "El nombre del presupuesto es obligatorio" });

        var presupuesto = new PresupuestoAnual
        {
            EmpresaId = empresaId,
            Nombre = dto.Nombre,
            Anio = dto.Anio,
            Estado = EstadoPresupuesto.BORRADOR,
        };
        _db.PresupuestosAnuales.Add(presupuesto);
        await _db.SaveChangesAsync();
        return Ok(presupuesto);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll([FromQuery] int? anio)
    {
        var empresaId = User.GetEmpresaId();
        var query = _db.PresupuestosAnuales.Where(p => p.EmpresaId == empresaId);
        if (anio.HasValue) query = query.Where(p => p.Anio == anio.Value);
        var presupuestos = await query
            .OrderByDescending(p => p.Anio).ThenBy(p => p.Nombre)
            .Select(p => new
            {
                p.Id,
                p.Nombre,
                p.Anio,
                p.Estado,
                p.FechaAprobacion,
                totalPresupuestado = p.Detalles.Sum(d => d.Monto),
                cantidadLineas = p.Detalles.Count,
            })
            .ToListAsync();
        return Ok(presupuestos);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var presupuesto = await _db.PresupuestosAnuales
            .Include(p => p.Detalles).ThenInclude(d => d.Cuenta)
            .Include(p => p.Detalles).ThenInclude(d => d.CentroCosto)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (presupuesto is null) return NotFound(new { message = "Presupuesto no encontrado" });
        return Ok(presupuesto);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePresupuestoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var presupuesto = await _db.PresupuestosAnuales.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (presupuesto is null) return NotFound(new { message = "Presupuesto no encontrado" });
        if (presupuesto.Estado == EstadoPresupuesto.APROBADO)
            return BadRequest(new { message = "No se puede modificar un presupuesto APROBADO" });
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return BadRequest(new { message = "El nombre del presupuesto es obligatorio" });

        presupuesto.Nombre = dto.Nombre;
        presupuesto.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(presupuesto);
    }

    /// <summary>Elimina un presupuesto en BORRADOR (incluyendo todas sus líneas); un presupuesto APROBADO no se puede eliminar.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var presupuesto = await _db.PresupuestosAnuales.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (presupuesto is null) return NotFound(new { message = "Presupuesto no encontrado" });
        if (presupuesto.Estado == EstadoPresupuesto.APROBADO)
            return BadRequest(new { message = "No se puede eliminar un presupuesto APROBADO" });

        _db.PresupuestosAnuales.Remove(presupuesto);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Presupuesto eliminado" });
    }

    /// <summary>Aprueba el presupuesto: queda de solo lectura y sirve como línea base estable para el comparativo Real vs. Presupuestado.</summary>
    [HttpPost("{id:guid}/aprobar")]
    public async Task<IActionResult> Aprobar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var presupuesto = await _db.PresupuestosAnuales
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (presupuesto is null) return NotFound(new { message = "Presupuesto no encontrado" });
        if (presupuesto.Estado == EstadoPresupuesto.APROBADO)
            return BadRequest(new { message = "El presupuesto ya está aprobado" });
        if (presupuesto.Detalles.Count == 0)
            return BadRequest(new { message = "El presupuesto no tiene líneas registradas" });

        presupuesto.Estado = EstadoPresupuesto.APROBADO;
        presupuesto.FechaAprobacion = DateTime.UtcNow;
        presupuesto.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(presupuesto);
    }

    /// <summary>Reabre un presupuesto APROBADO a BORRADOR para poder corregir sus líneas.</summary>
    [HttpPost("{id:guid}/reabrir")]
    public async Task<IActionResult> Reabrir(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var presupuesto = await _db.PresupuestosAnuales.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (presupuesto is null) return NotFound(new { message = "Presupuesto no encontrado" });
        if (presupuesto.Estado != EstadoPresupuesto.APROBADO)
            return BadRequest(new { message = "El presupuesto no está aprobado" });

        presupuesto.Estado = EstadoPresupuesto.BORRADOR;
        presupuesto.FechaAprobacion = null;
        presupuesto.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(presupuesto);
    }

    // --- Líneas del presupuesto (grilla cuenta x mes) ---

    /// <summary>
    /// Alta/edición masiva de líneas: por cada combinación de cuenta + centro de costo + mes crea
    /// la línea si no existe, o actualiza su monto si ya existía (upsert), evitando que el
    /// frontend tenga que orquestar altas y modificaciones por separado al guardar la grilla.
    /// </summary>
    [HttpPost("{id:guid}/detalles")]
    public async Task<IActionResult> GuardarDetalles(Guid id, GuardarDetallesPresupuestoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var presupuesto = await _db.PresupuestosAnuales
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (presupuesto is null) return NotFound(new { message = "Presupuesto no encontrado" });
        if (presupuesto.Estado == EstadoPresupuesto.APROBADO)
            return BadRequest(new { message = "No se puede modificar un presupuesto APROBADO; reábralo primero" });
        if (dto.Detalles.Count == 0) return BadRequest(new { message = "Debe indicar al menos una línea" });
        if (dto.Detalles.Any(d => d.Mes < 1 || d.Mes > 12))
            return BadRequest(new { message = "El mes debe estar entre 1 y 12" });

        var cuentaIds = dto.Detalles.Select(d => d.CuentaId).Distinct().ToList();
        var cuentasValidas = await _db.CuentasContables.CountAsync(c => c.EmpresaId == empresaId && cuentaIds.Contains(c.Id));
        if (cuentasValidas != cuentaIds.Count) return BadRequest(new { message = "Alguna cuenta contable no existe" });

        var centroCostoIds = dto.Detalles.Where(d => d.CentroCostoId.HasValue).Select(d => d.CentroCostoId!.Value).Distinct().ToList();
        if (centroCostoIds.Count > 0)
        {
            var centrosValidos = await _db.CentrosCosto.CountAsync(c => c.EmpresaId == empresaId && centroCostoIds.Contains(c.Id));
            if (centrosValidos != centroCostoIds.Count) return BadRequest(new { message = "Algún centro de costo no existe" });
        }

        foreach (var item in dto.Detalles)
        {
            var existente = presupuesto.Detalles.FirstOrDefault(d =>
                d.CuentaId == item.CuentaId && d.CentroCostoId == item.CentroCostoId && d.Mes == item.Mes);
            if (existente is not null)
            {
                existente.Monto = item.Monto;
                existente.Glosa = item.Glosa;
                existente.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                presupuesto.Detalles.Add(new PresupuestoDetalle
                {
                    PresupuestoId = presupuesto.Id,
                    CuentaId = item.CuentaId,
                    CentroCostoId = item.CentroCostoId,
                    Mes = item.Mes,
                    Monto = item.Monto,
                    Glosa = item.Glosa,
                });
            }
        }
        presupuesto.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await _db.PresupuestosAnuales
            .Include(p => p.Detalles).ThenInclude(d => d.Cuenta)
            .Include(p => p.Detalles).ThenInclude(d => d.CentroCosto)
            .FirstAsync(p => p.Id == id));
    }

    /// <summary>Elimina una línea puntual del presupuesto (solo mientras está en BORRADOR).</summary>
    [HttpDelete("{id:guid}/detalles/{detalleId:guid}")]
    public async Task<IActionResult> RemoveDetalle(Guid id, Guid detalleId)
    {
        var empresaId = User.GetEmpresaId();
        var presupuesto = await _db.PresupuestosAnuales.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (presupuesto is null) return NotFound(new { message = "Presupuesto no encontrado" });
        if (presupuesto.Estado == EstadoPresupuesto.APROBADO)
            return BadRequest(new { message = "No se puede modificar un presupuesto APROBADO; reábralo primero" });

        var detalle = await _db.PresupuestoDetalles.FirstOrDefaultAsync(d => d.Id == detalleId && d.PresupuestoId == id);
        if (detalle is null) return NotFound(new { message = "Línea de presupuesto no encontrada" });

        _db.PresupuestoDetalles.Remove(detalle);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Línea eliminada" });
    }

    // --- Reporte: Real vs. Presupuestado ---

    /// <summary>
    /// Comparativo de ejecución Real vs. Presupuestado por cuenta (y centro de costo), al estilo
    /// del reporte homónimo de CONCAR: suma lo presupuestado (<see cref="PresupuestoDetalle"/>)
    /// en el rango de meses indicado contra lo realmente ejecutado (asientos CONFIRMADOs de ese
    /// mismo rango de fechas), mostrando la desviación en monto y en porcentaje de ejecución.
    /// </summary>
    [HttpGet("comparativo")]
    public async Task<IActionResult> Comparativo(
        [FromQuery] Guid? presupuestoId, [FromQuery] int? anio,
        [FromQuery] Guid? cuentaId, [FromQuery] Guid? centroCostoId,
        [FromQuery] int mesDesde = 1, [FromQuery] int mesHasta = 12)
    {
        var empresaId = User.GetEmpresaId();
        if (mesDesde < 1 || mesDesde > 12 || mesHasta < 1 || mesHasta > 12 || mesDesde > mesHasta)
            return BadRequest(new { message = "Rango de meses inválido (1-12)" });

        PresupuestoAnual? presupuesto;
        if (presupuestoId.HasValue)
        {
            presupuesto = await _db.PresupuestosAnuales
                .Include(p => p.Detalles).ThenInclude(d => d.Cuenta)
                .Include(p => p.Detalles).ThenInclude(d => d.CentroCosto)
                .FirstOrDefaultAsync(p => p.Id == presupuestoId.Value && p.EmpresaId == empresaId);
            if (presupuesto is null) return NotFound(new { message = "Presupuesto no encontrado" });
        }
        else
        {
            if (!anio.HasValue) return BadRequest(new { message = "Debe indicar presupuestoId o anio" });
            // Si hay varios presupuestos para el año (p. ej. "Inicial" y "Reformulado"), se prioriza
            // el único APROBADO; si ninguno está aprobado, se usa el más reciente en BORRADOR.
            presupuesto = await _db.PresupuestosAnuales
                .Include(p => p.Detalles).ThenInclude(d => d.Cuenta)
                .Include(p => p.Detalles).ThenInclude(d => d.CentroCosto)
                .Where(p => p.EmpresaId == empresaId && p.Anio == anio.Value)
                .OrderByDescending(p => p.Estado == EstadoPresupuesto.APROBADO)
                .ThenByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync();
            if (presupuesto is null) return Ok(new { presupuesto = (object?)null, lineas = Array.Empty<object>(), resumen = new { } });
        }

        var detalles = presupuesto.Detalles.Where(d => d.Mes >= mesDesde && d.Mes <= mesHasta).AsEnumerable();
        if (cuentaId.HasValue) detalles = detalles.Where(d => d.CuentaId == cuentaId.Value);
        if (centroCostoId.HasValue) detalles = detalles.Where(d => d.CentroCostoId == centroCostoId.Value);
        var detallesLista = detalles.ToList();

        var fechaDesde = new DateTime(presupuesto.Anio, mesDesde, 1);
        var fechaHasta = new DateTime(presupuesto.Anio, mesHasta, 1).AddMonths(1).AddDays(-1);

        var movimientosQuery = _db.AsientoDetalles
            .Include(m => m.Cuenta)
            .Include(m => m.Asiento)
            .Include(m => m.CentroCosto)
            .Where(m => m.Asiento.EmpresaId == empresaId && m.Asiento.Estado == EstadoAsiento.CONFIRMADO
                        && m.Asiento.Fecha >= fechaDesde && m.Asiento.Fecha <= fechaHasta);
        if (cuentaId.HasValue) movimientosQuery = movimientosQuery.Where(m => m.CuentaId == cuentaId.Value);
        if (centroCostoId.HasValue) movimientosQuery = movimientosQuery.Where(m => m.CentroCostoId == centroCostoId.Value);
        var movimientos = await movimientosQuery.ToListAsync();

        // Monto "ejecutado" con el mismo signo que el Estado de Resultados: para Ingresos
        // (Haber - Debe) y para Costos/Gastos/Activo/Pasivo/Patrimonio (Debe - Haber), de modo
        // que un presupuesto de gasto y su ejecución real se comparen en el mismo sentido.
        static decimal MontoEjecutado(IEnumerable<AsientoDetalle> movs, TipoCuentaContable tipo) =>
            tipo == TipoCuentaContable.INGRESO ? movs.Sum(m => m.Haber - m.Debe) : movs.Sum(m => m.Debe - m.Haber);

        var clavesPresupuesto = detallesLista
            .Select(d => (d.CuentaId, d.CentroCostoId))
            .Distinct();
        var clavesReal = movimientos
            .Select(m => (CuentaId: m.CuentaId, CentroCostoId: m.CentroCostoId))
            .Distinct();
        var claves = clavesPresupuesto.Union(clavesReal).ToList();

        var cuentasPorId = detallesLista.Select(d => d.Cuenta)
            .Concat(movimientos.Select(m => m.Cuenta))
            .GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());
        var centrosPorId = detallesLista.Where(d => d.CentroCosto != null).Select(d => d.CentroCosto!)
            .Concat(movimientos.Where(m => m.CentroCosto != null).Select(m => m.CentroCosto!))
            .GroupBy(c => c.Id).ToDictionary(g => g.Key, g => g.First());

        var lineas = claves.Select(k =>
        {
            var cuenta = cuentasPorId[k.CuentaId];
            var presupuestado = detallesLista.Where(d => d.CuentaId == k.CuentaId && d.CentroCostoId == k.CentroCostoId).Sum(d => d.Monto);
            var movsCuenta = movimientos.Where(m => m.CuentaId == k.CuentaId && m.CentroCostoId == k.CentroCostoId);
            var ejecutado = MontoEjecutado(movsCuenta, cuenta.Tipo);
            var variacion = Math.Round(ejecutado - presupuestado, 2);
            var porcentajeEjecucion = presupuestado != 0 ? Math.Round(ejecutado / presupuestado * 100, 2) : (decimal?)null;
            return new
            {
                cuentaId = k.CuentaId,
                codigo = cuenta.Codigo,
                nombre = cuenta.Nombre,
                tipo = cuenta.Tipo,
                centroCostoId = k.CentroCostoId,
                centroCosto = k.CentroCostoId.HasValue ? centrosPorId[k.CentroCostoId.Value].Nombre : null,
                presupuestado = Math.Round(presupuestado, 2),
                ejecutado = Math.Round(ejecutado, 2),
                variacion,
                porcentajeEjecucion,
            };
        })
        .OrderBy(l => l.codigo).ThenBy(l => l.centroCosto)
        .ToList();

        var resumen = new
        {
            totalPresupuestado = Math.Round(lineas.Sum(l => l.presupuestado), 2),
            totalEjecutado = Math.Round(lineas.Sum(l => l.ejecutado), 2),
            totalVariacion = Math.Round(lineas.Sum(l => l.variacion), 2),
            porTipo = lineas.GroupBy(l => l.tipo).Select(g => new
            {
                tipo = g.Key,
                presupuestado = Math.Round(g.Sum(l => l.presupuestado), 2),
                ejecutado = Math.Round(g.Sum(l => l.ejecutado), 2),
                variacion = Math.Round(g.Sum(l => l.variacion), 2),
            }),
        };

        return Ok(new
        {
            presupuesto = new { presupuesto.Id, presupuesto.Nombre, presupuesto.Anio, presupuesto.Estado },
            desde = fechaDesde,
            hasta = fechaHasta,
            lineas,
            resumen,
        });
    }
}
