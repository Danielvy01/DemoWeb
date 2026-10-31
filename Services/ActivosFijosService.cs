using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

/// <summary>
/// Libro de Activos Fijos: calcula y registra la depreciación mensual de los activos
/// (método línea recta) y genera el asiento contable correspondiente, al estilo del
/// módulo de activos fijos de CONCAR (Debe: gasto por depreciación / Haber: depreciación
/// acumulada). Agrupa en un mismo asiento todos los activos del período que comparten las
/// mismas cuentas y centro de costo, para no generar un asiento por cada activo.
/// </summary>
public class ActivosFijosService
{
    private readonly SiserpDbContext _db;

    public ActivosFijosService(SiserpDbContext db)
    {
        _db = db;
    }

    /// <summary>Depreciación mensual en línea recta de un activo: (ValorAdquisición - ValorResidual) / VidaÚtilMeses.</summary>
    public static decimal CalcularDepreciacionMensual(ActivoFijo activo) =>
        activo.VidaUtilMeses <= 0 ? 0 : Math.Round(activo.BaseDepreciable / activo.VidaUtilMeses, 2);

    /// <summary>
    /// Genera la depreciación del período indicado ("yyyy-MM") para todos los activos ACTIVO de
    /// la empresa cuya fecha de inicio de depreciación ya se cumplió y que aún no se depreciaron
    /// en ese período (idempotente: si ya existe el registro del período para un activo, se omite).
    /// Devuelve el detalle generado por activo y los asientos contables creados.
    /// </summary>
    public async Task<List<DepreciacionActivoFijo>> GenerarDepreciacionPeriodoAsync(Guid empresaId, string periodo)
    {
        if (!DateTime.TryParseExact(periodo + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var fechaPeriodo))
            throw new ArgumentException("Periodo inválido; formato esperado yyyy-MM");
        var finPeriodo = fechaPeriodo.AddMonths(1).AddDays(-1);

        var activos = await _db.ActivosFijos
            .Where(a => a.EmpresaId == empresaId && a.Estado == EstadoActivoFijo.ACTIVO && a.FechaInicioDepreciacion <= finPeriodo)
            .Include(a => a.Depreciaciones)
            .ToListAsync();

        var generadas = new List<DepreciacionActivoFijo>();
        // Agrupa por combinación de cuentas + centro de costo, para consolidar un asiento por grupo.
        var grupos = new Dictionary<(Guid CuentaGasto, Guid CuentaDepAcum, Guid? CentroCosto), List<AsientoDetalle>>();
        var glosa = $"Depreciación de activos fijos - período {periodo}";

        foreach (var activo in activos)
        {
            if (activo.Depreciaciones.Any(d => d.Periodo == periodo)) continue; // ya procesado, idempotente

            var depreciacionAcumuladaPrevia = activo.Depreciaciones.Sum(d => d.MontoDepreciacion);
            var saldoPorDepreciar = activo.BaseDepreciable - depreciacionAcumuladaPrevia;
            if (saldoPorDepreciar <= 0) continue;

            var monto = Math.Min(CalcularDepreciacionMensual(activo), saldoPorDepreciar);
            if (monto <= 0) continue;

            var depreciacionAcumulada = depreciacionAcumuladaPrevia + monto;
            var detalle = new DepreciacionActivoFijo
            {
                ActivoFijoId = activo.Id,
                Periodo = periodo,
                MontoDepreciacion = monto,
                DepreciacionAcumulada = depreciacionAcumulada,
            };
            _db.DepreciacionesActivoFijo.Add(detalle);
            generadas.Add(detalle);

            if (depreciacionAcumulada >= activo.BaseDepreciable)
            {
                activo.Estado = EstadoActivoFijo.TOTALMENTE_DEPRECIADO;
                activo.UpdatedAt = DateTime.UtcNow;
            }

            var key = (activo.CuentaGastoDepreciacionId, activo.CuentaDepreciacionAcumuladaId, activo.CentroCostoId);
            if (!grupos.TryGetValue(key, out var lineas))
            {
                lineas = new List<AsientoDetalle>();
                grupos[key] = lineas;
            }
            lineas.Add(new AsientoDetalle { CuentaId = activo.CuentaGastoDepreciacionId, Debe = monto, Haber = 0, Glosa = $"{activo.Codigo} - {activo.Nombre}", CentroCostoId = activo.CentroCostoId });
            lineas.Add(new AsientoDetalle { CuentaId = activo.CuentaDepreciacionAcumuladaId, Debe = 0, Haber = monto, Glosa = $"{activo.Codigo} - {activo.Nombre}", CentroCostoId = activo.CentroCostoId });

            // Se enlaza el asiento una vez creado (ver abajo); por ahora queda pendiente.
        }

        await _db.SaveChangesAsync();

        // Crea un asiento contable por cada grupo de cuentas y lo enlaza a las depreciaciones de ese grupo.
        foreach (var ((cuentaGasto, cuentaDepAcum, centroCosto), lineas) in grupos)
        {
            var numero = await _db.AsientosContables.CountAsync(a => a.EmpresaId == empresaId) + 1;
            var asiento = new AsientoContable
            {
                EmpresaId = empresaId,
                Numero = numero,
                Fecha = finPeriodo,
                Glosa = glosa,
                Referencia = periodo,
                Estado = EstadoAsiento.CONFIRMADO,
                Subdiario = TipoSubdiario.ACTIVOS_FIJOS,
                Detalles = lineas,
            };
            _db.AsientosContables.Add(asiento);
            await _db.SaveChangesAsync();

            foreach (var detalle in generadas.Where(d =>
                d.AsientoContableId is null &&
                activos.First(a => a.Id == d.ActivoFijoId) is var activo &&
                activo.CuentaGastoDepreciacionId == cuentaGasto &&
                activo.CuentaDepreciacionAcumuladaId == cuentaDepAcum &&
                activo.CentroCostoId == centroCosto))
            {
                detalle.AsientoContableId = asiento.Id;
            }
        }
        await _db.SaveChangesAsync();

        return generadas;
    }

    /// <summary>Libro de Activos Fijos: detalle de cada activo con su depreciación acumulada y valor neto en libros a la fecha de corte.</summary>
    public async Task<object> ObtenerLibroAsync(Guid empresaId, DateTime fechaCorte)
    {
        var activos = await _db.ActivosFijos
            .Where(a => a.EmpresaId == empresaId && a.FechaAdquisicion <= fechaCorte)
            .Include(a => a.CentroCosto)
            .Include(a => a.CuentaActivo)
            .Include(a => a.Depreciaciones)
            .OrderBy(a => a.Codigo)
            .ToListAsync();

        return activos.Select(a =>
        {
            var depreciacionAcumulada = a.Depreciaciones
                .Where(d => string.Compare(d.Periodo, fechaCorte.ToString("yyyy-MM"), StringComparison.Ordinal) <= 0)
                .Sum(d => d.MontoDepreciacion);
            return new
            {
                a.Id,
                a.Codigo,
                a.Nombre,
                a.Categoria,
                centroCosto = a.CentroCosto?.Nombre,
                cuentaActivo = a.CuentaActivo.Codigo + " - " + a.CuentaActivo.Nombre,
                a.FechaAdquisicion,
                a.ValorAdquisicion,
                a.ValorResidual,
                a.VidaUtilMeses,
                a.Estado,
                depreciacionAcumulada,
                valorNetoLibros = a.ValorAdquisicion - depreciacionAcumulada,
            };
        }).ToList();
    }
}
