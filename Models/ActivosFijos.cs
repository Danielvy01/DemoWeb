using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

/// <summary>
/// Activo fijo (inmueble, maquinaria, equipo, mobiliario, equipo de cómputo, etc.) registrado
/// en el Libro de Activos Fijos. La depreciación mensual se calcula y registra mediante
/// <see cref="DepreciacionActivoFijo"/> (ver <c>Services/ActivosFijosService.cs</c>), que a su
/// vez genera el asiento contable correspondiente (Debe: gasto por depreciación, Haber:
/// depreciación acumulada), igual que el módulo de activos fijos de CONCAR.
/// </summary>
public class ActivoFijo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(50)] public string Codigo { get; set; } = null!;
    [MaxLength(255)] public string Nombre { get; set; } = null!;
    [MaxLength(1000)] public string? Descripcion { get; set; }
    /// <summary>Clase/categoría del activo (p. ej. "Equipos de cómputo", "Muebles y enseres", "Maquinaria"); libre, de referencia.</summary>
    [MaxLength(150)] public string Categoria { get; set; } = null!;
    /// <summary>Centro de costo/área dueña del activo (opcional); se propaga al asiento de depreciación.</summary>
    public Guid? CentroCostoId { get; set; }
    public CentroCosto? CentroCosto { get; set; }

    public DateTime FechaAdquisicion { get; set; }
    /// <summary>Fecha desde la que se calcula la depreciación (normalmente igual a la fecha de adquisición, o el primer día del mes siguiente).</summary>
    public DateTime FechaInicioDepreciacion { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal ValorAdquisicion { get; set; }
    /// <summary>Valor residual al final de la vida útil; se resta de la base depreciable.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal ValorResidual { get; set; } = 0;
    /// <summary>Vida útil en meses (p. ej. 60 para equipos de cómputo a 5 años, conforme tasas de la Ley del IR).</summary>
    public int VidaUtilMeses { get; set; }
    public MetodoDepreciacion MetodoDepreciacion { get; set; } = MetodoDepreciacion.LINEA_RECTA;

    /// <summary>Cuenta del activo (p. ej. 33 Inmuebles, maquinaria y equipo) donde está registrado el costo de adquisición.</summary>
    public Guid CuentaActivoId { get; set; }
    public CuentaContable CuentaActivo { get; set; } = null!;
    /// <summary>Cuenta de depreciación acumulada (p. ej. 39 Depreciación, amortización y agotamiento acumulados), que se abona cada período.</summary>
    public Guid CuentaDepreciacionAcumuladaId { get; set; }
    public CuentaContable CuentaDepreciacionAcumulada { get; set; } = null!;
    /// <summary>Cuenta de gasto por depreciación (p. ej. 68) que se carga cada período.</summary>
    public Guid CuentaGastoDepreciacionId { get; set; }
    public CuentaContable CuentaGastoDepreciacion { get; set; } = null!;

    public EstadoActivoFijo Estado { get; set; } = EstadoActivoFijo.ACTIVO;
    /// <summary>Fecha en que el activo fue dado de baja o vendido; null mientras esté activo.</summary>
    public DateTime? FechaBaja { get; set; }
    [MaxLength(500)] public string? MotivoBaja { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<DepreciacionActivoFijo> Depreciaciones { get; set; } = new List<DepreciacionActivoFijo>();

    /// <summary>Base depreciable (valor de adquisición menos valor residual); no se persiste.</summary>
    [NotMapped]
    public decimal BaseDepreciable => ValorAdquisicion - ValorResidual;
}

/// <summary>
/// Depreciación de un activo fijo correspondiente a un período mensual (formato "yyyy-MM").
/// Un activo tiene a lo más un registro por período; su existencia evita recalcular/duplicar
/// la depreciación de un mismo mes al volver a ejecutar el proceso.
/// </summary>
public class DepreciacionActivoFijo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivoFijoId { get; set; }
    public ActivoFijo ActivoFijo { get; set; } = null!;
    /// <summary>Período depreciado, formato "yyyy-MM" (p. ej. "2026-01").</summary>
    [MaxLength(7)] public string Periodo { get; set; } = null!;
    public DateTime FechaProceso { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(12,2)")] public decimal MontoDepreciacion { get; set; }
    /// <summary>Depreciación acumulada del activo hasta este período (inclusive), informativo.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal DepreciacionAcumulada { get; set; }
    /// <summary>Asiento contable generado para este lote de depreciación (puede agrupar varios activos del mismo período/cuentas).</summary>
    public Guid? AsientoContableId { get; set; }
    public AsientoContable? AsientoContable { get; set; }
}
