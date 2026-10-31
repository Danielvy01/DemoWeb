using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

/// <summary>Campaña de mensajes masivos (WhatsApp y/o correo electrónico) hacia clientes.</summary>
public class CampanaMarketing
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(255)] public string Nombre { get; set; } = null!;
    public CanalCampana Canal { get; set; } = CanalCampana.EMAIL;
    [MaxLength(255)] public string? Asunto { get; set; }
    public string Mensaje { get; set; } = null!;
    public EstadoCampana Estado { get; set; } = EstadoCampana.BORRADOR;
    public DateTime? FechaProgramada { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MensajeCampana> Mensajes { get; set; } = new List<MensajeCampana>();
}

/// <summary>Envío individual de una campaña a un cliente concreto.</summary>
public class MensajeCampana
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CampanaId { get; set; }
    public CampanaMarketing Campana { get; set; } = null!;
    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    [MaxLength(255)] public string Destino { get; set; } = null!; // email o teléfono
    public EstadoMensajeCampana Estado { get; set; } = EstadoMensajeCampana.PENDIENTE;
    public string? Error { get; set; }
    public DateTime? EnviadoAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
