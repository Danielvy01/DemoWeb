using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Dtos;
using Siserp.Api.Models;
using Siserp.Api.Services;

namespace Siserp.Api.Controllers;

/// <summary>
/// Suscripción de dispositivos/navegadores a notificaciones push (Web Push / VAPID),
/// usadas principalmente para alertar cuando el stock de un producto cae por
/// debajo de su umbral mínimo.
/// </summary>
[Authorize]
[ApiController]
[Route("notificaciones")]
public class NotificacionesController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly WebPushNotificationService _webPush;

    public NotificacionesController(SiserpDbContext db, WebPushNotificationService webPush)
    {
        _db = db;
        _webPush = webPush;
    }

    /// <summary>Clave pública VAPID que el frontend necesita para suscribirse al Push API del navegador.</summary>
    [HttpGet("vapid-public-key")]
    [AllowAnonymous]
    public IActionResult VapidPublicKey() => Ok(new { publicKey = _webPush.PublicKey });

    [HttpPost("suscribir")]
    public async Task<IActionResult> Suscribir(SuscripcionPushDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var existente = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == dto.Endpoint);
        if (existente is not null)
        {
            existente.P256dh = dto.P256dh;
            existente.Auth = dto.Auth;
            existente.EmpresaId = empresaId;
            existente.UserAgent = dto.UserAgent;
        }
        else
        {
            _db.PushSubscriptions.Add(new PushSubscription
            {
                EmpresaId = empresaId,
                Endpoint = dto.Endpoint,
                P256dh = dto.P256dh,
                Auth = dto.Auth,
                UserAgent = dto.UserAgent,
            });
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = "Suscripción registrada" });
    }

    [HttpDelete("suscribir")]
    public async Task<IActionResult> Desuscribir([FromQuery] string endpoint)
    {
        var sub = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
        if (sub is not null)
        {
            _db.PushSubscriptions.Remove(sub);
            await _db.SaveChangesAsync();
        }
        return Ok(new { message = "Suscripción eliminada" });
    }

    /// <summary>Envía una notificación de prueba a todos los dispositivos suscritos de la empresa.</summary>
    [HttpPost("probar")]
    public async Task<IActionResult> Probar()
    {
        var empresaId = User.GetEmpresaId();
        await _webPush.EnviarATodaLaEmpresaAsync(empresaId, "SISERP", "Notificación de prueba de alertas de stock.");
        return Ok(new { message = "Notificación de prueba enviada" });
    }
}
