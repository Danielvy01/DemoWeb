using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

/// <summary>Una línea ya parseada del extracto bancario, antes de persistirse.</summary>
public record LineaExtractoParseada(DateTime Fecha, string? Descripcion, string? NumeroOperacion, decimal Cargo, decimal Abono, decimal? Saldo);

/// <summary>
/// Conciliación bancaria (al estilo CONCAR): importa el extracto bancario (CSV) de una cuenta de
/// Caja y Bancos para un periodo, y calcea automáticamente cada línea contra el Libro Caja y
/// Bancos (asientos CONFIRMADOs) por fecha (con tolerancia de días) e importe exacto, dejando en
/// evidencia las partidas conciliatorias (depósitos/cheques en tránsito, cargos/abonos bancarios
/// no registrados) y el cuadre entre el saldo según banco y el saldo según libros.
/// </summary>
public class ConciliacionBancariaService
{
    private readonly SiserpDbContext _db;

    /// <summary>Tolerancia, en días calendario, para calzar una línea del extracto contra una línea de asiento (los pagos suelen demorar en procesarse/compensarse).</summary>
    public const int ToleranciaDias = 4;

    public ConciliacionBancariaService(SiserpDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Parsea un extracto bancario en CSV (delimitado por "," o ";", con o sin fila de cabecera)
    /// con columnas Fecha, Descripción, N° Operación, Cargo, Abono[, Saldo]. Acepta fechas
    /// dd/MM/yyyy, yyyy-MM-dd o MM/dd/yyyy, y montos con "," o "." como separador decimal.
    /// </summary>
    public List<LineaExtractoParseada> ParsearCsv(string contenido)
    {
        var lineas = contenido
            .Split('\n')
            .Select(l => l.TrimEnd('\r').Trim())
            .Where(l => l.Length > 0)
            .ToList();
        if (lineas.Count == 0) return new List<LineaExtractoParseada>();

        var delimitador = lineas[0].Count(c => c == ';') >= lineas[0].Count(c => c == ',') ? ';' : ',';
        var resultado = new List<LineaExtractoParseada>();

        foreach (var linea in lineas)
        {
            var campos = linea.Split(delimitador).Select(c => c.Trim().Trim('"')).ToArray();
            if (campos.Length < 4) continue;
            if (!TryParseFecha(campos[0], out var fecha)) continue; // salta la fila de cabecera u otras filas no reconocidas

            var descripcion = campos.Length > 1 ? campos[1] : null;
            var numeroOperacion = campos.Length > 2 ? campos[2] : null;
            var cargo = campos.Length > 3 ? ParseMonto(campos[3]) : 0m;
            var abono = campos.Length > 4 ? ParseMonto(campos[4]) : 0m;
            decimal? saldo = campos.Length > 5 ? ParseMonto(campos[5]) : null;

            if (cargo == 0m && abono == 0m) continue;
            resultado.Add(new LineaExtractoParseada(fecha, descripcion, numeroOperacion, Math.Abs(cargo), Math.Abs(abono), saldo));
        }

        return resultado.OrderBy(l => l.Fecha).ToList();
    }

    private static bool TryParseFecha(string texto, out DateTime fecha)
    {
        string[] formatos = { "dd/MM/yyyy", "yyyy-MM-dd", "MM/dd/yyyy", "d/M/yyyy", "yyyy/MM/dd" };
        return DateTime.TryParseExact(texto, formatos, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha)
            || DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
    }

    private static decimal ParseMonto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return 0m;
        var limpio = texto.Replace(" ", "");
        // Si ambos separadores están presentes, el último es el decimal (p. ej. "1.234,56" o "1,234.56").
        if (limpio.Contains(',') && limpio.Contains('.'))
        {
            limpio = limpio.LastIndexOf(',') > limpio.LastIndexOf('.')
                ? limpio.Replace(".", "").Replace(",", ".")
                : limpio.Replace(",", "");
        }
        else if (limpio.Contains(','))
        {
            limpio = limpio.Replace(",", ".");
        }
        return decimal.TryParse(limpio, NumberStyles.Any, CultureInfo.InvariantCulture, out var valor) ? valor : 0m;
    }

    /// <summary>
    /// Importa (reemplazando el extracto anterior si existía) los movimientos bancarios del
    /// periodo indicado, crea la <see cref="ConciliacionBancaria"/> si no existe y dispara el
    /// calce automático.
    /// </summary>
    public async Task<(ConciliacionBancaria? Conciliacion, string? Error)> ImportarAsync(
        Guid empresaId, Guid cuentaId, int anio, int mes, string nombreArchivo, string contenidoCsv,
        decimal? saldoInicialBanco, decimal? saldoFinalBanco)
    {
        var cuenta = await _db.CuentasContables.FirstOrDefaultAsync(c => c.Id == cuentaId && c.EmpresaId == empresaId);
        if (cuenta is null) return (null, "Cuenta contable no encontrada");
        if (!cuenta.Codigo.StartsWith("10")) return (null, "La cuenta debe ser de Caja y Bancos (PCGE clase 10)");

        var lineas = ParsearCsv(contenidoCsv);
        if (lineas.Count == 0) return (null, "El archivo no contiene movimientos reconocibles (se esperan columnas Fecha, Descripción, N° Operación, Cargo, Abono)");

        var conciliacion = await _db.ConciliacionesBancarias
            .Include(c => c.Movimientos)
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.CuentaId == cuentaId && c.Anio == anio && c.Mes == mes);
        if (conciliacion is not null && conciliacion.Estado == EstadoConciliacionBancaria.CERRADA)
            return (null, "La conciliación de este periodo ya está cerrada; reábrala antes de reimportar el extracto");

        if (conciliacion is null)
        {
            conciliacion = new ConciliacionBancaria { EmpresaId = empresaId, CuentaId = cuentaId, Anio = anio, Mes = mes };
            _db.ConciliacionesBancarias.Add(conciliacion);
        }
        else
        {
            _db.MovimientosExtractoBancario.RemoveRange(conciliacion.Movimientos);
            conciliacion.Movimientos.Clear();
        }

        conciliacion.NombreArchivoImportado = nombreArchivo;
        conciliacion.FechaImportacion = DateTime.UtcNow;
        conciliacion.SaldoInicialBanco = saldoInicialBanco ?? conciliacion.SaldoInicialBanco;
        conciliacion.SaldoFinalBanco = saldoFinalBanco ?? lineas.Last().Saldo ?? conciliacion.SaldoFinalBanco;
        conciliacion.UpdatedAt = DateTime.UtcNow;

        foreach (var linea in lineas)
        {
            conciliacion.Movimientos.Add(new MovimientoExtractoBancario
            {
                ConciliacionId = conciliacion.Id,
                Fecha = linea.Fecha,
                Descripcion = linea.Descripcion,
                NumeroOperacion = linea.NumeroOperacion,
                Cargo = linea.Cargo,
                Abono = linea.Abono,
                SaldoExtracto = linea.Saldo,
            });
        }

        await _db.SaveChangesAsync();
        await ConciliarAutomaticoAsync(empresaId, conciliacion.Id);
        return (conciliacion, null);
    }

    /// <summary>
    /// Calza automáticamente cada movimiento PENDIENTE del extracto contra una línea del Libro
    /// Caja y Bancos (asiento CONFIRMADO) de la misma cuenta aún sin calzar, por importe exacto
    /// (Abono=Debe, Cargo=Haber) y fecha más cercana dentro de <see cref="ToleranciaDias"/> días.
    /// </summary>
    public async Task<int> ConciliarAutomaticoAsync(Guid empresaId, Guid conciliacionId)
    {
        var conciliacion = await _db.ConciliacionesBancarias
            .Include(c => c.Movimientos)
            .FirstOrDefaultAsync(c => c.Id == conciliacionId && c.EmpresaId == empresaId);
        if (conciliacion is null) return 0;

        var desde = new DateTime(conciliacion.Anio, conciliacion.Mes, 1).AddDays(-ToleranciaDias);
        var hasta = new DateTime(conciliacion.Anio, conciliacion.Mes, 1).AddMonths(1).AddDays(ToleranciaDias - 1);

        var yaCalzadosIds = await _db.MovimientosExtractoBancario
            .Where(m => m.AsientoDetalleId != null)
            .Select(m => m.AsientoDetalleId!.Value)
            .ToListAsync();

        var candidatosLibro = await _db.AsientoDetalles
            .Include(d => d.Asiento)
            .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Estado == EstadoAsiento.CONFIRMADO
                        && d.CuentaId == conciliacion.CuentaId
                        && d.Asiento.Fecha >= desde && d.Asiento.Fecha <= hasta
                        && !yaCalzadosIds.Contains(d.Id))
            .ToListAsync();

        var disponibles = candidatosLibro.ToList();
        var pendientes = conciliacion.Movimientos.Where(m => m.Estado == EstadoMovimientoExtracto.PENDIENTE).ToList();
        var calzados = 0;

        foreach (var mov in pendientes)
        {
            var monto = mov.Abono > 0 ? mov.Abono : mov.Cargo;
            var match = disponibles
                .Where(d => (mov.Abono > 0 ? d.Debe : d.Haber) == monto)
                .OrderBy(d => Math.Abs((d.Asiento.Fecha.Date - mov.Fecha.Date).Days))
                .FirstOrDefault();
            if (match is null) continue;

            mov.AsientoDetalleId = match.Id;
            mov.Estado = EstadoMovimientoExtracto.CONCILIADO_AUTOMATICO;
            disponibles.Remove(match);
            calzados++;
        }

        if (calzados > 0) await _db.SaveChangesAsync();
        return calzados;
    }
}
