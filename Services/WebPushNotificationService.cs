using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using log4net;
using WebPushSubscription = WebPush.PushSubscription;

namespace Siserp.Api.Services;

/// <summary>
/// Envía notificaciones push (Web Push / VAPID) a los navegadores/dispositivos
/// suscritos de una empresa. Usado para alertar cuando el stock de un producto
/// cae por debajo del umbral mínimo configurado.
/// </summary>
public class WebPushNotificationService
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(WebPushNotificationService));

    private readonly SiserpDbContext _db;
    private readonly IConfiguration _config;

    public WebPushNotificationService(SiserpDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public string? PublicKey => _config["WebPush:PublicKey"];

    public async Task EnviarATodaLaEmpresaAsync(Guid empresaId, string titulo, string cuerpo, object? data = null)
    {
        var publicKey = _config["WebPush:PublicKey"];
        var privateKey = _config["WebPush:PrivateKey"];
        var subject = _config["WebPush:Subject"] ?? "mailto:soporte@siserp.local";
        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
        {
            Log.Warn("WebPush no configurado (faltan VAPID keys); se omite el envio de notificaciones push.");
            return;
        }

        var suscripciones = await _db.PushSubscriptions.Where(s => s.EmpresaId == empresaId).ToListAsync();
        if (suscripciones.Count == 0) return;

        var vapidDetails = new WebPush.VapidDetails(subject, publicKey, privateKey);
        var webPushClient = new WebPush.WebPushClient();
        var payload = System.Text.Json.JsonSerializer.Serialize(new { title = titulo, body = cuerpo, data });

        foreach (var s in suscripciones)
        {
            try
            {
                var pushSubscription = new WebPushSubscription(s.Endpoint, s.P256dh, s.Auth);
                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }
            catch (WebPush.WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // La suscripción ya no es válida (el usuario la revocó o expiró): se elimina.
                _db.PushSubscriptions.Remove(s);
            }
            catch (Exception ex)
            {
                Log.Error($"Error enviando push a {s.Endpoint}: {ex.Message}");
            }
        }
        await _db.SaveChangesAsync();
    }
}
