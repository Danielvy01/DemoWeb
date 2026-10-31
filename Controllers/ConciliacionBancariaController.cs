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
/// Conciliación bancaria (al estilo CONCAR): importación del extracto bancario (CSV) de una
/// cuenta de Caja y Bancos para un periodo, calce automático/manual contra el Libro Caja y
/// Bancos, y reporte del cuadre entre el saldo según banco y el saldo según libros.
/// </summary>
[Authorize]
[MenuPermiso("conciliacion-bancaria")]
[ApiController]
[Route("conciliacion-bancaria")]
public class ConciliacionBancariaController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly ConciliacionBancariaService _service;

    public ConciliacionBancariaController(SiserpDbContext db, ConciliacionBancariaService service)
    {
        _db = db;
        _service = service;
    }

    /// <summary>Cuentas de Caja y Bancos (PCGE clase 10) elegibles para conciliar.</summary>
    [HttpGet("cuentas")]
    public async Task<IActionResult> Cuentas()
    {
        var empresaId = User.GetEmpresaId();
        var cuentas = await _db.CuentasContables
            .Where(c => c.EmpresaId == empresaId && c.Activa && c.Codigo.StartsWith("10"))
            .OrderBy(c => c.Codigo)
            .Select(c => new { c.Id, c.Codigo, c.Nombre })
            .ToListAsync();
        return Ok(cuentas);
    }

    /// <summary>
    /// Importa el extracto bancario (CSV: Fecha, Descripción, N° Operación, Cargo, Abono[, Saldo])
    /// para la cuenta y periodo indicados, reemplazando cualquier extracto previamente importado
    /// para ese mismo periodo, y dispara el calce automático contra el Libro Caja y Bancos.
    /// </summary>
    [HttpPost("importar")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Importar(
        [FromForm] IFormFile archivo, [FromForm] Guid cuentaId, [FromForm] int anio, [FromForm] int mes,
        [FromForm] decimal? saldoInicialBanco, [FromForm] decimal? saldoFinalBanco)
    {
        var empresaId = User.GetEmpresaId();
        if (archivo is null || archivo.Length == 0) return BadRequest(new { message = "Debe adjuntar el archivo del extracto bancario (CSV)" });
        if (mes < 1 || mes > 12) return BadRequest(new { message = "Mes inválido" });

        string contenido;
        using (var lector = new StreamReader(archivo.OpenReadStream(), System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            contenido = await lector.ReadToEndAsync();

        var (conciliacion, error) = await _service.ImportarAsync(empresaId, cuentaId, anio, mes, archivo.FileName, contenido, saldoInicialBanco, saldoFinalBanco);
        if (error != null) return BadRequest(new { message = error });

        return Ok(await ConstruirReporteAsync(empresaId, conciliacion!.Id));
    }

    /// <summary>Obtiene la conciliación (extracto + Libro Caja y Bancos + resumen de cuadre) de una cuenta/periodo, si ya fue importada.</summary>
    [HttpGet]
    public async Task<IActionResult> Obtener([FromQuery] Guid cuentaId, [FromQuery] int anio, [FromQuery] int mes)
    {
        var empresaId = User.GetEmpresaId();
        var conciliacion = await _db.ConciliacionesBancarias
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.CuentaId == cuentaId && c.Anio == anio && c.Mes == mes);
        if (conciliacion is null) return Ok(null);
        return Ok(await ConstruirReporteAsync(empresaId, conciliacion.Id));
    }

    /// <summary>Vuelve a correr el calce automático sobre los movimientos aún pendientes (p. ej. tras confirmar nuevos asientos).</summary>
    [HttpPost("{id:guid}/conciliar-automatico")]
    public async Task<IActionResult> ConciliarAutomatico(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var existe = await _db.ConciliacionesBancarias.AnyAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (!existe) return NotFound(new { message = "Conciliación no encontrada" });
        await _service.ConciliarAutomaticoAsync(empresaId, id);
        return Ok(await ConstruirReporteAsync(empresaId, id));
    }

    /// <summary>Calza manualmente un movimiento del extracto contra una línea del Libro Caja y Bancos elegida por el usuario.</summary>
    [HttpPost("movimientos/{movimientoId:guid}/conciliar")]
    public async Task<IActionResult> ConciliarManual(Guid movimientoId, ConciliarManualDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var movimiento = await _db.MovimientosExtractoBancario
            .Include(m => m.Conciliacion)
            .FirstOrDefaultAsync(m => m.Id == movimientoId && m.Conciliacion.EmpresaId == empresaId);
        if (movimiento is null) return NotFound(new { message = "Movimiento del extracto no encontrado" });
        if (movimiento.Conciliacion.Estado == EstadoConciliacionBancaria.CERRADA)
            return BadRequest(new { message = "La conciliación de este periodo ya está cerrada" });

        var detalle = await _db.AsientoDetalles
            .Include(d => d.Asiento)
            .FirstOrDefaultAsync(d => d.Id == dto.AsientoDetalleId && d.Asiento.EmpresaId == empresaId);
        if (detalle is null) return NotFound(new { message = "Línea de asiento no encontrada" });
        if (detalle.Asiento.Estado != EstadoAsiento.CONFIRMADO) return BadRequest(new { message = "La línea de asiento debe pertenecer a un asiento CONFIRMADO" });
        if (detalle.CuentaId != movimiento.Conciliacion.CuentaId) return BadRequest(new { message = "La línea de asiento debe pertenecer a la misma cuenta de Caja y Bancos que se está conciliando" });

        var yaUsado = await _db.MovimientosExtractoBancario.AnyAsync(m => m.AsientoDetalleId == dto.AsientoDetalleId && m.Id != movimientoId);
        if (yaUsado) return BadRequest(new { message = "Esa línea de asiento ya está calzada con otro movimiento del extracto" });

        movimiento.AsientoDetalleId = dto.AsientoDetalleId;
        movimiento.Estado = EstadoMovimientoExtracto.CONCILIADO_MANUAL;
        await _db.SaveChangesAsync();
        return Ok(await ConstruirReporteAsync(empresaId, movimiento.ConciliacionId));
    }

    /// <summary>Deshace el calce (automático o manual) de un movimiento del extracto, dejándolo PENDIENTE nuevamente.</summary>
    [HttpPost("movimientos/{movimientoId:guid}/desconciliar")]
    public async Task<IActionResult> Desconciliar(Guid movimientoId)
    {
        var empresaId = User.GetEmpresaId();
        var movimiento = await _db.MovimientosExtractoBancario
            .Include(m => m.Conciliacion)
            .FirstOrDefaultAsync(m => m.Id == movimientoId && m.Conciliacion.EmpresaId == empresaId);
        if (movimiento is null) return NotFound(new { message = "Movimiento del extracto no encontrado" });
        if (movimiento.Conciliacion.Estado == EstadoConciliacionBancaria.CERRADA)
            return BadRequest(new { message = "La conciliación de este periodo ya está cerrada" });

        movimiento.AsientoDetalleId = null;
        movimiento.Estado = EstadoMovimientoExtracto.PENDIENTE;
        await _db.SaveChangesAsync();
        return Ok(await ConstruirReporteAsync(empresaId, movimiento.ConciliacionId));
    }

    /// <summary>Cierra la conciliación del periodo (recomendado solo cuando la diferencia es 0); queda de solo lectura.</summary>
    [HttpPost("{id:guid}/cerrar")]
    public async Task<IActionResult> Cerrar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var conciliacion = await _db.ConciliacionesBancarias.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (conciliacion is null) return NotFound(new { message = "Conciliación no encontrada" });
        conciliacion.Estado = EstadoConciliacionBancaria.CERRADA;
        conciliacion.FechaCierre = DateTime.UtcNow;
        conciliacion.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(await ConstruirReporteAsync(empresaId, id));
    }

    /// <summary>Reabre una conciliación cerrada, p. ej. para reimportar el extracto o corregir un calce.</summary>
    [HttpPost("{id:guid}/reabrir")]
    public async Task<IActionResult> Reabrir(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var conciliacion = await _db.ConciliacionesBancarias.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (conciliacion is null) return NotFound(new { message = "Conciliación no encontrada" });
        conciliacion.Estado = EstadoConciliacionBancaria.ABIERTA;
        conciliacion.FechaCierre = null;
        conciliacion.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(await ConstruirReporteAsync(empresaId, id));
    }

    /// <summary>
    /// Construye el reporte completo de la conciliación: movimientos del extracto (banco), líneas
    /// del Libro Caja y Bancos del periodo (libros) con su estado de calce, y el resumen de cuadre
    /// (saldo según banco + depósitos en tránsito - cheques en tránsito = saldo según libros +
    /// abonos no registrados - cargos no registrados).
    /// </summary>
    private async Task<object> ConstruirReporteAsync(Guid empresaId, Guid conciliacionId)
    {
        var conciliacion = await _db.ConciliacionesBancarias
            .Include(c => c.Cuenta)
            .Include(c => c.Movimientos).ThenInclude(m => m.AsientoDetalle).ThenInclude(d => d!.Asiento)
            .FirstAsync(c => c.Id == conciliacionId && c.EmpresaId == empresaId);

        var desde = new DateTime(conciliacion.Anio, conciliacion.Mes, 1);
        var hasta = desde.AddMonths(1).AddTicks(-1);

        var anteriores = await _db.AsientoDetalles
            .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Estado == EstadoAsiento.CONFIRMADO
                        && d.CuentaId == conciliacion.CuentaId && d.Asiento.Fecha < desde)
            .SumAsync(d => (decimal?)(d.Debe - d.Haber)) ?? 0m;

        var movimientosLibro = await _db.AsientoDetalles
            .Include(d => d.Asiento)
            .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Estado == EstadoAsiento.CONFIRMADO
                        && d.CuentaId == conciliacion.CuentaId && d.Asiento.Fecha >= desde && d.Asiento.Fecha <= hasta)
            .OrderBy(d => d.Asiento.Fecha).ThenBy(d => d.Asiento.Numero)
            .ToListAsync();

        var idsCalzados = conciliacion.Movimientos.Where(m => m.AsientoDetalleId.HasValue).Select(m => m.AsientoDetalleId!.Value).ToHashSet();
        var saldoLibro = anteriores;
        var libro = movimientosLibro.Select(d =>
        {
            saldoLibro += d.Debe - d.Haber;
            return new
            {
                asientoDetalleId = d.Id,
                fecha = d.Asiento.Fecha,
                numero = d.Asiento.Numero,
                glosa = d.Glosa ?? d.Asiento.Glosa,
                ingreso = d.Debe,
                egreso = d.Haber,
                saldo = saldoLibro,
                conciliado = idsCalzados.Contains(d.Id),
            };
        }).ToList();

        var banco = conciliacion.Movimientos.OrderBy(m => m.Fecha).Select(m => new
        {
            id = m.Id,
            fecha = m.Fecha,
            descripcion = m.Descripcion,
            numeroOperacion = m.NumeroOperacion,
            cargo = m.Cargo,
            abono = m.Abono,
            saldoExtracto = m.SaldoExtracto,
            estado = m.Estado.ToString(),
            asientoDetalleId = m.AsientoDetalleId,
            asientoNumero = m.AsientoDetalle?.Asiento.Numero,
            asientoGlosa = m.AsientoDetalle?.Glosa ?? m.AsientoDetalle?.Asiento.Glosa,
        }).ToList();

        var depositosEnTransito = libro.Where(l => !l.conciliado && l.ingreso > 0).Sum(l => l.ingreso);
        var chequesEnTransito = libro.Where(l => !l.conciliado && l.egreso > 0).Sum(l => l.egreso);
        var cargosNoRegistrados = conciliacion.Movimientos.Where(m => m.Estado == EstadoMovimientoExtracto.PENDIENTE && m.Cargo > 0).Sum(m => m.Cargo);
        var abonosNoRegistrados = conciliacion.Movimientos.Where(m => m.Estado == EstadoMovimientoExtracto.PENDIENTE && m.Abono > 0).Sum(m => m.Abono);

        var saldoConciliadoBanco = conciliacion.SaldoFinalBanco + depositosEnTransito - chequesEnTransito;
        var saldoConciliadoLibros = saldoLibro + abonosNoRegistrados - cargosNoRegistrados;

        return new
        {
            id = conciliacion.Id,
            cuentaId = conciliacion.CuentaId,
            cuenta = new { conciliacion.Cuenta.Codigo, conciliacion.Cuenta.Nombre },
            conciliacion.Anio,
            conciliacion.Mes,
            conciliacion.SaldoInicialBanco,
            conciliacion.SaldoFinalBanco,
            conciliacion.NombreArchivoImportado,
            conciliacion.FechaImportacion,
            estado = conciliacion.Estado.ToString(),
            banco,
            libro,
            resumen = new
            {
                saldoBanco = conciliacion.SaldoFinalBanco,
                saldoLibros = saldoLibro,
                depositosEnTransito,
                chequesEnTransito,
                cargosNoRegistrados,
                abonosNoRegistrados,
                saldoConciliadoBanco,
                saldoConciliadoLibros,
                diferencia = Math.Round(saldoConciliadoBanco - saldoConciliadoLibros, 2),
                cuadra = Math.Round(saldoConciliadoBanco - saldoConciliadoLibros, 2) == 0,
            },
        };
    }
}
