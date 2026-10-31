using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Siserp.Api.Common;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;
using Siserp.Api.Services.Ple;

namespace Siserp.Api.Controllers;

/// <summary>
/// Exportación de los Libros Electrónicos (PLE) exigidos por SUNAT, al estilo del módulo de
/// Libros Electrónicos de CONCAR: Registro de Compras (8.1), Registro de Ventas e Ingresos
/// (14.1), Libro Diario (5.1), Libro Mayor (6.1) y Libro Caja y Bancos (1.1). Cada acción
/// descarga directamente el archivo TXT con la nomenclatura y el formato oficial, listo para
/// cargarse en el validador PLE de SUNAT.
/// </summary>
[Authorize]
[MenuPermiso("exportacion-ple", NivelAcceso.LECTURA)]
[ApiController]
[Route("contabilidad/ple")]
public class PleController : ControllerBase
{
    private readonly PleExportService _pleExportService;

    public PleController(PleExportService pleExportService)
    {
        _pleExportService = pleExportService;
    }

    private static bool PeriodoInvalido(int anio, int mes) => anio < 2000 || anio > 2100 || mes < 1 || mes > 12;

    private IActionResult Descargar(PleArchivo archivo) => File(archivo.Contenido, "text/plain", archivo.NombreArchivo);

    /// <summary>8.1 Registro de Compras.</summary>
    [HttpGet("registro-compras")]
    public async Task<IActionResult> RegistroCompras([FromQuery] int anio, [FromQuery] int mes)
    {
        if (PeriodoInvalido(anio, mes)) return BadRequest(new { message = "Año/mes inválido" });
        var empresaId = User.GetEmpresaId();
        return Descargar(await _pleExportService.GenerarRegistroComprasAsync(empresaId, anio, mes));
    }

    /// <summary>14.1 Registro de Ventas e Ingresos.</summary>
    [HttpGet("registro-ventas")]
    public async Task<IActionResult> RegistroVentas([FromQuery] int anio, [FromQuery] int mes)
    {
        if (PeriodoInvalido(anio, mes)) return BadRequest(new { message = "Año/mes inválido" });
        var empresaId = User.GetEmpresaId();
        return Descargar(await _pleExportService.GenerarRegistroVentasAsync(empresaId, anio, mes));
    }

    /// <summary>5.1 Libro Diario.</summary>
    [HttpGet("libro-diario")]
    public async Task<IActionResult> LibroDiario([FromQuery] int anio, [FromQuery] int mes)
    {
        if (PeriodoInvalido(anio, mes)) return BadRequest(new { message = "Año/mes inválido" });
        var empresaId = User.GetEmpresaId();
        return Descargar(await _pleExportService.GenerarLibroDiarioAsync(empresaId, anio, mes));
    }

    /// <summary>6.1 Libro Mayor.</summary>
    [HttpGet("libro-mayor")]
    public async Task<IActionResult> LibroMayor([FromQuery] int anio, [FromQuery] int mes)
    {
        if (PeriodoInvalido(anio, mes)) return BadRequest(new { message = "Año/mes inválido" });
        var empresaId = User.GetEmpresaId();
        return Descargar(await _pleExportService.GenerarLibroMayorAsync(empresaId, anio, mes));
    }

    /// <summary>1.1 Libro Caja y Bancos.</summary>
    [HttpGet("libro-caja-bancos")]
    public async Task<IActionResult> LibroCajaBancos([FromQuery] int anio, [FromQuery] int mes)
    {
        if (PeriodoInvalido(anio, mes)) return BadRequest(new { message = "Año/mes inválido" });
        var empresaId = User.GetEmpresaId();
        return Descargar(await _pleExportService.GenerarLibroCajaBancosAsync(empresaId, anio, mes));
    }
}
