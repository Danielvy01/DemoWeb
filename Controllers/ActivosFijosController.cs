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
/// Libro de Activos Fijos: alta/mantenimiento de activos y generación mensual de la
/// depreciación (línea recta) con su asiento contable asociado, al estilo CONCAR.
/// </summary>
[Authorize]
[MenuPermiso("activos-fijos")]
[ApiController]
[Route("activos-fijos")]
public class ActivosFijosController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly ActivosFijosService _activosFijosService;

    public ActivosFijosController(SiserpDbContext db, ActivosFijosService activosFijosService)
    {
        _db = db;
        _activosFijosService = activosFijosService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateActivoFijoDto dto)
    {
        var empresaId = User.GetEmpresaId();

        var cuentaIds = new[] { dto.CuentaActivoId, dto.CuentaDepreciacionAcumuladaId, dto.CuentaGastoDepreciacionId };
        var cuentasValidas = await _db.CuentasContables.CountAsync(c => c.EmpresaId == empresaId && cuentaIds.Contains(c.Id));
        if (cuentasValidas != cuentaIds.Distinct().Count())
            return BadRequest(new { message = "Alguna cuenta contable no existe" });

        if (dto.CentroCostoId.HasValue)
        {
            var centroExiste = await _db.CentrosCosto.AnyAsync(c => c.Id == dto.CentroCostoId && c.EmpresaId == empresaId);
            if (!centroExiste) return BadRequest(new { message = "El centro de costo indicado no existe" });
        }

        if (dto.VidaUtilMeses <= 0) return BadRequest(new { message = "La vida útil debe ser mayor a cero" });

        var metodoDepreciacion = MetodoDepreciacion.LINEA_RECTA;
        if (!string.IsNullOrWhiteSpace(dto.MetodoDepreciacion) && !Enum.TryParse(dto.MetodoDepreciacion, true, out metodoDepreciacion))
            return BadRequest(new { message = "Método de depreciación inválido" });

        var activo = new ActivoFijo
        {
            EmpresaId = empresaId,
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            Descripcion = dto.Descripcion,
            Categoria = dto.Categoria,
            CentroCostoId = dto.CentroCostoId,
            FechaAdquisicion = dto.FechaAdquisicion,
            FechaInicioDepreciacion = dto.FechaInicioDepreciacion ?? dto.FechaAdquisicion,
            ValorAdquisicion = dto.ValorAdquisicion,
            ValorResidual = dto.ValorResidual ?? 0,
            VidaUtilMeses = dto.VidaUtilMeses,
            MetodoDepreciacion = metodoDepreciacion,
            CuentaActivoId = dto.CuentaActivoId,
            CuentaDepreciacionAcumuladaId = dto.CuentaDepreciacionAcumuladaId,
            CuentaGastoDepreciacionId = dto.CuentaGastoDepreciacionId,
        };
        _db.ActivosFijos.Add(activo);
        await _db.SaveChangesAsync();
        return Ok(activo);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll([FromQuery] string? estado)
    {
        var empresaId = User.GetEmpresaId();
        var query = _db.ActivosFijos.Where(a => a.EmpresaId == empresaId);
        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<EstadoActivoFijo>(estado, true, out var estadoEnum))
            query = query.Where(a => a.Estado == estadoEnum);

        var activos = await query
            .Include(a => a.CentroCosto)
            .Include(a => a.Depreciaciones)
            .OrderBy(a => a.Codigo)
            .ToListAsync();
        return Ok(activos.Select(a => new
        {
            a.Id,
            a.Codigo,
            a.Nombre,
            a.Categoria,
            a.Estado,
            centroCosto = a.CentroCosto?.Nombre,
            a.FechaAdquisicion,
            a.ValorAdquisicion,
            a.ValorResidual,
            a.VidaUtilMeses,
            depreciacionAcumulada = a.Depreciaciones.Sum(d => d.MontoDepreciacion),
            valorNetoLibros = a.ValorAdquisicion - a.Depreciaciones.Sum(d => d.MontoDepreciacion),
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var activo = await _db.ActivosFijos
            .Where(a => a.Id == id && a.EmpresaId == empresaId)
            .Include(a => a.CentroCosto)
            .Include(a => a.CuentaActivo)
            .Include(a => a.CuentaDepreciacionAcumulada)
            .Include(a => a.CuentaGastoDepreciacion)
            .Include(a => a.Depreciaciones.OrderBy(d => d.Periodo))
            .FirstOrDefaultAsync();
        if (activo is null) return NotFound(new { message = "Activo fijo no encontrado" });
        return Ok(activo);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateActivoFijoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var activo = await _db.ActivosFijos.FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);
        if (activo is null) return NotFound(new { message = "Activo fijo no encontrado" });

        if (dto.Nombre is not null) activo.Nombre = dto.Nombre;
        if (dto.Categoria is not null) activo.Categoria = dto.Categoria;
        if (dto.Descripcion is not null) activo.Descripcion = dto.Descripcion;
        if (dto.CentroCostoId is not null) activo.CentroCostoId = dto.CentroCostoId;
        if (dto.CuentaActivoId is not null) activo.CuentaActivoId = dto.CuentaActivoId.Value;
        if (dto.CuentaDepreciacionAcumuladaId is not null) activo.CuentaDepreciacionAcumuladaId = dto.CuentaDepreciacionAcumuladaId.Value;
        if (dto.CuentaGastoDepreciacionId is not null) activo.CuentaGastoDepreciacionId = dto.CuentaGastoDepreciacionId.Value;
        activo.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(activo);
    }

    /// <summary>Da de baja (o marca como vendido) un activo fijo; deja de depreciarse a partir de ese momento.</summary>
    [HttpPost("{id:guid}/dar-baja")]
    public async Task<IActionResult> DarBaja(Guid id, DarBajaActivoFijoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var activo = await _db.ActivosFijos.FirstOrDefaultAsync(a => a.Id == id && a.EmpresaId == empresaId);
        if (activo is null) return NotFound(new { message = "Activo fijo no encontrado" });
        if (activo.Estado is EstadoActivoFijo.DADO_DE_BAJA or EstadoActivoFijo.VENDIDO)
            return BadRequest(new { message = "El activo ya fue dado de baja" });

        activo.Estado = dto.Vendido ? EstadoActivoFijo.VENDIDO : EstadoActivoFijo.DADO_DE_BAJA;
        activo.FechaBaja = dto.Fecha;
        activo.MotivoBaja = dto.Motivo;
        activo.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(activo);
    }

    /// <summary>Genera la depreciación del período ("yyyy-MM") para todos los activos activos pendientes y su asiento contable.</summary>
    [HttpPost("depreciacion/generar")]
    public async Task<IActionResult> GenerarDepreciacion(GenerarDepreciacionDto dto)
    {
        var empresaId = User.GetEmpresaId();
        try
        {
            var generadas = await _activosFijosService.GenerarDepreciacionPeriodoAsync(empresaId, dto.Periodo);
            return Ok(new { generadas = generadas.Count, detalle = generadas });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Libro de Activos Fijos a una fecha de corte (por defecto, hoy): valor de adquisición, depreciación acumulada y valor neto en libros.</summary>
    [HttpGet("libro")]
    public async Task<IActionResult> Libro([FromQuery] DateTime? corte)
    {
        var empresaId = User.GetEmpresaId();
        var resultado = await _activosFijosService.ObtenerLibroAsync(empresaId, corte ?? DateTime.UtcNow);
        return Ok(resultado);
    }
}
