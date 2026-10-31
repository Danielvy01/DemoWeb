using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Dtos;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

/// <summary>
/// Motor de reportes financieros NIIF / Dinámica de Cuentas: arma y calcula
/// <see cref="ReporteFinancieroPlantilla"/> (Balance General, Estado de Resultados o Notas a los
/// EEFF) a partir de la estructura de <see cref="ReporteFinancieroSeccion"/> y la "dinámica de
/// cuentas" (<see cref="ReporteFinancieroCuenta"/>) que el propio usuario configura, en vez de la
/// agrupación fija por <see cref="TipoCuentaContable"/> que usaban antes los reportes de
/// Contabilidad. También expone el CRUD de plantillas/secciones/cuentas y la siembra de
/// plantillas predeterminadas (equivalentes al PCGE) para que una empresa nueva tenga reportes
/// funcionando de inmediato y el usuario los personalice desde ahí.
/// </summary>
public class ReporteFinancieroService
{
    private readonly SiserpDbContext _db;

    public ReporteFinancieroService(SiserpDbContext db)
    {
        _db = db;
    }

    // --- Plantillas ---

    public async Task<List<ReporteFinancieroPlantilla>> ListarPlantillasAsync(Guid empresaId, TipoReporteFinanciero? tipo = null)
    {
        var query = _db.ReportesFinancierosPlantillas.Where(p => p.EmpresaId == empresaId);
        if (tipo.HasValue) query = query.Where(p => p.Tipo == tipo.Value);
        return await query.OrderBy(p => p.Tipo).ThenBy(p => p.Orden).ThenBy(p => p.Nombre).ToListAsync();
    }

    public async Task<ReporteFinancieroPlantilla?> ObtenerPlantillaAsync(Guid empresaId, Guid id) =>
        await _db.ReportesFinancierosPlantillas
            .Include(p => p.Secciones.OrderBy(s => s.Orden)).ThenInclude(s => s.Cuentas).ThenInclude(c => c.Cuenta)
            .FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);

    public async Task<(ReporteFinancieroPlantilla? Plantilla, string? Error)> CrearPlantillaAsync(Guid empresaId, CreatePlantillaReporteDto dto)
    {
        if (!Enum.TryParse<TipoReporteFinanciero>(dto.Tipo, true, out var tipo))
            return (null, "Tipo de reporte inválido");
        var yaExiste = await _db.ReportesFinancierosPlantillas.AnyAsync(p => p.EmpresaId == empresaId && p.Codigo == dto.Codigo);
        if (yaExiste) return (null, $"Ya existe una plantilla con el código {dto.Codigo}");

        if (dto.EsPredeterminada)
            await QuitarPredeterminadaAsync(empresaId, tipo);

        var plantilla = new ReporteFinancieroPlantilla
        {
            EmpresaId = empresaId,
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Tipo = tipo,
            Descripcion = dto.Descripcion,
            EsPredeterminada = dto.EsPredeterminada,
        };
        _db.ReportesFinancierosPlantillas.Add(plantilla);
        await _db.SaveChangesAsync();
        return (plantilla, null);
    }

    public async Task<(ReporteFinancieroPlantilla? Plantilla, string? Error)> ActualizarPlantillaAsync(Guid empresaId, Guid id, UpdatePlantillaReporteDto dto)
    {
        var plantilla = await _db.ReportesFinancierosPlantillas.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (plantilla is null) return (null, "Plantilla no encontrada");

        if (dto.Nombre is not null) plantilla.Nombre = dto.Nombre;
        if (dto.Descripcion is not null) plantilla.Descripcion = dto.Descripcion;
        if (dto.Activa.HasValue) plantilla.Activa = dto.Activa.Value;
        if (dto.EsPredeterminada.HasValue && dto.EsPredeterminada.Value && !plantilla.EsPredeterminada)
        {
            await QuitarPredeterminadaAsync(empresaId, plantilla.Tipo);
            plantilla.EsPredeterminada = true;
        }
        else if (dto.EsPredeterminada.HasValue && !dto.EsPredeterminada.Value)
        {
            plantilla.EsPredeterminada = false;
        }
        plantilla.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (plantilla, null);
    }

    public async Task<string?> EliminarPlantillaAsync(Guid empresaId, Guid id)
    {
        var plantilla = await _db.ReportesFinancierosPlantillas.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (plantilla is null) return "Plantilla no encontrada";
        _db.ReportesFinancierosPlantillas.Remove(plantilla); // cascada elimina secciones y cuentas mapeadas
        await _db.SaveChangesAsync();
        return null;
    }

    private async Task QuitarPredeterminadaAsync(Guid empresaId, TipoReporteFinanciero tipo)
    {
        var actuales = await _db.ReportesFinancierosPlantillas
            .Where(p => p.EmpresaId == empresaId && p.Tipo == tipo && p.EsPredeterminada)
            .ToListAsync();
        foreach (var p in actuales) p.EsPredeterminada = false;
    }

    // --- Secciones ---

    public async Task<(ReporteFinancieroSeccion? Seccion, string? Error)> CrearSeccionAsync(Guid empresaId, Guid plantillaId, CreateSeccionReporteDto dto)
    {
        var plantilla = await _db.ReportesFinancierosPlantillas.FirstOrDefaultAsync(p => p.Id == plantillaId && p.EmpresaId == empresaId);
        if (plantilla is null) return (null, "Plantilla no encontrada");
        if (dto.SeccionPadreId.HasValue)
        {
            var padreValido = await _db.ReportesFinancierosSecciones.AnyAsync(s => s.Id == dto.SeccionPadreId.Value && s.PlantillaId == plantillaId);
            if (!padreValido) return (null, "La sección padre no pertenece a esta plantilla");
        }

        var seccion = new ReporteFinancieroSeccion
        {
            PlantillaId = plantillaId,
            SeccionPadreId = dto.SeccionPadreId,
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Orden = dto.Orden,
            Signo = dto.Signo,
            EsTotalDestacado = dto.EsTotalDestacado,
            EsResultadoDelEjercicio = dto.EsResultadoDelEjercicio,
            ReferenciaNota = dto.ReferenciaNota,
            PlantillaNotaVinculadaId = dto.PlantillaNotaVinculadaId,
        };
        _db.ReportesFinancierosSecciones.Add(seccion);
        await _db.SaveChangesAsync();
        return (seccion, null);
    }

    public async Task<(ReporteFinancieroSeccion? Seccion, string? Error)> ActualizarSeccionAsync(Guid empresaId, Guid seccionId, UpdateSeccionReporteDto dto)
    {
        var seccion = await _db.ReportesFinancierosSecciones
            .Include(s => s.Plantilla)
            .FirstOrDefaultAsync(s => s.Id == seccionId && s.Plantilla.EmpresaId == empresaId);
        if (seccion is null) return (null, "Sección no encontrada");

        if (dto.Codigo is not null) seccion.Codigo = dto.Codigo;
        if (dto.Nombre is not null) seccion.Nombre = dto.Nombre;
        if (dto.Orden.HasValue) seccion.Orden = dto.Orden.Value;
        if (dto.Signo.HasValue) seccion.Signo = dto.Signo.Value;
        if (dto.EsTotalDestacado.HasValue) seccion.EsTotalDestacado = dto.EsTotalDestacado.Value;
        if (dto.EsResultadoDelEjercicio.HasValue) seccion.EsResultadoDelEjercicio = dto.EsResultadoDelEjercicio.Value;
        if (dto.ReferenciaNota is not null) seccion.ReferenciaNota = dto.ReferenciaNota;
        if (dto.PlantillaNotaVinculadaId.HasValue) seccion.PlantillaNotaVinculadaId = dto.PlantillaNotaVinculadaId;
        await _db.SaveChangesAsync();
        return (seccion, null);
    }

    /// <summary>Elimina una sección y, recursivamente, todas sus subsecciones (y las cuentas mapeadas de cada una).</summary>
    public async Task<string?> EliminarSeccionAsync(Guid empresaId, Guid seccionId)
    {
        var seccion = await _db.ReportesFinancierosSecciones
            .Include(s => s.Plantilla)
            .FirstOrDefaultAsync(s => s.Id == seccionId && s.Plantilla.EmpresaId == empresaId);
        if (seccion is null) return "Sección no encontrada";

        var todasLasSecciones = await _db.ReportesFinancierosSecciones.Where(s => s.PlantillaId == seccion.PlantillaId).ToListAsync();
        var aEliminar = new List<ReporteFinancieroSeccion>();
        void Recolectar(Guid id)
        {
            var actual = todasLasSecciones.First(s => s.Id == id);
            aEliminar.Add(actual);
            foreach (var hija in todasLasSecciones.Where(s => s.SeccionPadreId == id))
                Recolectar(hija.Id);
        }
        Recolectar(seccionId);

        _db.ReportesFinancierosSecciones.RemoveRange(aEliminar); // cascada elimina las cuentas mapeadas de cada una
        await _db.SaveChangesAsync();
        return null;
    }

    // --- Cuentas mapeadas (dinámica de cuentas) ---

    public async Task<(ReporteFinancieroCuenta? Cuenta, string? Error)> AgregarCuentaMapeadaAsync(Guid empresaId, Guid seccionId, CreateCuentaMapeadaDto dto)
    {
        var seccion = await _db.ReportesFinancierosSecciones
            .Include(s => s.Plantilla)
            .FirstOrDefaultAsync(s => s.Id == seccionId && s.Plantilla.EmpresaId == empresaId);
        if (seccion is null) return (null, "Sección no encontrada");
        if (dto.CuentaId is null && string.IsNullOrWhiteSpace(dto.CodigoPrefijo))
            return (null, "Debe indicar una cuenta específica o un prefijo de código");
        if (dto.CuentaId is not null && !string.IsNullOrWhiteSpace(dto.CodigoPrefijo))
            return (null, "Indique solo una cuenta específica o un prefijo de código, no ambos");
        if (dto.CuentaId.HasValue)
        {
            var cuentaValida = await _db.CuentasContables.AnyAsync(c => c.Id == dto.CuentaId.Value && c.EmpresaId == empresaId);
            if (!cuentaValida) return (null, "La cuenta indicada no pertenece a esta empresa");
        }

        var mapeo = new ReporteFinancieroCuenta
        {
            SeccionId = seccionId,
            CuentaId = dto.CuentaId,
            CodigoPrefijo = dto.CodigoPrefijo?.Trim(),
            Signo = dto.Signo,
        };
        _db.ReportesFinancierosCuentas.Add(mapeo);
        await _db.SaveChangesAsync();
        return (mapeo, null);
    }

    public async Task<string?> EliminarCuentaMapeadaAsync(Guid empresaId, Guid cuentaMapeadaId)
    {
        var mapeo = await _db.ReportesFinancierosCuentas
            .Include(c => c.Seccion).ThenInclude(s => s.Plantilla)
            .FirstOrDefaultAsync(c => c.Id == cuentaMapeadaId && c.Seccion.Plantilla.EmpresaId == empresaId);
        if (mapeo is null) return "Mapeo de cuenta no encontrado";
        _db.ReportesFinancierosCuentas.Remove(mapeo);
        await _db.SaveChangesAsync();
        return null;
    }

    // --- Generación del reporte ---

    /// <summary>
    /// Calcula una <see cref="ReporteFinancieroPlantilla"/> a la fecha de corte indicada, a partir
    /// de los asientos CONFIRMADOs: para cada sección suma el saldo (normalizado por naturaleza)
    /// de las cuentas que le fueron mapeadas más el total de sus subsecciones, salvo que la
    /// sección esté marcada como Resultado del Ejercicio, en cuyo caso se calcula como
    /// Ingresos-Costos-Gastos acumulado al corte.
    /// </summary>
    public async Task<(ReporteFinancieroResultadoDto? Resultado, string? Error)> GenerarReporteAsync(Guid empresaId, Guid plantillaId, DateTime? desde, DateTime? hasta)
    {
        var plantilla = await _db.ReportesFinancierosPlantillas
            .Include(p => p.Secciones).ThenInclude(s => s.Cuentas)
            .FirstOrDefaultAsync(p => p.Id == plantillaId && p.EmpresaId == empresaId);
        if (plantilla is null) return (null, "Plantilla no encontrada");

        var fechaCorte = (hasta ?? DateTime.UtcNow).Date;

        var query = _db.AsientoDetalles
            .Include(d => d.Cuenta)
            .Include(d => d.Asiento)
            .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Estado == EstadoAsiento.CONFIRMADO && d.Asiento.Fecha <= fechaCorte);
        if (desde.HasValue) query = query.Where(d => d.Asiento.Fecha >= desde.Value);
        var movimientos = await query.ToListAsync();

        var saldosPorCuenta = movimientos
            .GroupBy(d => new { d.CuentaId, d.Cuenta.Codigo, d.Cuenta.Nombre, d.Cuenta.Naturaleza })
            .Select(g => new
            {
                g.Key.CuentaId,
                g.Key.Codigo,
                g.Key.Nombre,
                Saldo = g.Key.Naturaleza == NaturalezaCuenta.DEUDORA ? g.Sum(x => x.Debe) - g.Sum(x => x.Haber) : g.Sum(x => x.Haber) - g.Sum(x => x.Debe),
            })
            .ToList();

        decimal CalcularResultadoDelEjercicio()
        {
            var ingresos = movimientos.Where(d => d.Cuenta.Tipo == TipoCuentaContable.INGRESO).Sum(d => d.Haber - d.Debe);
            var costos = movimientos.Where(d => d.Cuenta.Tipo == TipoCuentaContable.COSTO).Sum(d => d.Debe - d.Haber);
            var gastos = movimientos.Where(d => d.Cuenta.Tipo == TipoCuentaContable.GASTO).Sum(d => d.Debe - d.Haber);
            return ingresos - costos - gastos;
        }

        var porPadre = plantilla.Secciones.Where(s => s.SeccionPadreId.HasValue).ToLookup(s => s.SeccionPadreId!.Value);
        var raices = plantilla.Secciones.Where(s => !s.SeccionPadreId.HasValue).OrderBy(s => s.Orden).ToList();

        SeccionReporteDto Calcular(ReporteFinancieroSeccion seccion)
        {
            var hijas = porPadre[seccion.Id].OrderBy(s => s.Orden).Select(Calcular).ToList();
            decimal total;
            var cuentasDto = new List<CuentaReporteDto>();

            if (seccion.EsResultadoDelEjercicio)
            {
                total = CalcularResultadoDelEjercicio();
            }
            else
            {
                var totalCuentas = 0m;
                foreach (var mapeo in seccion.Cuentas)
                {
                    var coincidencias = mapeo.CuentaId.HasValue
                        ? saldosPorCuenta.Where(s => s.CuentaId == mapeo.CuentaId.Value)
                        : saldosPorCuenta.Where(s => s.Codigo.StartsWith(mapeo.CodigoPrefijo!));
                    foreach (var c in coincidencias)
                    {
                        var importe = c.Saldo * mapeo.Signo;
                        totalCuentas += importe;
                        cuentasDto.Add(new CuentaReporteDto(c.CuentaId, c.Codigo, c.Nombre, Math.Round(importe, 2)));
                    }
                }
                total = totalCuentas + hijas.Sum(h => h.Total * h.Signo);
            }

            return new SeccionReporteDto(
                seccion.Id, seccion.Codigo, seccion.Nombre, seccion.Signo, seccion.EsTotalDestacado,
                seccion.ReferenciaNota, seccion.PlantillaNotaVinculadaId,
                Math.Round(total, 2), cuentasDto.OrderBy(c => c.Codigo).ToList(), hijas);
        }

        var secciones = raices.Select(Calcular).ToList();
        var totalGeneral = Math.Round(secciones.Sum(s => s.Total * s.Signo), 2);

        var resultado = new ReporteFinancieroResultadoDto(
            plantilla.Id, plantilla.Codigo, plantilla.Nombre, plantilla.Tipo.ToString(),
            desde, fechaCorte, secciones, totalGeneral);
        return (resultado, null);
    }

    // --- Siembra de plantillas predeterminadas (equivalentes al PCGE fijo que traía el sistema) ---

    /// <summary>
    /// Crea, si la empresa todavía no tiene ninguna, las plantillas predeterminadas de Balance
    /// General y Estado de Resultados (agrupando por <see cref="TipoCuentaContable"/>, igual que
    /// hacía el reporte fijo original) más una Nota a los EEFF de ejemplo ("Efectivo y
    /// Equivalentes de Efectivo"), para que el módulo funcione de inmediato y el usuario las
    /// personalice (reordenar, dividir en corriente/no corriente, agregar más notas, etc.) desde
    /// la pantalla de mantenimiento en vez de tocar código.
    /// </summary>
    public async Task SembrarPlantillasPredeterminadasAsync(Guid empresaId)
    {
        var yaTiene = await _db.ReportesFinancierosPlantillas.AnyAsync(p => p.EmpresaId == empresaId);
        if (yaTiene) return;

        var balance = new ReporteFinancieroPlantilla
        {
            EmpresaId = empresaId,
            Codigo = "BG-PCGE",
            Nombre = "Balance General (Estado de Situación Financiera)",
            Tipo = TipoReporteFinanciero.BALANCE_GENERAL,
            Descripcion = "Plantilla predeterminada: agrupa las cuentas del plan de cuentas por su tipo (Activo/Pasivo/Patrimonio), igual que el reporte original. Personalícela libremente.",
            EsPredeterminada = true,
            Orden = 1,
        };
        var activo = new ReporteFinancieroSeccion { Plantilla = balance, Codigo = "1", Nombre = "Activo", Orden = 1, Signo = 1, EsTotalDestacado = true };
        foreach (var prefijo in new[] { "1", "2", "3" })
            activo.Cuentas.Add(new ReporteFinancieroCuenta { CodigoPrefijo = prefijo, Signo = 1 });

        var pasivo = new ReporteFinancieroSeccion { Plantilla = balance, Codigo = "2", Nombre = "Pasivo", Orden = 2, Signo = -1, EsTotalDestacado = true };
        foreach (var prefijo in new[] { "4" })
            pasivo.Cuentas.Add(new ReporteFinancieroCuenta { CodigoPrefijo = prefijo, Signo = 1 });

        var patrimonio = new ReporteFinancieroSeccion { Plantilla = balance, Codigo = "3", Nombre = "Patrimonio", Orden = 3, Signo = -1, EsTotalDestacado = true };
        foreach (var prefijo in new[] { "5" })
            patrimonio.Cuentas.Add(new ReporteFinancieroCuenta { CodigoPrefijo = prefijo, Signo = 1 });
        var resultadoEjercicio = new ReporteFinancieroSeccion
        {
            Plantilla = balance,
            SeccionPadre = patrimonio,
            Codigo = "3.1",
            Nombre = "Resultado del Ejercicio",
            Orden = 1,
            Signo = 1,
            EsResultadoDelEjercicio = true,
        };

        balance.Secciones = new List<ReporteFinancieroSeccion> { activo, pasivo, patrimonio, resultadoEjercicio };

        var estadoResultados = new ReporteFinancieroPlantilla
        {
            EmpresaId = empresaId,
            Codigo = "ER-PCGE",
            Nombre = "Estado de Resultados",
            Tipo = TipoReporteFinanciero.ESTADO_RESULTADOS,
            Descripcion = "Plantilla predeterminada: Ingresos - Costos = Utilidad Bruta; Utilidad Bruta - Gastos = Utilidad Neta. Personalícela libremente.",
            EsPredeterminada = true,
            Orden = 1,
        };
        var utilidadNeta = new ReporteFinancieroSeccion { Plantilla = estadoResultados, Codigo = "3", Nombre = "Utilidad Neta", Orden = 1, Signo = 1, EsTotalDestacado = true };
        var utilidadBruta = new ReporteFinancieroSeccion { Plantilla = estadoResultados, SeccionPadre = utilidadNeta, Codigo = "1", Nombre = "Utilidad Bruta", Orden = 1, Signo = 1, EsTotalDestacado = true };
        var ingresos = new ReporteFinancieroSeccion { Plantilla = estadoResultados, SeccionPadre = utilidadBruta, Codigo = "1.1", Nombre = "Ingresos", Orden = 1, Signo = 1 };
        ingresos.Cuentas.Add(new ReporteFinancieroCuenta { CodigoPrefijo = "7", Signo = 1 });
        var costos = new ReporteFinancieroSeccion { Plantilla = estadoResultados, SeccionPadre = utilidadBruta, Codigo = "1.2", Nombre = "Costo de Ventas", Orden = 2, Signo = -1 };
        costos.Cuentas.Add(new ReporteFinancieroCuenta { CodigoPrefijo = "6", Signo = 1 });
        var gastos = new ReporteFinancieroSeccion { Plantilla = estadoResultados, SeccionPadre = utilidadNeta, Codigo = "2", Nombre = "Gastos", Orden = 2, Signo = -1 };
        gastos.Cuentas.Add(new ReporteFinancieroCuenta { CodigoPrefijo = "9", Signo = 1 });

        estadoResultados.Secciones = new List<ReporteFinancieroSeccion> { utilidadNeta, utilidadBruta, ingresos, costos, gastos };

        var notaEfectivo = new ReporteFinancieroPlantilla
        {
            EmpresaId = empresaId,
            Codigo = "NOTA-01",
            Nombre = "Nota 1 - Efectivo y Equivalentes de Efectivo",
            Tipo = TipoReporteFinanciero.NOTA_EEFF,
            Descripcion = "Ejemplo de nota a los EEFF: detalla, cuenta por cuenta, el saldo de Caja y Bancos (PCGE clase 10) que se presenta consolidado en el Balance General.",
            Orden = 1,
        };
        var detalleEfectivo = new ReporteFinancieroSeccion { Plantilla = notaEfectivo, Codigo = "1", Nombre = "Efectivo y Equivalentes de Efectivo", Orden = 1, Signo = 1, EsTotalDestacado = true };
        detalleEfectivo.Cuentas.Add(new ReporteFinancieroCuenta { CodigoPrefijo = "10", Signo = 1 });
        notaEfectivo.Secciones = new List<ReporteFinancieroSeccion> { detalleEfectivo };

        _db.ReportesFinancierosPlantillas.AddRange(balance, estadoResultados, notaEfectivo);
        await _db.SaveChangesAsync();

        // Enlaza la Nota 1 a la línea de Activo del Balance (solo informativo/navegable).
        activo.ReferenciaNota = "1";
        activo.PlantillaNotaVinculadaId = notaEfectivo.Id;
        await _db.SaveChangesAsync();
    }
}
