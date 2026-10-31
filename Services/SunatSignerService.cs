using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace Siserp.Api.Services;

/// <summary>
/// Firma digitalmente el XML del comprobante usando el certificado digital
/// (.pfx/.p12) configurado para la empresa. SUNAT exige que todo comprobante
/// electronico este firmado con un certificado digital vigente antes de ser
/// enviado (excepto en modo LOCAL, donde el comprobante se guarda sin firmar
/// para uso interno/pre-contabilizacion).
/// </summary>
public class SunatSignerService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SunatSignerService> _logger;

    public SunatSignerService(IConfiguration config, ILogger<SunatSignerService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public bool IsCertificadoConfigurado()
    {
        var certPath = _config["Sunat:CertPath"];
        return !string.IsNullOrEmpty(certPath) && File.Exists(certPath);
    }

    /// <summary>
    /// Firma el XML. Si no hay certificado configurado (modo LOCAL / desarrollo)
    /// retorna el XML sin firmar para no bloquear el flujo local del ERP.
    /// </summary>
    public (string XmlFirmado, bool Firmado) SignXml(string xml)
    {
        if (!IsCertificadoConfigurado())
        {
            _logger.LogWarning("No hay certificado digital configurado (Sunat:CertPath). Se continua en modo local sin firma.");
            return (xml, false);
        }

        try
        {
            var certPath = _config["Sunat:CertPath"]!;
            var certPassword = _config["Sunat:CertPassword"] ?? string.Empty;
            var cert = new X509Certificate2(certPath, certPassword);

            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(xml);

            var signedXml = new SignedXml(doc) { SigningKey = cert.GetRSAPrivateKey() };
            var reference = new Reference { Uri = "" };
            reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            signedXml.AddReference(reference);
            signedXml.ComputeSignature();

            doc.DocumentElement!.AppendChild(doc.ImportNode(signedXml.GetXml(), true));

            return (doc.OuterXml, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al firmar el XML, se continua sin firma");
            return (xml, false);
        }
    }
}
