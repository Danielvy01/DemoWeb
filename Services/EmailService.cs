using System.Net;
using System.Net.Mail;

namespace Siserp.Api.Services;

/// <summary>
/// Envío de correo electrónico vía SMTP. La configuración se lee de la
/// sección "Smtp" en appsettings.json; si no está configurada, el envío
/// lanza <see cref="InvalidOperationException"/> para que el llamador lo
/// registre como error sin detener el resto de la campaña.
/// </summary>
public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public Task SendAsync(string to, string subject, string body) =>
        SendAsync(to, subject, body, null, null);

    /// <summary>Envía un correo, opcionalmente con un archivo adjunto (p. ej. el PDF de un documento).</summary>
    public async Task SendAsync(string to, string subject, string body, byte[]? adjunto, string? nombreAdjunto)
    {
        var host = _config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("SMTP no configurado (sección Smtp en appsettings.json)");

        var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 587;
        var user = _config["Smtp:User"];
        var password = _config["Smtp:Password"];
        var from = _config["Smtp:From"] ?? user ?? "no-reply@siserp.local";
        var enableSsl = !bool.TryParse(_config["Smtp:EnableSsl"], out var ssl) || ssl;

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = string.IsNullOrWhiteSpace(user) ? null : new NetworkCredential(user, password),
        };
        using var message = new MailMessage(from, to, subject, body) { IsBodyHtml = true };

        MemoryStream? stream = null;
        if (adjunto is not null && adjunto.Length > 0)
        {
            stream = new MemoryStream(adjunto);
            message.Attachments.Add(new Attachment(stream, nombreAdjunto ?? "documento.pdf", "application/pdf"));
        }

        try
        {
            await client.SendMailAsync(message);
        }
        finally
        {
            stream?.Dispose();
        }
    }
}
