using System.Text;
using System.Text.Json;

namespace Siserp.Api.Services;

/// <summary>
/// Envío de mensajes de WhatsApp usando la WhatsApp Cloud API (Meta).
/// La configuración se lee de la sección "WhatsApp" en appsettings.json
/// (Token, PhoneNumberId). Si no está configurada, lanza
/// <see cref="InvalidOperationException"/> para que el llamador lo registre
/// como error sin detener el resto de la campaña.
/// </summary>
public class WhatsAppService
{
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public WhatsAppService(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
    }

    public async Task SendAsync(string toPhoneNumber, string message)
    {
        var token = _config["WhatsApp:Token"];
        var phoneNumberId = _config["WhatsApp:PhoneNumberId"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(phoneNumberId))
            throw new InvalidOperationException("WhatsApp Cloud API no configurada (sección WhatsApp en appsettings.json)");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var payload = new
        {
            messaging_product = "whatsapp",
            to = toPhoneNumber,
            type = "text",
            text = new { body = message },
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"https://graph.facebook.com/v20.0/{phoneNumberId}/messages", content);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Error al enviar WhatsApp: {response.StatusCode} - {error}");
        }
    }
}
