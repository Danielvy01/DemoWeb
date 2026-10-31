using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Services;

namespace Siserp.Api.Controllers;

[Authorize]
[MenuPermiso("sunat")]
[ApiController]
[Route("sunat")]
public class SunatController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly SunatQueueProcessor _queueProcessor;

    public SunatController(SiserpDbContext db, SunatQueueProcessor queueProcessor)
    {
        _db = db;
        _queueProcessor = queueProcessor;
    }

    /// <summary>Estado de la cola de sincronizacion de la empresa actual.</summary>
    [HttpGet("cola")]
    public async Task<IActionResult> Cola()
    {
        var empresaId = User.GetEmpresaId();
        var cola = await _db.SunatSyncQueue
            .Where(i => i.Comprobante.EmpresaId == empresaId)
            .Include(i => i.Comprobante)
            .OrderByDescending(i => i.CreatedAt)
            .Take(50)
            .ToListAsync();
        return Ok(cola);
    }

    /// <summary>Fuerza el procesamiento inmediato de un item de la cola (util para pruebas/soporte).</summary>
    [HttpPost("cola/{id:guid}/procesar")]
    public async Task<IActionResult> ProcesarAhora(Guid id)
    {
        await _queueProcessor.ProcesarItemAsync(id);
        return Ok(new { ok = true });
    }
}
