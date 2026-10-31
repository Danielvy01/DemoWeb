using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siserp.Api.Common;
using Siserp.Api.Dtos;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Services;

namespace Siserp.Api.Controllers;

/// <summary>
/// NIIF / Dinámica de Cuentas: mantenimiento de plantillas configurables de reportes financieros
/// (Balance General, Estado de Resultados) y Notas a los Estados Financieros NIIF, y generación de
/// dichos reportes a partir del plan de cuentas y los asientos confirmados. Reemplaza, de forma
/// configurable por el usuario, la agrupación fija por tipo de cuenta que usaban antes los
/// reportes del módulo de Contabilidad.
/// </summary>
[Authorize]
[MenuPermiso("dinamica-cuentas")]
[ApiController]
[Route("reportes-financieros")]
public class ReportesFinancierosController : ControllerBase
{
    private readonly ReporteFinancieroService _service;

    public ReportesFinancierosController(ReporteFinancieroService service)
    {
        _service = service;
    }

    // --- Plantillas ---

    [HttpGet("plantillas")]
    public async Task<IActionResult> ListarPlantillas([FromQuery] string? tipo)
    {
        var empresaId = User.GetEmpresaId();
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            if (!Enum.TryParse<Models.TipoReporteFinanciero>(tipo, true, out var tipoEnum))
                return BadRequest(new { message = "Tipo de reporte inválido" });
            return Ok(await _service.ListarPlantillasAsync(empresaId, tipoEnum));
        }
        return Ok(await _service.ListarPlantillasAsync(empresaId));
    }

    [HttpGet("plantillas/{id:guid}")]
    public async Task<IActionResult> ObtenerPlantilla(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var plantilla = await _service.ObtenerPlantillaAsync(empresaId, id);
        if (plantilla is null) return NotFound(new { message = "Plantilla no encontrada" });
        return Ok(plantilla);
    }

    [HttpPost("plantillas")]
    public async Task<IActionResult> CrearPlantilla(CreatePlantillaReporteDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var (plantilla, error) = await _service.CrearPlantillaAsync(empresaId, dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(plantilla);
    }

    [HttpPut("plantillas/{id:guid}")]
    public async Task<IActionResult> ActualizarPlantilla(Guid id, UpdatePlantillaReporteDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var (plantilla, error) = await _service.ActualizarPlantillaAsync(empresaId, id, dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(plantilla);
    }

    [HttpDelete("plantillas/{id:guid}")]
    public async Task<IActionResult> EliminarPlantilla(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var error = await _service.EliminarPlantillaAsync(empresaId, id);
        if (error != null) return BadRequest(new { message = error });
        return Ok(new { message = "Plantilla eliminada" });
    }

    /// <summary>Crea, si la empresa todavía no tiene ninguna, las plantillas predeterminadas (Balance General, Estado de Resultados y una Nota NIIF de ejemplo).</summary>
    [HttpPost("plantillas/sembrar-predeterminadas")]
    public async Task<IActionResult> SembrarPredeterminadas()
    {
        var empresaId = User.GetEmpresaId();
        await _service.SembrarPlantillasPredeterminadasAsync(empresaId);
        return Ok(await _service.ListarPlantillasAsync(empresaId));
    }

    // --- Secciones (estructura del reporte) ---

    [HttpPost("plantillas/{plantillaId:guid}/secciones")]
    public async Task<IActionResult> CrearSeccion(Guid plantillaId, CreateSeccionReporteDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var (seccion, error) = await _service.CrearSeccionAsync(empresaId, plantillaId, dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(seccion);
    }

    [HttpPut("secciones/{seccionId:guid}")]
    public async Task<IActionResult> ActualizarSeccion(Guid seccionId, UpdateSeccionReporteDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var (seccion, error) = await _service.ActualizarSeccionAsync(empresaId, seccionId, dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(seccion);
    }

    [HttpDelete("secciones/{seccionId:guid}")]
    public async Task<IActionResult> EliminarSeccion(Guid seccionId)
    {
        var empresaId = User.GetEmpresaId();
        var error = await _service.EliminarSeccionAsync(empresaId, seccionId);
        if (error != null) return BadRequest(new { message = error });
        return Ok(new { message = "Sección eliminada" });
    }

    // --- Cuentas mapeadas (dinámica de cuentas) ---

    [HttpPost("secciones/{seccionId:guid}/cuentas")]
    public async Task<IActionResult> AgregarCuentaMapeada(Guid seccionId, CreateCuentaMapeadaDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var (cuenta, error) = await _service.AgregarCuentaMapeadaAsync(empresaId, seccionId, dto);
        if (error != null) return BadRequest(new { message = error });
        return Ok(cuenta);
    }

    [HttpDelete("cuentas-mapeadas/{cuentaMapeadaId:guid}")]
    public async Task<IActionResult> EliminarCuentaMapeada(Guid cuentaMapeadaId)
    {
        var empresaId = User.GetEmpresaId();
        var error = await _service.EliminarCuentaMapeadaAsync(empresaId, cuentaMapeadaId);
        if (error != null) return BadRequest(new { message = error });
        return Ok(new { message = "Cuenta desvinculada de la sección" });
    }

    // --- Generación del reporte ---

    [HttpGet("plantillas/{plantillaId:guid}/generar")]
    public async Task<IActionResult> Generar(Guid plantillaId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var (resultado, error) = await _service.GenerarReporteAsync(empresaId, plantillaId, desde, hasta);
        if (error != null) return NotFound(new { message = error });
        return Ok(resultado);
    }
}
