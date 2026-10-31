using Microsoft.EntityFrameworkCore;
using Siserp.Api.Data;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

/// <summary>
/// Worker que procesa la cola de sincronizacion SUNAT (SunatSyncQueue).
/// Permite que el ERP funcione 100% offline/local: los comprobantes se crean
/// y usan normalmente en el dia a dia, y este proceso los sincroniza con
/// SUNAT en segundo plano cuando la empresa esta en modo BETA/PRODUCCION.
/// </summary>
public class SunatQueueProcessor : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SunatQueueProcessor> _logger;

    public SunatQueueProcessor(IServiceProvider services, ILogger<SunatQueueProcessor> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                await ProcesarColaAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando la cola SUNAT");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task ProcesarColaAsync(CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SiserpDbContext>();

        var pendientes = await db.SunatSyncQueue
            .Where(i => i.Estado == EstadoCola.PENDIENTE && i.ProximoIntento <= DateTime.UtcNow)
            .OrderBy(i => i.CreatedAt)
            .Take(10)
            .Select(i => i.Id)
            .ToListAsync(ct);

        foreach (var id in pendientes)
        {
            await ProcesarItemAsync(id, ct);
        }
    }

    public async Task ProcesarItemAsync(Guid itemId, CancellationToken ct = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SiserpDbContext>();
        var ublBuilder = scope.ServiceProvider.GetRequiredService<UblBuilderService>();
        var signer = scope.ServiceProvider.GetRequiredService<SunatSignerService>();
        var sunatClient = scope.ServiceProvider.GetRequiredService<SunatClientService>();

        var item = await db.SunatSyncQueue
            .Include(i => i.Comprobante).ThenInclude(c => c.Cliente)
            .Include(i => i.Comprobante).ThenInclude(c => c.Empresa)
            .Include(i => i.Comprobante).ThenInclude(c => c.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item is null) return;

        var comprobante = item.Comprobante;

        if (comprobante.Empresa.ModoSunat == SunatModo.LOCAL)
        {
            // La empresa opera solo local: no se envia nada, se marca como completado.
            await MarcarCompletadoAsync(db, item.Id, comprobante.Id, ct);
            return;
        }

        item.Estado = EstadoCola.PROCESANDO;
        await db.SaveChangesAsync(ct);

        try
        {
            var xml = ublBuilder.BuildInvoiceXml(comprobante);
            var (xmlFirmado, firmado) = signer.SignXml(xml);

            comprobante.XmlSinFirma = xml;
            comprobante.XmlFirmado = xmlFirmado;
            comprobante.Estado = EstadoComprobante.ENVIADO;
            await db.SaveChangesAsync(ct);

            if (!firmado)
            {
                throw new InvalidOperationException("El comprobante no pudo firmarse (certificado no configurado); no se enviara a SUNAT.");
            }

            var nombreArchivo = $"{comprobante.Empresa.Ruc}-{comprobante.TipoComprobante}-{comprobante.Serie}-{comprobante.Correlativo}.xml";
            var zipBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(xmlFirmado)); // TODO: comprimir en ZIP real

            var respuesta = await sunatClient.EnviarComprobanteAsync(comprobante.Empresa.ModoSunat, nombreArchivo, zipBase64);

            comprobante.Estado = respuesta.Aceptado ? EstadoComprobante.ACEPTADO : EstadoComprobante.ERROR_ENVIO;
            comprobante.CodigoRespuestaSunat = respuesta.Codigo;
            comprobante.DescripcionRespuestaSunat = respuesta.Descripcion;
            comprobante.CdrXml = respuesta.CdrXml;
            await db.SaveChangesAsync(ct);

            if (respuesta.Aceptado)
            {
                await MarcarCompletadoAsync(db, item.Id, comprobante.Id, ct);
            }
            else
            {
                await MarcarFallidoOReintentoAsync(db, item.Id, respuesta.Descripcion, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando comprobante {ComprobanteId}", comprobante.Id);
            await MarcarFallidoOReintentoAsync(db, item.Id, ex.Message, ct);
        }
    }

    private static async Task MarcarCompletadoAsync(SiserpDbContext db, Guid itemId, Guid comprobanteId, CancellationToken ct)
    {
        var item = await db.SunatSyncQueue.FirstAsync(i => i.Id == itemId, ct);
        item.Estado = EstadoCola.COMPLETADO;
        var comprobante = await db.Comprobantes.FirstAsync(c => c.Id == comprobanteId, ct);
        comprobante.Estado = EstadoComprobante.ACEPTADO;
        await db.SaveChangesAsync(ct);
    }

    private static async Task MarcarFallidoOReintentoAsync(SiserpDbContext db, Guid itemId, string error, CancellationToken ct)
    {
        var item = await db.SunatSyncQueue.FirstAsync(i => i.Id == itemId, ct);
        var intentos = item.Intentos + 1;
        var agotoIntentos = intentos >= item.MaxIntentos;

        item.Intentos = intentos;
        item.UltimoError = error;
        item.Estado = agotoIntentos ? EstadoCola.FALLIDO : EstadoCola.PENDIENTE;
        // backoff exponencial simple: 2^intentos minutos
        item.ProximoIntento = DateTime.UtcNow.AddMinutes(Math.Pow(2, intentos));
        await db.SaveChangesAsync(ct);
    }
}
