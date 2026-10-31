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
/// Contabilidad: plan de cuentas, Libro Diario (asientos), Libro Mayor y
/// Estado de Resultados, todo derivado de los asientos en estado CONFIRMADO.
/// </summary>
[Authorize]
[MenuPermiso("contabilidad")]
[ApiController]
[Route("contabilidad")]
public class ContabilidadController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly ContabilidadService _contabilidadService;
    private readonly EjercicioContableService _ejercicioService;

    public ContabilidadController(SiserpDbContext db, ContabilidadService contabilidadService, EjercicioContableService ejercicioService)
    {
        _db = db;
        _contabilidadService = contabilidadService;
        _ejercicioService = ejercicioService;
    }

    // --- Plan de cuentas ---

    [HttpPost("cuentas")]
    public async Task<IActionResult> CreateCuenta(CreateCuentaContableDto dto)
    {
        var empresaId = User.GetEmpresaId();
        if (!Enum.TryParse<TipoCuentaContable>(dto.Tipo, true, out var tipo))
            return BadRequest(new { message = "Tipo de cuenta inválido" });
        if (!Enum.TryParse<NaturalezaCuenta>(dto.Naturaleza, true, out var naturaleza))
            return BadRequest(new { message = "Naturaleza de cuenta inválida" });

        var cuenta = new CuentaContable
        {
            EmpresaId = empresaId,
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Tipo = tipo,
            Naturaleza = naturaleza,
            CuentaPadreId = dto.CuentaPadreId,
            RequiereAnalisisDocumentario = dto.RequiereAnalisisDocumentario,
        };
        _db.CuentasContables.Add(cuenta);
        await _db.SaveChangesAsync();
        return Ok(cuenta);
    }

    [HttpGet("cuentas")]
    public async Task<IActionResult> FindCuentas()
    {
        var empresaId = User.GetEmpresaId();
        var cuentas = await _db.CuentasContables
            .Where(c => c.EmpresaId == empresaId)
            .OrderBy(c => c.Codigo)
            .ToListAsync();
        return Ok(cuentas);
    }

    // --- Códigos de operación (cuenta completa + subdiario por tipo de documento/moneda, al estilo CONCAR) ---

    [HttpGet("codigos-operacion")]
    public async Task<IActionResult> FindCodigosOperacion()
    {
        var empresaId = User.GetEmpresaId();
        var codigos = await _db.ConfiguracionesCuentaOperacion
            .Include(c => c.CuentaContable)
            .Where(c => c.EmpresaId == empresaId)
            .OrderBy(c => c.Movimiento).ThenBy(c => c.TipoDocumento).ThenBy(c => c.Moneda)
            .ToListAsync();
        return Ok(codigos);
    }

    [HttpPost("codigos-operacion")]
    public async Task<IActionResult> CreateCodigoOperacion(CreateConfiguracionCuentaOperacionDto dto)
    {
        var empresaId = User.GetEmpresaId();
        if (!Enum.TryParse<MovimientoOperacionContable>(dto.Movimiento, true, out var movimiento))
            return BadRequest(new { message = "Movimiento inválido: debe ser EMITIDO (venta) o RECIBIDO (compra)" });
        if (!Enum.TryParse<TipoSubdiario>(dto.Subdiario, true, out var subdiario))
            return BadRequest(new { message = "Subdiario inválido" });

        var cuenta = await _db.CuentasContables.FirstOrDefaultAsync(c => c.Id == dto.CuentaContableId && c.EmpresaId == empresaId);
        if (cuenta is null) return BadRequest(new { message = "La cuenta contable indicada no existe" });

        var moneda = string.IsNullOrWhiteSpace(dto.Moneda) ? "PEN" : dto.Moneda.ToUpperInvariant();
        var yaExiste = await _db.ConfiguracionesCuentaOperacion.AnyAsync(c =>
            c.EmpresaId == empresaId && c.Movimiento == movimiento && c.TipoDocumento == dto.TipoDocumento && c.Moneda == moneda);
        if (yaExiste)
            return BadRequest(new { message = "Ya existe un código de operación configurado para ese movimiento, tipo de documento y moneda" });

        var configuracion = new ConfiguracionCuentaOperacion
        {
            EmpresaId = empresaId,
            Movimiento = movimiento,
            TipoDocumento = dto.TipoDocumento,
            Moneda = moneda,
            CuentaContableId = dto.CuentaContableId,
            Subdiario = subdiario,
            Codigo = dto.Codigo,
            Descripcion = dto.Descripcion,
            Activo = dto.Activo,
        };
        _db.ConfiguracionesCuentaOperacion.Add(configuracion);
        await _db.SaveChangesAsync();
        return Ok(configuracion);
    }

    [HttpPut("codigos-operacion/{id:guid}")]
    public async Task<IActionResult> UpdateCodigoOperacion(Guid id, CreateConfiguracionCuentaOperacionDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var configuracion = await _db.ConfiguracionesCuentaOperacion.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (configuracion is null) return NotFound(new { message = "Código de operación no encontrado" });

        if (!Enum.TryParse<MovimientoOperacionContable>(dto.Movimiento, true, out var movimiento))
            return BadRequest(new { message = "Movimiento inválido: debe ser EMITIDO (venta) o RECIBIDO (compra)" });
        if (!Enum.TryParse<TipoSubdiario>(dto.Subdiario, true, out var subdiario))
            return BadRequest(new { message = "Subdiario inválido" });
        var cuenta = await _db.CuentasContables.FirstOrDefaultAsync(c => c.Id == dto.CuentaContableId && c.EmpresaId == empresaId);
        if (cuenta is null) return BadRequest(new { message = "La cuenta contable indicada no existe" });

        configuracion.Movimiento = movimiento;
        configuracion.TipoDocumento = dto.TipoDocumento;
        configuracion.Moneda = string.IsNullOrWhiteSpace(dto.Moneda) ? "PEN" : dto.Moneda.ToUpperInvariant();
        configuracion.CuentaContableId = dto.CuentaContableId;
        configuracion.Subdiario = subdiario;
        configuracion.Codigo = dto.Codigo;
        configuracion.Descripcion = dto.Descripcion;
        configuracion.Activo = dto.Activo;
        configuracion.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(configuracion);
    }

    [HttpDelete("codigos-operacion/{id:guid}")]
    public async Task<IActionResult> DeleteCodigoOperacion(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var configuracion = await _db.ConfiguracionesCuentaOperacion.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (configuracion is null) return NotFound(new { message = "Código de operación no encontrado" });
        _db.ConfiguracionesCuentaOperacion.Remove(configuracion);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Libro Diario (asientos) ---

    [HttpPost("asientos")]
    public async Task<IActionResult> CreateAsiento(CreateAsientoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        if (dto.Detalles.Count < 2)
            return BadRequest(new { message = "Un asiento requiere al menos dos líneas (partida doble)" });

        var fecha = dto.Fecha ?? DateTime.UtcNow;
        if (!await _ejercicioService.EstaPeriodoAbiertoAsync(empresaId, fecha))
            return BadRequest(new { message = EjercicioContableService.MensajePeriodoCerrado(fecha) });

        var (detalles, error) = await ValidarYConstruirDetallesAsync(empresaId, fecha, dto.Detalles);
        if (error is not null) return error;

        var numero = await _contabilidadService.ObtenerSiguienteNumeroAsientoAsync(empresaId);
        var usuarioId = User.GetUsuarioId();

        var asiento = new AsientoContable
        {
            EmpresaId = empresaId,
            Numero = numero,
            Fecha = fecha,
            Glosa = dto.Glosa,
            Referencia = dto.Referencia,
            Estado = EstadoAsiento.BORRADOR,
            UsuarioCreacionId = usuarioId,
            Detalles = detalles!,
        };
        _db.AsientosContables.Add(asiento);
        await _contabilidadService.GuardarAsientoConReintentoAsync(asiento);
        _contabilidadService.RegistrarHistorial(asiento, AccionHistorialAsiento.CREACION, null, usuarioId, null);
        await _db.SaveChangesAsync();
        return Ok(await Obtener(asiento.Id, empresaId));
    }

    /// <summary>
    /// Edita la cabecera y líneas de un asiento aún en BORRADOR (el único estado editable: un
    /// asiento CONFIRMADO o ANULADO se corrige con un nuevo asiento, nunca modificando el original).
    /// Registra la versión anterior completa en el historial de auditoría antes de sobrescribirla.
    /// </summary>
    [HttpPut("asientos/{id:guid}")]
    public async Task<IActionResult> EditarAsiento(Guid id, CreateAsientoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var asiento = await _db.AsientosContables
            .Include(a => a.Detalles).ThenInclude(d => d.Cuenta)
            .FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);
        if (asiento is null) return NotFound(new { message = "Asiento no encontrado" });
        if (asiento.Estado != EstadoAsiento.BORRADOR)
            return BadRequest(new { message = "Solo se puede editar un asiento en BORRADOR" });
        if (dto.Detalles.Count < 2)
            return BadRequest(new { message = "Un asiento requiere al menos dos líneas (partida doble)" });

        var fecha = dto.Fecha ?? asiento.Fecha;
        if (!await _ejercicioService.EstaPeriodoAbiertoAsync(empresaId, fecha))
            return BadRequest(new { message = EjercicioContableService.MensajePeriodoCerrado(fecha) });

        var (detalles, error) = await ValidarYConstruirDetallesAsync(empresaId, fecha, dto.Detalles);
        if (error is not null) return error;

        var snapshotAnterior = ContabilidadService.SerializarSnapshotAsiento(asiento);

        _db.AsientoDetalles.RemoveRange(asiento.Detalles);
        asiento.Detalles = detalles!;
        asiento.Fecha = fecha;
        asiento.Glosa = dto.Glosa;
        asiento.Referencia = dto.Referencia;
        asiento.UsuarioModificacionId = User.GetUsuarioId();
        asiento.UpdatedAt = DateTime.UtcNow;
        _contabilidadService.RegistrarHistorial(asiento, AccionHistorialAsiento.MODIFICACION, asiento.Estado, asiento.UsuarioModificacionId, snapshotAnterior);
        await _db.SaveChangesAsync();
        return Ok(await Obtener(asiento.Id, empresaId));
    }

    /// <summary>
    /// Valida las reglas de un asiento (partida doble cuadrada, multimoneda, cuentas y centros de
    /// costo existentes) y arma las líneas (<see cref="AsientoDetalle"/>) listas para asignar a un
    /// <see cref="AsientoContable"/> nuevo o editado. Devuelve el <see cref="IActionResult"/> de
    /// error correspondiente (BadRequest) cuando alguna validación falla, o <c>null</c> si todo es
    /// correcto.
    /// </summary>
    private async Task<(List<AsientoDetalle>? Detalles, IActionResult? Error)> ValidarYConstruirDetallesAsync(Guid empresaId, DateTime fecha, List<CreateAsientoDetalleDto> detallesDto)
    {
        // --- Soporte multimoneda (al estilo CONCAR): cada línea puede pactarse en una moneda
        // distinta de la funcional (soles); Debe/Haber siempre quedan en soles (convertidos con
        // el tipo de cambio indicado o el vigente a la fecha del asiento), y DebeMe/HaberMe
        // conservan el importe original en su moneda de origen para el reporte en moneda extranjera.
        var lineasConvertidas = new List<(CreateAsientoDetalleDto Dto, string Moneda, decimal TipoCambio, decimal Debe, decimal Haber)>();
        foreach (var d in detallesDto)
        {
            var moneda = string.IsNullOrWhiteSpace(d.Moneda) ? "PEN" : d.Moneda!.ToUpperInvariant();
            if (moneda == "PEN")
            {
                lineasConvertidas.Add((d, moneda, 1m, d.Debe, d.Haber));
                continue;
            }
            var tipoCambio = d.TipoCambio ?? await ObtenerTipoCambioVigenteAsync(empresaId, fecha);
            if (tipoCambio is null)
                return (null, BadRequest(new { message = $"No hay tipo de cambio registrado para convertir la línea en {moneda} a soles" }));
            var debeMe = d.DebeMe ?? d.Debe;
            var haberMe = d.HaberMe ?? d.Haber;
            lineasConvertidas.Add((d, moneda, tipoCambio.Value, Math.Round(debeMe * tipoCambio.Value, 2), Math.Round(haberMe * tipoCambio.Value, 2)));
        }

        var totalDebe = Math.Round(lineasConvertidas.Sum(l => l.Debe), 2);
        var totalHaber = Math.Round(lineasConvertidas.Sum(l => l.Haber), 2);
        if (totalDebe != totalHaber)
            return (null, BadRequest(new { message = $"El asiento no está cuadrado: Debe={totalDebe} Haber={totalHaber}" }));

        var cuentaIds = detallesDto.Select(d => d.CuentaId).ToList();
        var cuentasValidas = await _db.CuentasContables.CountAsync(c => c.EmpresaId == empresaId && cuentaIds.Contains(c.Id));
        if (cuentasValidas != cuentaIds.Distinct().Count())
            return (null, BadRequest(new { message = "Alguna cuenta contable no existe" }));

        var centroCostoIds = detallesDto.Where(d => d.CentroCostoId.HasValue).Select(d => d.CentroCostoId!.Value).Distinct().ToList();
        if (centroCostoIds.Count > 0)
        {
            var centrosValidos = await _db.CentrosCosto.CountAsync(c => c.EmpresaId == empresaId && centroCostoIds.Contains(c.Id));
            if (centrosValidos != centroCostoIds.Count)
                return (null, BadRequest(new { message = "Algún centro de costo no existe" }));
        }

        var detalles = lineasConvertidas.Select(l => new AsientoDetalle
        {
            CuentaId = l.Dto.CuentaId,
            Debe = l.Debe,
            Haber = l.Haber,
            Moneda = l.Moneda,
            TipoCambio = l.TipoCambio,
            DebeMe = l.Dto.DebeMe ?? l.Dto.Debe,
            HaberMe = l.Dto.HaberMe ?? l.Dto.Haber,
            Glosa = l.Dto.Glosa,
            CentroCostoId = l.Dto.CentroCostoId,
            ClienteId = l.Dto.ClienteId,
            ProveedorId = l.Dto.ProveedorId,
            NumeroDocumento = l.Dto.NumeroDocumento,
            TipoDocumento = l.Dto.TipoDocumento,
            FechaVencimiento = l.Dto.FechaVencimiento,
        }).ToList();
        return (detalles, null);
    }

    [HttpGet("asientos")]
    public async Task<IActionResult> FindAsientos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var query = _db.AsientosContables.Where(a => a.EmpresaId == empresaId);
        if (desde.HasValue) query = query.Where(a => a.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(a => a.Fecha <= hasta.Value);
        var asientos = await query
            .Include(a => a.Detalles).ThenInclude(d => d.Cuenta)
            .OrderBy(a => a.Numero)
            .ToListAsync();
        return Ok(asientos);
    }

    [HttpPost("asientos/{id:guid}/confirmar")]
    public async Task<IActionResult> ConfirmarAsiento(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var asiento = await _db.AsientosContables
            .Include(a => a.Detalles).ThenInclude(d => d.Cuenta)
            .FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);
        if (asiento is null) return NotFound(new { message = "Asiento no encontrado" });
        if (asiento.Estado != EstadoAsiento.BORRADOR) return BadRequest(new { message = "Solo se puede confirmar un asiento en BORRADOR" });
        if (!await _ejercicioService.EstaPeriodoAbiertoAsync(empresaId, asiento.Fecha))
            return BadRequest(new { message = EjercicioContableService.MensajePeriodoCerrado(asiento.Fecha) });
        var estadoAnterior = asiento.Estado;
        var snapshotAnterior = ContabilidadService.SerializarSnapshotAsiento(asiento);
        asiento.Estado = EstadoAsiento.CONFIRMADO;
        asiento.UpdatedAt = DateTime.UtcNow;
        asiento.UsuarioModificacionId = User.GetUsuarioId();
        foreach (var detalle in asiento.Detalles)
        {
            await _contabilidadService.GenerarDocumentoCtaCteAsync(empresaId, asiento, detalle, detalle.Cuenta);
        }
        _contabilidadService.RegistrarHistorial(asiento, AccionHistorialAsiento.CONFIRMACION, estadoAnterior, asiento.UsuarioModificacionId, snapshotAnterior);
        await _db.SaveChangesAsync();
        return Ok(asiento);
    }

    /// <summary>Tipo de cambio (venta) vigente a la fecha indicada, o null si no hay ninguno registrado hasta esa fecha.</summary>
    private async Task<decimal?> ObtenerTipoCambioVigenteAsync(Guid empresaId, DateTime fecha)
    {
        var tipoCambio = await _db.TiposCambio
            .Where(t => t.EmpresaId == empresaId && t.Fecha <= fecha.Date)
            .OrderByDescending(t => t.Fecha)
            .FirstOrDefaultAsync();
        return tipoCambio?.Venta;
    }

    [HttpPost("asientos/{id:guid}/anular")]
    public async Task<IActionResult> AnularAsiento(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var asiento = await _db.AsientosContables
            .Include(a => a.Detalles).ThenInclude(d => d.Cuenta)
            .FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);
        if (asiento is null) return NotFound(new { message = "Asiento no encontrado" });
        if (!await _ejercicioService.EstaPeriodoAbiertoAsync(empresaId, asiento.Fecha))
            return BadRequest(new { message = EjercicioContableService.MensajePeriodoCerrado(asiento.Fecha) });
        var estadoAnterior = asiento.Estado;
        var snapshotAnterior = ContabilidadService.SerializarSnapshotAsiento(asiento);
        asiento.Estado = EstadoAsiento.ANULADO;
        asiento.UpdatedAt = DateTime.UtcNow;
        asiento.UsuarioModificacionId = User.GetUsuarioId();
        _contabilidadService.RegistrarHistorial(asiento, AccionHistorialAsiento.ANULACION, estadoAnterior, asiento.UsuarioModificacionId, snapshotAnterior);
        await _db.SaveChangesAsync();
        return Ok(asiento);
    }

    // --- Auditoría y Trazabilidad (historial de cambios sobre asientos, al estilo CONCAR) ---

    /// <summary>
    /// Historial completo de un asiento: cada versión (creación, ediciones, confirmación,
    /// anulación) con quién la realizó, cuándo y la fotografía (JSON) del asiento antes/después,
    /// ordenado de la más reciente a la más antigua.
    /// </summary>
    [HttpGet("asientos/{id:guid}/historial")]
    public async Task<IActionResult> HistorialAsiento(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var existe = await _db.AsientosContables.AnyAsync(a => a.Id == id && a.EmpresaId == empresaId);
        if (!existe) return NotFound(new { message = "Asiento no encontrado" });

        var historial = await _db.AsientosContablesHistorial
            .Include(h => h.Usuario)
            .Where(h => h.AsientoId == id)
            .OrderByDescending(h => h.Version)
            .Select(h => new
            {
                h.Id,
                h.Version,
                accion = h.Accion.ToString(),
                usuarioId = h.UsuarioId,
                usuario = h.Usuario != null ? h.Usuario.Nombre : "Sistema",
                h.Fecha,
                estadoAnterior = h.EstadoAnterior == null ? null : h.EstadoAnterior.ToString(),
                estadoNuevo = h.EstadoNuevo.ToString(),
                h.SnapshotAnterior,
                h.SnapshotNuevo,
                h.Observacion,
            })
            .ToListAsync();
        return Ok(historial);
    }

    // --- Provisiones (CxP/CxC con tercero y vencimiento, al estilo CONCAR) ---

    /// <summary>
    /// Registra una provisión: crea directamente un asiento CONFIRMADO con la(s) contrapartida(s)
    /// indicada(s) (gasto, activo, etc.) contra la cuenta de control documentaria, y genera el
    /// <see cref="DocumentoCtaCte"/> pendiente de pago/cobro resultante.
    /// </summary>
    [HttpPost("provisiones")]
    public async Task<IActionResult> CreateProvision(CreateProvisionDto dto)
    {
        var empresaId = User.GetEmpresaId();
        if (!Enum.TryParse<TipoDocumentoCtaCte>(dto.Tipo, true, out var tipo))
            return BadRequest(new { message = "Tipo de provisión inválido (use CXC o CXP)" });
        if (dto.Contrapartidas.Count == 0)
            return BadRequest(new { message = "Debe indicar al menos una contrapartida" });
        if (tipo == TipoDocumentoCtaCte.CXP && dto.ProveedorId is null)
            return BadRequest(new { message = "Una provisión CXP requiere indicar el proveedor" });
        if (tipo == TipoDocumentoCtaCte.CXC && dto.ClienteId is null)
            return BadRequest(new { message = "Una provisión CXC requiere indicar el cliente" });

        var cuentaControl = await _db.CuentasContables.FirstOrDefaultAsync(c => c.Id == dto.CuentaControlId && c.EmpresaId == empresaId);
        if (cuentaControl is null) return BadRequest(new { message = "La cuenta de control no existe" });
        if (!cuentaControl.RequiereAnalisisDocumentario)
            return BadRequest(new { message = "La cuenta de control debe tener habilitado el Análisis de Cuentas (RequiereAnalisisDocumentario)" });

        var cuentaIds = dto.Contrapartidas.Select(c => c.CuentaId).Distinct().ToList();
        var cuentasValidas = await _db.CuentasContables.CountAsync(c => c.EmpresaId == empresaId && cuentaIds.Contains(c.Id));
        if (cuentasValidas != cuentaIds.Count)
            return BadRequest(new { message = "Alguna cuenta contable de la contrapartida no existe" });

        var fecha = dto.Fecha ?? DateTime.UtcNow;
        if (!await _ejercicioService.EstaPeriodoAbiertoAsync(empresaId, fecha))
            return BadRequest(new { message = EjercicioContableService.MensajePeriodoCerrado(fecha) });
        var montoTotal = Math.Round(dto.Contrapartidas.Sum(c => c.Debe + c.Haber), 2);
        if (montoTotal <= 0) return BadRequest(new { message = "El monto de la provisión debe ser mayor a cero" });

        // La cuenta de control se imputa en el sentido contrario a la contrapartida para cuadrar el asiento:
        // CXP (obligación): Debe = contrapartida (gasto/activo), Haber = control. CXC (derecho): Debe = control, Haber = contrapartida (ingreso).
        var lineaControl = new AsientoDetalle
        {
            CuentaId = cuentaControl.Id,
            Debe = tipo == TipoDocumentoCtaCte.CXC ? montoTotal : 0,
            Haber = tipo == TipoDocumentoCtaCte.CXP ? montoTotal : 0,
            DebeMe = tipo == TipoDocumentoCtaCte.CXC ? montoTotal : 0,
            HaberMe = tipo == TipoDocumentoCtaCte.CXP ? montoTotal : 0,
            Moneda = "PEN",
            TipoCambio = 1m,
            TipoDocumento = dto.TipoDocumento,
            Glosa = dto.Glosa,
            CentroCostoId = dto.CentroCostoId,
            ClienteId = dto.ClienteId,
            ProveedorId = dto.ProveedorId,
            NumeroDocumento = dto.NumeroDocumento,
            FechaVencimiento = dto.FechaVencimiento,
        };
        var detalles = new List<AsientoDetalle> { lineaControl };
        detalles.AddRange(dto.Contrapartidas.Select(c => new AsientoDetalle
        {
            CuentaId = c.CuentaId,
            Debe = c.Debe,
            Haber = c.Haber,
            DebeMe = c.Debe,
            HaberMe = c.Haber,
            Moneda = "PEN",
            TipoCambio = 1m,
            Glosa = c.Glosa ?? dto.Glosa,
            CentroCostoId = c.CentroCostoId ?? dto.CentroCostoId,
        }));

        var numero = await _contabilidadService.ObtenerSiguienteNumeroAsientoAsync(empresaId);
        var asiento = new AsientoContable
        {
            EmpresaId = empresaId,
            Numero = numero,
            Fecha = fecha,
            Glosa = dto.Glosa,
            Referencia = dto.NumeroDocumento,
            Estado = EstadoAsiento.CONFIRMADO,
            Detalles = detalles,
        };
        _db.AsientosContables.Add(asiento);

        _db.DocumentosCtaCte.Add(new DocumentoCtaCte
        {
            EmpresaId = empresaId,
            AsientoDetalleId = lineaControl.Id,
            CuentaId = cuentaControl.Id,
            Tipo = tipo,
            ClienteId = dto.ClienteId,
            ProveedorId = dto.ProveedorId,
            TipoDocumento = dto.TipoDocumento,
            NumeroDocumento = dto.NumeroDocumento,
            FechaEmision = fecha,
            FechaVencimiento = dto.FechaVencimiento,
            Glosa = dto.Glosa,
            CentroCostoId = dto.CentroCostoId,
            MontoOriginal = montoTotal,
            MontoCancelado = 0,
            Estado = EstadoDocumentoCtaCte.PENDIENTE,
        });

        await _contabilidadService.GuardarAsientoConReintentoAsync(asiento);
        return Ok(await Obtener(asiento.Id, empresaId));
    }

    // --- Análisis de Cuentas (cuenta corriente de CxC/CxP por tercero, con vencimientos) ---

    /// <summary>
    /// Lista los documentos de cuenta corriente (provisiones, ventas al crédito, etc.) con su
    /// saldo pendiente y el tramo de vencimiento (al estilo del reporte "Análisis de Cuentas" de
    /// CONCAR: vencido por tramos de antigüedad o por vencer).
    /// </summary>
    [HttpGet("analisis-cuentas")]
    public async Task<IActionResult> AnalisisCuentas(
        [FromQuery] Guid? cuentaId, [FromQuery] string? tipo, [FromQuery] string? estado,
        [FromQuery] Guid? clienteId, [FromQuery] Guid? proveedorId,
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] bool soloPendientes = false)
    {
        var empresaId = User.GetEmpresaId();
        var query = _db.DocumentosCtaCte
            .Include(d => d.Cuenta)
            .Include(d => d.Cliente)
            .Include(d => d.Proveedor)
            .Include(d => d.CentroCosto)
            .Where(d => d.EmpresaId == empresaId);

        if (cuentaId.HasValue) query = query.Where(d => d.CuentaId == cuentaId.Value);
        if (clienteId.HasValue) query = query.Where(d => d.ClienteId == clienteId.Value);
        if (proveedorId.HasValue) query = query.Where(d => d.ProveedorId == proveedorId.Value);
        if (desde.HasValue) query = query.Where(d => d.FechaEmision >= desde.Value);
        if (hasta.HasValue) query = query.Where(d => d.FechaEmision <= hasta.Value);
        if (!string.IsNullOrWhiteSpace(tipo) && Enum.TryParse<TipoDocumentoCtaCte>(tipo, true, out var tipoParsed))
            query = query.Where(d => d.Tipo == tipoParsed);
        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<EstadoDocumentoCtaCte>(estado, true, out var estadoParsed))
            query = query.Where(d => d.Estado == estadoParsed);
        if (soloPendientes) query = query.Where(d => d.Estado == EstadoDocumentoCtaCte.PENDIENTE || d.Estado == EstadoDocumentoCtaCte.PARCIAL);

        var documentos = await query.OrderBy(d => d.FechaVencimiento).ToListAsync();
        var hoy = DateTime.UtcNow.Date;

        string TramoVencimiento(DateTime vencimiento, decimal saldo)
        {
            if (saldo <= 0) return "CANCELADO";
            var dias = (hoy - vencimiento.Date).Days;
            if (dias <= 0) return "POR_VENCER";
            if (dias <= 30) return "VENCIDO_1_30";
            if (dias <= 60) return "VENCIDO_31_60";
            if (dias <= 90) return "VENCIDO_61_90";
            return "VENCIDO_MAS_90";
        }

        var resultado = documentos.Select(d => new
        {
            d.Id,
            d.Tipo,
            cuenta = new { d.CuentaId, d.Cuenta.Codigo, d.Cuenta.Nombre },
            tercero = d.Cliente != null ? d.Cliente.NombreORazonSocial : d.Proveedor != null ? d.Proveedor.RazonSocial : null,
            d.TipoDocumento,
            d.NumeroDocumento,
            d.FechaEmision,
            d.FechaVencimiento,
            d.Glosa,
            centroCosto = d.CentroCosto != null ? d.CentroCosto.Nombre : null,
            d.MontoOriginal,
            d.MontoCancelado,
            saldoPendiente = d.SaldoPendiente,
            d.Estado,
            diasVencido = Math.Max(0, (hoy - d.FechaVencimiento.Date).Days),
            tramo = TramoVencimiento(d.FechaVencimiento, d.SaldoPendiente),
        }).ToList();

        var resumen = new
        {
            totalOriginal = documentos.Sum(d => d.MontoOriginal),
            totalCancelado = documentos.Sum(d => d.MontoCancelado),
            totalPendiente = documentos.Sum(d => d.SaldoPendiente),
            porTramo = resultado.Where(r => r.saldoPendiente > 0)
                .GroupBy(r => r.tramo)
                .Select(g => new { tramo = g.Key, monto = g.Sum(r => r.saldoPendiente), cantidad = g.Count() }),
        };

        return Ok(new { documentos = resultado, resumen });
    }

    /// <summary>Detalle de un documento de cuenta corriente con sus aplicaciones (cobros/pagos) previas.</summary>
    [HttpGet("documentos/{id:guid}")]
    public async Task<IActionResult> ObtenerDocumento(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var documento = await _db.DocumentosCtaCte
            .Include(d => d.Cuenta)
            .Include(d => d.Cliente)
            .Include(d => d.Proveedor)
            .Include(d => d.Aplicaciones).ThenInclude(a => a.Asiento)
            .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId);
        if (documento is null) return NotFound(new { message = "Documento no encontrado" });
        return Ok(documento);
    }

    /// <summary>
    /// Cancela (total o parcialmente) un documento de cuenta corriente pendiente: registra el
    /// asiento de pago/cobro (Debe/Haber cuenta de control vs. caja/bancos) y actualiza el saldo
    /// y estado del documento. Equivale al "cruce" de documentos de CONCAR.
    /// </summary>
    [HttpPost("documentos/{id:guid}/cancelar")]
    public async Task<IActionResult> CancelarDocumento(Guid id, CancelarDocumentoCtaCteDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var documento = await _db.DocumentosCtaCte
            .Include(d => d.Cuenta)
            .FirstOrDefaultAsync(d => d.Id == id && d.EmpresaId == empresaId);
        if (documento is null) return NotFound(new { message = "Documento no encontrado" });
        if (documento.Estado is EstadoDocumentoCtaCte.CANCELADO or EstadoDocumentoCtaCte.ANULADO)
            return BadRequest(new { message = "El documento ya no tiene saldo pendiente" });
        if (dto.Monto <= 0) return BadRequest(new { message = "El monto a cancelar debe ser mayor a cero" });
        var saldoPendiente = Math.Round(documento.SaldoPendiente, 2);
        if (dto.Monto > saldoPendiente)
            return BadRequest(new { message = $"El monto excede el saldo pendiente ({saldoPendiente})" });

        var cuentaCaja = await _db.CuentasContables.FirstOrDefaultAsync(c => c.Id == dto.CuentaCajaId && c.EmpresaId == empresaId);
        if (cuentaCaja is null) return BadRequest(new { message = "La cuenta de caja/bancos no existe" });

        var fecha = dto.Fecha ?? DateTime.UtcNow;
        if (!await _ejercicioService.EstaPeriodoAbiertoAsync(empresaId, fecha))
            return BadRequest(new { message = EjercicioContableService.MensajePeriodoCerrado(fecha) });
        var glosa = dto.Glosa ?? $"{(documento.Tipo == TipoDocumentoCtaCte.CXC ? "Cobro" : "Pago")} {documento.TipoDocumento} {documento.NumeroDocumento}";

        // CXC (cobro): Debe caja, Haber cuenta por cobrar. CXP (pago): Debe cuenta por pagar, Haber caja.
        var detalles = documento.Tipo == TipoDocumentoCtaCte.CXC
            ? new List<AsientoDetalle>
            {
                new() { CuentaId = cuentaCaja.Id, Debe = dto.Monto, Haber = 0, Glosa = glosa, CentroCostoId = dto.CentroCostoId ?? documento.CentroCostoId },
                new() { CuentaId = documento.CuentaId, Debe = 0, Haber = dto.Monto, Glosa = glosa, CentroCostoId = dto.CentroCostoId ?? documento.CentroCostoId },
            }
            : new List<AsientoDetalle>
            {
                new() { CuentaId = documento.CuentaId, Debe = dto.Monto, Haber = 0, Glosa = glosa, CentroCostoId = dto.CentroCostoId ?? documento.CentroCostoId },
                new() { CuentaId = cuentaCaja.Id, Debe = 0, Haber = dto.Monto, Glosa = glosa, CentroCostoId = dto.CentroCostoId ?? documento.CentroCostoId },
            };

        var numero = await _contabilidadService.ObtenerSiguienteNumeroAsientoAsync(empresaId);
        var asientoPago = new AsientoContable
        {
            EmpresaId = empresaId,
            Numero = numero,
            Fecha = fecha,
            Glosa = glosa,
            Referencia = documento.NumeroDocumento,
            Estado = EstadoAsiento.CONFIRMADO,
            Subdiario = TipoSubdiario.CAJA_BANCOS,
            Detalles = detalles,
        };
        _db.AsientosContables.Add(asientoPago);

        documento.MontoCancelado = Math.Round(documento.MontoCancelado + dto.Monto, 2);
        documento.Estado = documento.MontoCancelado >= documento.MontoOriginal ? EstadoDocumentoCtaCte.CANCELADO : EstadoDocumentoCtaCte.PARCIAL;
        documento.UpdatedAt = DateTime.UtcNow;

        _db.AplicacionesDocumentoCtaCte.Add(new AplicacionDocumentoCtaCte
        {
            DocumentoCtaCteId = documento.Id,
            AsientoId = asientoPago.Id,
            Fecha = fecha,
            Monto = dto.Monto,
            Glosa = glosa,
        });

        await _contabilidadService.GuardarAsientoConReintentoAsync(asientoPago);
        return Ok(documento);
    }

    // --- Cierre y apertura de ejercicio contable ---

    /// <summary>Lista los ejercicios contables (años fiscales) de la empresa, con sus periodos mensuales.</summary>
    [HttpGet("ejercicios")]
    public async Task<IActionResult> FindEjercicios()
    {
        var empresaId = User.GetEmpresaId();
        var ejercicios = await _db.EjerciciosContables
            .Where(e => e.EmpresaId == empresaId)
            .Include(e => e.Periodos.OrderBy(p => p.Mes))
            .OrderByDescending(e => e.Anio)
            .ToListAsync();
        return Ok(ejercicios);
    }

    /// <summary>
    /// Abre un nuevo ejercicio contable: crea sus 12 periodos (ABIERTO) y, si el ejercicio anterior
    /// existe y está CERRADO, genera automáticamente el Asiento de Apertura con los saldos de
    /// Activo/Pasivo/Patrimonio arrastrados como saldo inicial al 1 de enero.
    /// </summary>
    [HttpPost("ejercicios")]
    public async Task<IActionResult> AbrirEjercicio(AbrirEjercicioDto dto)
    {
        var empresaId = User.GetEmpresaId();
        if (dto.Anio < 2000 || dto.Anio > 2100) return BadRequest(new { message = "Año de ejercicio inválido" });
        var (ejercicio, error) = await _ejercicioService.AbrirEjercicioAsync(empresaId, dto.Anio);
        if (error != null) return BadRequest(new { message = error });
        return Ok(ejercicio);
    }

    /// <summary>
    /// Cierra el ejercicio contable: cierra los periodos que sigan abiertos y genera el Asiento de
    /// Cierre, que cancela las cuentas de Ingresos/Costos/Gastos contra Resultados del Ejercicio
    /// (Patrimonio), dejando el resultado (utilidad o pérdida) listo para arrastrarse al abrir el siguiente año.
    /// </summary>
    [HttpPost("ejercicios/{id:guid}/cerrar")]
    public async Task<IActionResult> CerrarEjercicio(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var (ejercicio, error) = await _ejercicioService.CerrarEjercicioAsync(empresaId, id);
        if (error != null) return BadRequest(new { message = error });
        return Ok(ejercicio);
    }

    /// <summary>Lista los periodos (meses) de un ejercicio contable.</summary>
    [HttpGet("periodos")]
    public async Task<IActionResult> FindPeriodos([FromQuery] Guid? ejercicioId)
    {
        var empresaId = User.GetEmpresaId();
        var query = _db.PeriodosContables.Where(p => p.EmpresaId == empresaId);
        if (ejercicioId.HasValue) query = query.Where(p => p.EjercicioId == ejercicioId.Value);
        var periodos = await query.OrderBy(p => p.Anio).ThenBy(p => p.Mes).ToListAsync();
        return Ok(periodos);
    }

    /// <summary>Cierra manualmente un periodo (mes) puntual, bloqueando nuevos asientos con fecha en ese mes.</summary>
    [HttpPost("periodos/{id:guid}/cerrar")]
    public async Task<IActionResult> CerrarPeriodo(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var (periodo, error) = await _ejercicioService.CerrarPeriodoAsync(empresaId, id);
        if (error != null) return BadRequest(new { message = error });
        return Ok(periodo);
    }

    /// <summary>Reabre un periodo previamente cerrado (p. ej. para corregir un asiento), si su ejercicio sigue abierto.</summary>
    [HttpPost("periodos/{id:guid}/abrir")]
    public async Task<IActionResult> AbrirPeriodo(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var (periodo, error) = await _ejercicioService.AbrirPeriodoAsync(empresaId, id);
        if (error != null) return BadRequest(new { message = error });
        return Ok(periodo);
    }

    // --- Reportes ---

    /// <summary>Libro Mayor: movimientos agrupados por cuenta con saldo acumulado.</summary>
    [HttpGet("libro-mayor")]
    public async Task<IActionResult> LibroMayor([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] Guid? cuentaId, [FromQuery] Guid? centroCostoId)
    {
        var empresaId = User.GetEmpresaId();
        var query = _db.AsientoDetalles
            .Include(d => d.Cuenta)
            .Include(d => d.Asiento)
            .Include(d => d.CentroCosto)
            .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Estado == EstadoAsiento.CONFIRMADO);
        if (desde.HasValue) query = query.Where(d => d.Asiento.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(d => d.Asiento.Fecha <= hasta.Value);
        if (cuentaId.HasValue) query = query.Where(d => d.CuentaId == cuentaId.Value);
        if (centroCostoId.HasValue) query = query.Where(d => d.CentroCostoId == centroCostoId.Value);

        var movimientos = await query.OrderBy(d => d.Cuenta.Codigo).ThenBy(d => d.Asiento.Fecha).ToListAsync();

        var resultado = movimientos
            .GroupBy(d => new { d.CuentaId, d.Cuenta.Codigo, d.Cuenta.Nombre, d.Cuenta.Naturaleza })
            .Select(g =>
            {
                var totalDebe = g.Sum(d => d.Debe);
                var totalHaber = g.Sum(d => d.Haber);
                var saldo = g.Key.Naturaleza == NaturalezaCuenta.DEUDORA ? totalDebe - totalHaber : totalHaber - totalDebe;
                return new
                {
                    cuentaId = g.Key.CuentaId,
                    codigo = g.Key.Codigo,
                    nombre = g.Key.Nombre,
                    totalDebe,
                    totalHaber,
                    saldo,
                    movimientos = g.Select(d => new { d.Asiento.Numero, d.Asiento.Fecha, d.Asiento.Glosa, d.Debe, d.Haber, centroCosto = d.CentroCosto != null ? d.CentroCosto.Nombre : null }),
                };
            })
            .OrderBy(r => r.codigo)
            .ToList();

        return Ok(resultado);
    }

    /// <summary>Estado de Resultados: Ingresos - Costos - Gastos = Utilidad del período.</summary>
    [HttpGet("estado-resultados")]
    public async Task<IActionResult> EstadoResultados([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var query = _db.AsientoDetalles
            .Include(d => d.Cuenta)
            .Include(d => d.Asiento)
            .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Estado == EstadoAsiento.CONFIRMADO
                        && (d.Cuenta.Tipo == TipoCuentaContable.INGRESO || d.Cuenta.Tipo == TipoCuentaContable.COSTO || d.Cuenta.Tipo == TipoCuentaContable.GASTO));
        if (desde.HasValue) query = query.Where(d => d.Asiento.Fecha >= desde.Value);
        if (hasta.HasValue) query = query.Where(d => d.Asiento.Fecha <= hasta.Value);

        var movimientos = await query.ToListAsync();

        decimal SumaCuenta(TipoCuentaContable tipo) => movimientos
            .Where(d => d.Cuenta.Tipo == tipo)
            .Sum(d => tipo == TipoCuentaContable.INGRESO ? d.Haber - d.Debe : d.Debe - d.Haber);

        var ingresos = SumaCuenta(TipoCuentaContable.INGRESO);
        var costos = SumaCuenta(TipoCuentaContable.COSTO);
        var gastos = SumaCuenta(TipoCuentaContable.GASTO);
        var utilidadBruta = ingresos - costos;
        var utilidadNeta = utilidadBruta - gastos;

        return Ok(new
        {
            desde,
            hasta,
            ingresos,
            costos,
            utilidadBruta,
            gastos,
            utilidadNeta,
        });
    }

    /// <summary>
    /// Balance General (Estado de Situación Financiera): saldos acumulados de Activo, Pasivo y
    /// Patrimonio a una fecha de corte, incluyendo el Resultado del Ejercicio (utilidad/pérdida
    /// acumulada hasta esa fecha) dentro del Patrimonio, al estilo del reporte homónimo de
    /// CONCAR. <c>diferencia</c> debe ser 0 si la partida doble de todos los asientos confirmados
    /// está correctamente cuadrada.
    /// </summary>
    [HttpGet("balance-general")]
    public async Task<IActionResult> BalanceGeneral([FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var fechaCorte = (hasta ?? DateTime.UtcNow).Date;

        var movimientos = await _db.AsientoDetalles
            .Include(d => d.Cuenta)
            .Include(d => d.Asiento)
            .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Estado == EstadoAsiento.CONFIRMADO && d.Asiento.Fecha <= fechaCorte)
            .ToListAsync();

        static decimal SaldoCuenta(IEnumerable<AsientoDetalle> movs, NaturalezaCuenta naturaleza)
        {
            var debe = movs.Sum(d => d.Debe);
            var haber = movs.Sum(d => d.Haber);
            return naturaleza == NaturalezaCuenta.DEUDORA ? debe - haber : haber - debe;
        }

        List<CuentaSaldoBalance> ConstruirCuentas(TipoCuentaContable tipo) => movimientos
            .Where(d => d.Cuenta.Tipo == tipo)
            .GroupBy(d => new { d.CuentaId, d.Cuenta.Codigo, d.Cuenta.Nombre, d.Cuenta.Naturaleza })
            .Select(g => new CuentaSaldoBalance(g.Key.CuentaId, g.Key.Codigo, g.Key.Nombre, SaldoCuenta(g, g.Key.Naturaleza)))
            .Where(c => c.Saldo != 0)
            .OrderBy(c => c.Codigo)
            .ToList();

        var cuentasActivo = ConstruirCuentas(TipoCuentaContable.ACTIVO);
        var cuentasPasivo = ConstruirCuentas(TipoCuentaContable.PASIVO);
        var cuentasPatrimonio = ConstruirCuentas(TipoCuentaContable.PATRIMONIO);

        var totalActivo = cuentasActivo.Sum(c => c.Saldo);
        var totalPasivo = cuentasPasivo.Sum(c => c.Saldo);
        var totalPatrimonio = cuentasPatrimonio.Sum(c => c.Saldo);

        // Resultado del ejercicio acumulado hasta la fecha de corte (Ingresos - Costos - Gastos),
        // se añade al Patrimonio como "utilidad del ejercicio" para que el balance cuadre.
        var movsResultado = movimientos.Where(d => d.Cuenta.Tipo is TipoCuentaContable.INGRESO or TipoCuentaContable.COSTO or TipoCuentaContable.GASTO);
        var ingresos = movsResultado.Where(d => d.Cuenta.Tipo == TipoCuentaContable.INGRESO).Sum(d => d.Haber - d.Debe);
        var costos = movsResultado.Where(d => d.Cuenta.Tipo == TipoCuentaContable.COSTO).Sum(d => d.Debe - d.Haber);
        var gastos = movsResultado.Where(d => d.Cuenta.Tipo == TipoCuentaContable.GASTO).Sum(d => d.Debe - d.Haber);
        var resultadoEjercicio = Math.Round(ingresos - costos - gastos, 2);

        var totalPasivoPatrimonio = Math.Round(totalPasivo + totalPatrimonio + resultadoEjercicio, 2);
        var totalActivoRedondeado = Math.Round(totalActivo, 2);

        return Ok(new
        {
            fechaCorte,
            activo = new { cuentas = cuentasActivo, total = totalActivoRedondeado },
            pasivo = new { cuentas = cuentasPasivo, total = Math.Round(totalPasivo, 2) },
            patrimonio = new { cuentas = cuentasPatrimonio, resultadoEjercicio, total = Math.Round(totalPatrimonio + resultadoEjercicio, 2) },
            totalPasivoPatrimonio,
            diferencia = Math.Round(totalActivoRedondeado - totalPasivoPatrimonio, 2),
        });
    }

    /// <summary>
    /// Libro Caja y Bancos consolidado: movimientos de todas las cuentas de Caja y Bancos (PCGE
    /// clase 10) con saldo corriente por sub-cuenta (caja, cada cuenta bancaria) y un consolidado
    /// cronológico único con saldo acumulado, al estilo del Libro Caja y Bancos de CONCAR.
    /// </summary>
    [HttpGet("libro-caja-bancos")]
    public async Task<IActionResult> LibroCajaBancos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] Guid? cuentaId, [FromQuery] Guid? centroCostoId)
    {
        var empresaId = User.GetEmpresaId();
        var cuentasCaja = await _db.CuentasContables
            .Where(c => c.EmpresaId == empresaId && c.Codigo.StartsWith("10"))
            .OrderBy(c => c.Codigo)
            .ToListAsync();
        if (cuentaId.HasValue) cuentasCaja = cuentasCaja.Where(c => c.Id == cuentaId.Value).ToList();
        if (cuentasCaja.Count == 0)
        {
            return Ok(new
            {
                cuentas = Array.Empty<object>(),
                consolidado = new { saldoInicial = 0m, ingresos = 0m, egresos = 0m, saldoFinal = 0m, movimientos = Array.Empty<object>() },
            });
        }

        var cuentaCajaIds = cuentasCaja.Select(c => c.Id).ToList();
        var query = _db.AsientoDetalles
            .Include(d => d.Asiento)
            .Include(d => d.Cuenta)
            .Include(d => d.Cliente)
            .Include(d => d.Proveedor)
            .Include(d => d.CentroCosto)
            .Where(d => d.Asiento.EmpresaId == empresaId && d.Asiento.Estado == EstadoAsiento.CONFIRMADO && cuentaCajaIds.Contains(d.CuentaId));
        if (centroCostoId.HasValue) query = query.Where(d => d.CentroCostoId == centroCostoId.Value);

        var fechaDesde = desde?.Date;
        var fechaHasta = hasta?.Date;

        var anteriores = fechaDesde.HasValue
            ? await query.Where(d => d.Asiento.Fecha < fechaDesde.Value).ToListAsync()
            : new List<AsientoDetalle>();

        var periodoQuery = query;
        if (fechaDesde.HasValue) periodoQuery = periodoQuery.Where(d => d.Asiento.Fecha >= fechaDesde.Value);
        if (fechaHasta.HasValue) periodoQuery = periodoQuery.Where(d => d.Asiento.Fecha <= fechaHasta.Value);
        var movimientosPeriodo = await periodoQuery.OrderBy(d => d.Asiento.Fecha).ThenBy(d => d.Asiento.Numero).ToListAsync();

        object ConstruirCuenta(CuentaContable cuenta)
        {
            var saldoInicial = anteriores.Where(d => d.CuentaId == cuenta.Id).Sum(d => d.Debe - d.Haber);
            var movs = movimientosPeriodo.Where(d => d.CuentaId == cuenta.Id).ToList();
            var saldo = saldoInicial;
            var lista = movs.Select(d =>
            {
                saldo += d.Debe - d.Haber;
                return new
                {
                    fecha = d.Asiento.Fecha,
                    numero = d.Asiento.Numero,
                    glosa = d.Glosa ?? d.Asiento.Glosa,
                    tercero = d.Cliente != null ? d.Cliente.NombreORazonSocial : d.Proveedor != null ? d.Proveedor.RazonSocial : null,
                    numeroDocumento = d.NumeroDocumento,
                    centroCosto = d.CentroCosto != null ? d.CentroCosto.Nombre : null,
                    ingreso = d.Debe,
                    egreso = d.Haber,
                    saldo,
                };
            }).ToList();
            return new
            {
                cuentaId = cuenta.Id,
                cuenta.Codigo,
                cuenta.Nombre,
                saldoInicial,
                ingresos = movs.Sum(d => d.Debe),
                egresos = movs.Sum(d => d.Haber),
                saldoFinal = saldo,
                movimientos = lista,
            };
        }

        var cuentasResultado = cuentasCaja.Select(ConstruirCuenta).ToList();

        var saldoInicialConsolidado = anteriores.Sum(d => d.Debe - d.Haber);
        var saldoConsolidado = saldoInicialConsolidado;
        var movimientosConsolidados = movimientosPeriodo.Select(d =>
        {
            saldoConsolidado += d.Debe - d.Haber;
            return new
            {
                fecha = d.Asiento.Fecha,
                numero = d.Asiento.Numero,
                cuenta = $"{d.Cuenta.Codigo} - {d.Cuenta.Nombre}",
                glosa = d.Glosa ?? d.Asiento.Glosa,
                tercero = d.Cliente != null ? d.Cliente.NombreORazonSocial : d.Proveedor != null ? d.Proveedor.RazonSocial : null,
                numeroDocumento = d.NumeroDocumento,
                ingreso = d.Debe,
                egreso = d.Haber,
                saldo = saldoConsolidado,
            };
        }).ToList();

        var consolidado = new
        {
            saldoInicial = saldoInicialConsolidado,
            ingresos = movimientosPeriodo.Sum(d => d.Debe),
            egresos = movimientosPeriodo.Sum(d => d.Haber),
            saldoFinal = saldoConsolidado,
            movimientos = movimientosConsolidados,
        };

        return Ok(new { cuentas = cuentasResultado, consolidado });
    }

    private async Task<AsientoContable?> Obtener(Guid id, Guid empresaId) =>
        await _db.AsientosContables
            .Where(a => a.Id == id && a.EmpresaId == empresaId)
            .Include(a => a.Detalles).ThenInclude(d => d.Cuenta)
            .FirstOrDefaultAsync();
}

/// <summary>Saldo acumulado de una cuenta a la fecha de corte del Balance General.</summary>
public record CuentaSaldoBalance(Guid CuentaId, string Codigo, string Nombre, decimal Saldo);
