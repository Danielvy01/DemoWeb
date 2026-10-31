using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Dtos;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;
using Siserp.Api.Services;

namespace Siserp.Api.Controllers;

/// <summary>Campañas masivas de marketing por WhatsApp y/o correo electrónico hacia clientes.</summary>
[Authorize]
[MenuPermiso("marketing")]
[ApiController]
[Route("marketing/campanas")]
public class MarketingController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly EmailService _emailService;
    private readonly WhatsAppService _whatsAppService;

    public MarketingController(SiserpDbContext db, EmailService emailService, WhatsAppService whatsAppService)
    {
        _db = db;
        _emailService = emailService;
        _whatsAppService = whatsAppService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCampanaDto dto)
    {
        var empresaId = User.GetEmpresaId();
        if (!Enum.TryParse<CanalCampana>(dto.Canal, true, out var canal))
            return BadRequest(new { message = "Canal inválido. Use EMAIL o WHATSAPP" });

        // Si no se especifican clientes, la campaña se dirige a toda la cartera con dato de contacto para el canal.
        var clientesQuery = _db.Clientes.Where(c => c.EmpresaId == empresaId);
        clientesQuery = dto.ClienteIds is { Count: > 0 }
            ? clientesQuery.Where(c => dto.ClienteIds.Contains(c.Id))
            : canal == CanalCampana.EMAIL
                ? clientesQuery.Where(c => c.Email != null)
                : clientesQuery.Where(c => c.Telefono != null);
        var clientes = await clientesQuery.ToListAsync();

        var campana = new CampanaMarketing
        {
            EmpresaId = empresaId,
            Nombre = dto.Nombre,
            Canal = canal,
            Asunto = dto.Asunto,
            Mensaje = dto.Mensaje,
            FechaProgramada = dto.FechaProgramada,
            Estado = dto.FechaProgramada.HasValue ? EstadoCampana.PROGRAMADA : EstadoCampana.BORRADOR,
            Mensajes = clientes.Select(c => new MensajeCampana
            {
                ClienteId = c.Id,
                Destino = (canal == CanalCampana.EMAIL ? c.Email : c.Telefono) ?? "",
            }).Where(m => !string.IsNullOrWhiteSpace(m.Destino)).ToList(),
        };
        _db.Campanas.Add(campana);
        await _db.SaveChangesAsync();

        return Ok(await Obtener(campana.Id, empresaId));
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        var campanas = await _db.Campanas
            .Where(c => c.EmpresaId == empresaId)
            .Include(c => c.Mensajes)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(campanas);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var campana = await Obtener(id, empresaId);
        if (campana is null) return NotFound(new { message = "Campaña no encontrada" });
        return Ok(campana);
    }

    /// <summary>Dispara el envío masivo inmediato de la campaña a todos sus destinatarios pendientes.</summary>
    [HttpPost("{id:guid}/enviar")]
    public async Task<IActionResult> Enviar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var campana = await _db.Campanas
            .Include(c => c.Mensajes)
            .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (campana is null) return NotFound(new { message = "Campaña no encontrada" });
        if (campana.Estado is EstadoCampana.COMPLETADA) return BadRequest(new { message = "La campaña ya fue enviada" });

        campana.Estado = EstadoCampana.ENVIANDO;
        await _db.SaveChangesAsync();

        var huboError = false;
        foreach (var mensaje in campana.Mensajes.Where(m => m.Estado == EstadoMensajeCampana.PENDIENTE))
        {
            try
            {
                if (campana.Canal == CanalCampana.EMAIL)
                    await _emailService.SendAsync(mensaje.Destino, campana.Asunto ?? campana.Nombre, campana.Mensaje);
                else
                    await _whatsAppService.SendAsync(mensaje.Destino, campana.Mensaje);

                mensaje.Estado = EstadoMensajeCampana.ENVIADO;
                mensaje.EnviadoAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                mensaje.Estado = EstadoMensajeCampana.FALLIDO;
                mensaje.Error = ex.Message;
                huboError = true;
            }
        }

        campana.Estado = huboError ? EstadoCampana.FALLIDA : EstadoCampana.COMPLETADA;
        campana.FechaEnvio = DateTime.UtcNow;
        campana.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await Obtener(campana.Id, empresaId));
    }

    /// <summary>Reemplaza la lista de clientes destinatarios de una campaña que aún no ha sido enviada.</summary>
    [HttpPut("{id:guid}/clientes")]
    public async Task<IActionResult> ActualizarClientes(Guid id, UpdateClientesCampanaDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var campana = await _db.Campanas
            .Include(c => c.Mensajes)
            .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (campana is null) return NotFound(new { message = "Campaña no encontrada" });
        if (campana.Estado is EstadoCampana.ENVIANDO or EstadoCampana.COMPLETADA)
            return BadRequest(new { message = "No se puede modificar los destinatarios de una campaña ya enviada" });

        var clienteIds = dto.ClienteIds.Distinct().ToList();
        var clientes = await _db.Clientes
            .Where(c => c.EmpresaId == empresaId && clienteIds.Contains(c.Id))
            .ToListAsync();

        // Quita los destinatarios que ya no están seleccionados (solo si aún no se les envió nada).
        var aQuitar = campana.Mensajes
            .Where(m => m.Estado == EstadoMensajeCampana.PENDIENTE && !clienteIds.Contains(m.ClienteId))
            .ToList();
        foreach (var mensaje in aQuitar) _db.Set<MensajeCampana>().Remove(mensaje);

        // Agrega los clientes seleccionados que todavía no tienen un mensaje en la campaña.
        var existentes = campana.Mensajes.Select(m => m.ClienteId).ToHashSet();
        foreach (var cliente in clientes.Where(c => !existentes.Contains(c.Id)))
        {
            var destino = (campana.Canal == CanalCampana.EMAIL ? cliente.Email : cliente.Telefono) ?? "";
            if (string.IsNullOrWhiteSpace(destino)) continue;
            campana.Mensajes.Add(new MensajeCampana { CampanaId = campana.Id, ClienteId = cliente.Id, Destino = destino });
        }

        campana.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(await Obtener(campana.Id, empresaId));
    }

    private async Task<CampanaMarketing?> Obtener(Guid id, Guid empresaId) =>
        await _db.Campanas
            .Where(c => c.Id == id && c.EmpresaId == empresaId)
            .Include(c => c.Mensajes).ThenInclude(m => m.Cliente)
            .FirstOrDefaultAsync();
}
