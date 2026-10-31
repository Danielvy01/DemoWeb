using Siserp.Api.Models;

namespace Siserp.Api.Services;

public record RespuestaSunat(bool Aceptado, string Codigo, string Descripcion, string? CdrXml = null);

/// <summary>
/// Cliente encargado de enviar el comprobante firmado a SUNAT (o a un
/// OSE/PSE homologado, ej. Nubefact, Efact) mediante el servicio SOAP
/// "billService" y de interpretar la Constancia de Recepcion (CDR).
///
/// En modo LOCAL este servicio nunca es invocado: el comprobante se queda
/// disponible localmente y puede sincronizarse mas adelante cuando la
/// empresa cambie a modo BETA o PRODUCCION.
/// </summary>
public class SunatClientService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SunatClientService> _logger;

    public SunatClientService(IConfiguration config, ILogger<SunatClientService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private string? GetEndpoint(SunatModo modo) => modo switch
    {
        SunatModo.BETA => _config["Sunat:BetaEndpoint"],
        SunatModo.PRODUCCION => _config["Sunat:ProdEndpoint"],
        _ => null,
    };

    public Task<RespuestaSunat> EnviarComprobanteAsync(SunatModo modo, string nombreArchivo, string zipBase64)
    {
        var endpoint = GetEndpoint(modo);
        if (string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException($"Modo {modo} no tiene endpoint SUNAT configurado");
        }

        var solUser = _config["Sunat:SolUser"];
        var solPassword = _config["Sunat:SolPassword"];
        if (string.IsNullOrEmpty(solUser) || string.IsNullOrEmpty(solPassword))
        {
            throw new InvalidOperationException("Credenciales SOL (Sunat:SolUser/Sunat:SolPassword) no configuradas");
        }

        // TODO(integracion real): invocar el WebService billService con WS-Security
        // (usuario/clave SOL) usando un cliente SOAP y procesar el CDR (zip) devuelto.
        // Se deja como punto de extension explicito para no acoplar el ERP a
        // credenciales reales durante el desarrollo local.
        _logger.LogWarning("Envio a SUNAT ({Modo}) no implementado aun de forma real. Archivo: {Archivo}", modo, nombreArchivo);

        return Task.FromResult(new RespuestaSunat(
            Aceptado: false,
            Codigo: "NO_IMPLEMENTADO",
            Descripcion: "El envio real a SUNAT no esta implementado en este entorno. Configure credenciales SOL y certificado, e implemente la llamada SOAP en SunatClientService.EnviarComprobanteAsync()."));
    }
}
