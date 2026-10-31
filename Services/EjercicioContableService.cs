using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

/// <summary>
/// Cierre y apertura de ejercicio contable (al estilo CONCAR):
/// - Al <see cref="AbrirEjercicioAsync"/> se crean los 12 <see cref="PeriodoContable"/> (meses) del
///   año y, si existe un ejercicio anterior ya CERRADO, se genera automáticamente el Asiento de
///   Apertura que arrastra los saldos de Activo/Pasivo/Patrimonio de ese ejercicio como saldos
///   iniciales del nuevo año (fecha 1 de enero).
/// - Al <see cref="CerrarEjercicioAsync"/> se cierran los periodos que sigan abiertos y se genera
///   el Asiento de Cierre, que cancela las cuentas de Ingresos/Costos/Gastos contra la cuenta de
///   Resultados del Ejercicio (Patrimonio, prefijo "59"), dejando el ejercicio listo para que su
///   resultado se arrastre al abrir el siguiente.
/// - <see cref="EstaPeriodoAbiertoAsync"/> es el control de bloqueo de periodos cerrados que deben
///   invocar todos los puntos de entrada que crean/confirman/anulan asientos.
/// </summary>
public class EjercicioContableService
{
    private readonly SiserpDbContext _db;

    public EjercicioContableService(SiserpDbContext db)
    {
        _db = db;
    }

    private async Task<CuentaContable?> BuscarCuentaAsync(Guid empresaId, string prefijoCodigo) =>
        await _db.CuentasContables
            .Where(c => c.EmpresaId == empresaId && c.Activa && c.Codigo.StartsWith(prefijoCodigo))
            .OrderBy(c => c.Codigo)
            .FirstOrDefaultAsync();

    /// <summary>
    /// Indica si la fecha dada cae dentro de un periodo contable ABIERTO. Si la empresa aún no
    /// tiene periodos configurados (no ha usado el módulo de ejercicios), se considera abierto
    /// para no romper el comportamiento previo sin control de periodos.
    /// </summary>
    public async Task<bool> EstaPeriodoAbiertoAsync(Guid empresaId, DateTime fecha)
    {
        var periodo = await _db.PeriodosContables
            .FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.Anio == fecha.Year && p.Mes == fecha.Month);
        return periodo is null || periodo.Estado == EstadoPeriodoContable.ABIERTO;
    }

    /// <summary>Mensaje de error estándar cuando una operación se bloquea por periodo cerrado.</summary>
    public static string MensajePeriodoCerrado(DateTime fecha) =>
        $"El periodo contable {fecha:MM/yyyy} está cerrado; no se pueden registrar ni modificar asientos con esa fecha.";

    /// <summary>
    /// Abre un nuevo ejercicio contable: crea el ejercicio y sus 12 periodos mensuales (ABIERTO).
    /// Si existe el ejercicio del año anterior y está CERRADO, genera el Asiento de Apertura que
    /// arrastra los saldos de Activo/Pasivo/Patrimonio (incluyendo el resultado acumulado del
    /// cierre anterior) como saldos iniciales al 1 de enero del nuevo año.
    /// </summary>
    public async Task<(EjercicioContable? Ejercicio, string? Error)> AbrirEjercicioAsync(Guid empresaId, int anio)
    {
        var yaExiste = await _db.EjerciciosContables.AnyAsync(e => e.EmpresaId == empresaId && e.Anio == anio);
        if (yaExiste) return (null, $"Ya existe un ejercicio contable {anio} para esta empresa");

        var ejercicioAnterior = await _db.EjerciciosContables
            .FirstOrDefaultAsync(e => e.EmpresaId == empresaId && e.Anio == anio - 1);
        if (ejercicioAnterior != null && ejercicioAnterior.Estado != EstadoEjercicioContable.CERRADO)
            return (null, $"El ejercicio {anio - 1} debe cerrarse antes de abrir el ejercicio {anio}");

        var fechaInicio = new DateTime(anio, 1, 1);
        var fechaFin = new DateTime(anio, 12, 31);

        var ejercicio = new EjercicioContable
        {
            EmpresaId = empresaId,
            Anio = anio,
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Estado = EstadoEjercicioContable.ABIERTO,
            Periodos = Enumerable.Range(1, 12).Select(mes => new PeriodoContable
            {
                EmpresaId = empresaId,
                Anio = anio,
                Mes = mes,
                FechaInicio = new DateTime(anio, mes, 1),
                FechaFin = new DateTime(anio, mes, DateTime.DaysInMonth(anio, mes)),
                Estado = EstadoPeriodoContable.ABIERTO,
            }).ToList(),
        };
        _db.EjerciciosContables.Add(ejercicio);

        if (ejercicioAnterior != null)
        {
            var asientoApertura = await ConstruirAsientoAperturaAsync(empresaId, ejercicioAnterior, fechaInicio);
            if (asientoApertura != null)
            {
                _db.AsientosContables.Add(asientoApertura);
                ejercicio.AsientoApertura = asientoApertura;
            }
        }

        await _db.SaveChangesAsync();
        return (ejercicio, null);
    }

    /// <summary>
    /// Construye (sin guardar) el asiento de apertura del nuevo ejercicio a partir de los saldos
    /// de las cuentas de Balance (Activo/Pasivo/Patrimonio) del ejercicio anterior, calculados con
    /// los asientos CONFIRMADOS hasta su <see cref="EjercicioContable.FechaFin"/>. Si no hay saldos
    /// (empresa sin movimientos), no genera asiento.
    /// </summary>
    private async Task<AsientoContable?> ConstruirAsientoAperturaAsync(Guid empresaId, EjercicioContable ejercicioAnterior, DateTime fechaApertura)
    {
        var movimientos = await _db.AsientoDetalles
            .Include(d => d.Cuenta)
            .Where(d => d.Asiento.EmpresaId == empresaId
                && d.Asiento.Estado == EstadoAsiento.CONFIRMADO
                && d.Asiento.Fecha <= ejercicioAnterior.FechaFin
                && (d.Cuenta.Tipo == TipoCuentaContable.ACTIVO || d.Cuenta.Tipo == TipoCuentaContable.PASIVO || d.Cuenta.Tipo == TipoCuentaContable.PATRIMONIO))
            .ToListAsync();

        static decimal Saldo(IEnumerable<AsientoDetalle> movs, NaturalezaCuenta naturaleza)
        {
            var debe = movs.Sum(d => d.Debe);
            var haber = movs.Sum(d => d.Haber);
            return naturaleza == NaturalezaCuenta.DEUDORA ? debe - haber : haber - debe;
        }

        var saldosPorCuenta = movimientos
            .GroupBy(d => new { d.CuentaId, d.Cuenta.Naturaleza })
            .Select(g => (CuentaId: g.Key.CuentaId, Naturaleza: g.Key.Naturaleza, Saldo: Saldo(g, g.Key.Naturaleza)))
            .Where(s => s.Saldo != 0)
            .ToList();

        // El resultado del ejercicio anterior (utilidad/pérdida) ya quedó trasladado a la cuenta
        // de Resultados Acumulados (prefijo "59") por el Asiento de Cierre, por lo que está
        // incluido en los saldos de Patrimonio anteriores: no se añade por separado.
        if (saldosPorCuenta.Count == 0) return null;

        var detalles = saldosPorCuenta.Select(s => new AsientoDetalle
        {
            CuentaId = s.CuentaId,
            Debe = s.Naturaleza == NaturalezaCuenta.DEUDORA && s.Saldo > 0 ? s.Saldo : (s.Naturaleza == NaturalezaCuenta.ACREEDORA && s.Saldo < 0 ? -s.Saldo : 0),
            Haber = s.Naturaleza == NaturalezaCuenta.ACREEDORA && s.Saldo > 0 ? s.Saldo : (s.Naturaleza == NaturalezaCuenta.DEUDORA && s.Saldo < 0 ? -s.Saldo : 0),
            DebeMe = 0,
            HaberMe = 0,
            Moneda = "PEN",
            TipoCambio = 1m,
            Glosa = $"Saldo inicial arrastrado del ejercicio {ejercicioAnterior.Anio}",
        }).ToList();
        foreach (var d in detalles) { d.DebeMe = d.Debe; d.HaberMe = d.Haber; }

        var numero = await _db.AsientosContables.CountAsync(a => a.EmpresaId == empresaId) + 1;
        return new AsientoContable
        {
            EmpresaId = empresaId,
            Numero = numero,
            Fecha = fechaApertura,
            Glosa = $"Asiento de apertura del ejercicio {fechaApertura.Year}",
            Referencia = "APERTURA",
            Estado = EstadoAsiento.CONFIRMADO,
            Subdiario = TipoSubdiario.APERTURA_CIERRE,
            Detalles = detalles,
        };
    }

    /// <summary>
    /// Cierra el ejercicio: cierra los periodos que sigan abiertos, calcula el resultado
    /// (Ingresos - Costos - Gastos) y genera el Asiento de Cierre que cancela esas cuentas contra
    /// la cuenta de Resultados del Ejercicio (prefijo "59"; si no existe, "58" o "56" como respaldo).
    /// </summary>
    public async Task<(EjercicioContable? Ejercicio, string? Error)> CerrarEjercicioAsync(Guid empresaId, Guid ejercicioId)
    {
        var ejercicio = await _db.EjerciciosContables
            .Include(e => e.Periodos)
            .FirstOrDefaultAsync(e => e.Id == ejercicioId && e.EmpresaId == empresaId);
        if (ejercicio is null) return (null, "Ejercicio no encontrado");
        if (ejercicio.Estado == EstadoEjercicioContable.CERRADO) return (null, "El ejercicio ya está cerrado");

        var cuentaResultados = await BuscarCuentaAsync(empresaId, "59")
            ?? await BuscarCuentaAsync(empresaId, "58")
            ?? await BuscarCuentaAsync(empresaId, "56");
        if (cuentaResultados is null)
            return (null, "No se encontró una cuenta de Resultados Acumulados (59) en el plan de cuentas para registrar el cierre");

        var movimientos = await _db.AsientoDetalles
            .Include(d => d.Cuenta)
            .Where(d => d.Asiento.EmpresaId == empresaId
                && d.Asiento.Estado == EstadoAsiento.CONFIRMADO
                && d.Asiento.Fecha >= ejercicio.FechaInicio && d.Asiento.Fecha <= ejercicio.FechaFin
                && (d.Cuenta.Tipo == TipoCuentaContable.INGRESO || d.Cuenta.Tipo == TipoCuentaContable.COSTO || d.Cuenta.Tipo == TipoCuentaContable.GASTO))
            .ToListAsync();

        var detalles = new List<AsientoDetalle>();
        foreach (var grupo in movimientos.GroupBy(d => new { d.CuentaId, d.Cuenta.Tipo }))
        {
            var debe = grupo.Sum(d => d.Debe);
            var haber = grupo.Sum(d => d.Haber);
            if (grupo.Key.Tipo == TipoCuentaContable.INGRESO)
            {
                var saldo = haber - debe; // saldo acreedor normal de un ingreso
                if (saldo == 0) continue;
                detalles.Add(new AsientoDetalle { CuentaId = grupo.Key.CuentaId, Debe = saldo > 0 ? saldo : 0, Haber = saldo < 0 ? -saldo : 0, Glosa = "Cancelación de cuenta de ingreso por cierre de ejercicio" });
            }
            else
            {
                var saldo = debe - haber; // saldo deudor normal de costo/gasto
                if (saldo == 0) continue;
                detalles.Add(new AsientoDetalle { CuentaId = grupo.Key.CuentaId, Haber = saldo > 0 ? saldo : 0, Debe = saldo < 0 ? -saldo : 0, Glosa = "Cancelación de cuenta de costo/gasto por cierre de ejercicio" });
            }
        }

        var ingresos = movimientos.Where(d => d.Cuenta.Tipo == TipoCuentaContable.INGRESO).Sum(d => d.Haber - d.Debe);
        var costos = movimientos.Where(d => d.Cuenta.Tipo == TipoCuentaContable.COSTO).Sum(d => d.Debe - d.Haber);
        var gastos = movimientos.Where(d => d.Cuenta.Tipo == TipoCuentaContable.GASTO).Sum(d => d.Debe - d.Haber);
        var resultado = Math.Round(ingresos - costos - gastos, 2);

        if (resultado != 0)
        {
            // Utilidad (resultado > 0): se abona (Haber) a Resultados del Ejercicio (Patrimonio).
            // Pérdida (resultado < 0): se carga (Debe) a Resultados del Ejercicio.
            detalles.Add(new AsientoDetalle
            {
                CuentaId = cuentaResultados.Id,
                Debe = resultado < 0 ? -resultado : 0,
                Haber = resultado > 0 ? resultado : 0,
                Glosa = resultado >= 0 ? "Utilidad del ejercicio" : "Pérdida del ejercicio",
            });
        }

        AsientoContable? asientoCierre = null;
        if (detalles.Count > 0)
        {
            foreach (var d in detalles) { d.Moneda = "PEN"; d.TipoCambio = 1m; d.DebeMe = d.Debe; d.HaberMe = d.Haber; }
            var numero = await _db.AsientosContables.CountAsync(a => a.EmpresaId == empresaId) + 1;
            asientoCierre = new AsientoContable
            {
                EmpresaId = empresaId,
                Numero = numero,
                Fecha = ejercicio.FechaFin,
                Glosa = $"Asiento de cierre del ejercicio {ejercicio.Anio}",
                Referencia = "CIERRE",
                Estado = EstadoAsiento.CONFIRMADO,
                Subdiario = TipoSubdiario.APERTURA_CIERRE,
                Detalles = detalles,
            };
            _db.AsientosContables.Add(asientoCierre);
        }

        foreach (var periodo in ejercicio.Periodos.Where(p => p.Estado == EstadoPeriodoContable.ABIERTO))
        {
            periodo.Estado = EstadoPeriodoContable.CERRADO;
            periodo.FechaCierre = DateTime.UtcNow;
            periodo.UpdatedAt = DateTime.UtcNow;
        }

        ejercicio.Estado = EstadoEjercicioContable.CERRADO;
        ejercicio.Resultado = resultado;
        ejercicio.FechaCierre = DateTime.UtcNow;
        ejercicio.UpdatedAt = DateTime.UtcNow;
        if (asientoCierre != null) ejercicio.AsientoCierre = asientoCierre;

        await _db.SaveChangesAsync();
        return (ejercicio, null);
    }

    /// <summary>Cierra manualmente un periodo (mes) puntual, sin necesidad de cerrar todo el ejercicio.</summary>
    public async Task<(PeriodoContable? Periodo, string? Error)> CerrarPeriodoAsync(Guid empresaId, Guid periodoId)
    {
        var periodo = await _db.PeriodosContables.FirstOrDefaultAsync(p => p.Id == periodoId && p.EmpresaId == empresaId);
        if (periodo is null) return (null, "Periodo no encontrado");
        if (periodo.Estado == EstadoPeriodoContable.CERRADO) return (null, "El periodo ya está cerrado");
        periodo.Estado = EstadoPeriodoContable.CERRADO;
        periodo.FechaCierre = DateTime.UtcNow;
        periodo.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (periodo, null);
    }

    /// <summary>Reabre un periodo previamente cerrado (p. ej. para corregir un asiento), siempre que su ejercicio siga ABIERTO.</summary>
    public async Task<(PeriodoContable? Periodo, string? Error)> AbrirPeriodoAsync(Guid empresaId, Guid periodoId)
    {
        var periodo = await _db.PeriodosContables
            .Include(p => p.Ejercicio)
            .FirstOrDefaultAsync(p => p.Id == periodoId && p.EmpresaId == empresaId);
        if (periodo is null) return (null, "Periodo no encontrado");
        if (periodo.Ejercicio.Estado == EstadoEjercicioContable.CERRADO)
            return (null, "No se puede reabrir un periodo de un ejercicio ya cerrado");
        periodo.Estado = EstadoPeriodoContable.ABIERTO;
        periodo.FechaCierre = null;
        periodo.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (periodo, null);
    }
}
