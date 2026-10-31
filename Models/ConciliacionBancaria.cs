using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

/// <summary>Estado de un proceso de conciliación bancaria mensual para una cuenta de Caja y Bancos.</summary>
public enum EstadoConciliacionBancaria
{
    /// <summary>Admite reimportar el extracto y volver a correr el calce automático/manual.</summary>
    ABIERTA,
    /// <summary>Cerrada (normalmente cuando la diferencia es 0); queda de solo lectura.</summary>
    CERRADA,
}

/// <summary>Estado de calce de un movimiento del extracto bancario importado contra el Libro Caja y Bancos.</summary>
public enum EstadoMovimientoExtracto
{
    /// <summary>Aún no tiene una línea de asiento (Libro Caja y Bancos) asociada.</summary>
    PENDIENTE,
    /// <summary>Calzado automáticamente (misma fecha ±tolerancia y mismo importe) contra una línea de asiento.</summary>
    CONCILIADO_AUTOMATICO,
    /// <summary>Calzado manualmente por el usuario contra una línea de asiento.</summary>
    CONCILIADO_MANUAL,
}

/// <summary>
/// Conciliación bancaria mensual (al estilo CONCAR) de una cuenta de Caja y Bancos (PCGE clase 10,
/// normalmente una cuenta bancaria): agrupa el extracto importado del banco (<see cref="Movimientos"/>)
/// para un periodo (año/mes) y permite calcear cada línea del extracto contra una línea de asiento
/// del Libro Caja y Bancos, dejando en evidencia las partidas conciliatorias (cheques girados no
/// cobrados, depósitos en tránsito, cargos/abonos bancarios no registrados) y el cuadre entre el
/// saldo según banco y el saldo según libros.
/// </summary>
public class ConciliacionBancaria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    /// <summary>Cuenta contable de Caja y Bancos (clase 10) que se concilia.</summary>
    public Guid CuentaId { get; set; }
    public CuentaContable Cuenta { get; set; } = null!;
    /// <summary>Año del periodo conciliado.</summary>
    public int Anio { get; set; }
    /// <summary>Mes calendario (1-12) del periodo conciliado.</summary>
    public int Mes { get; set; }
    /// <summary>Saldo inicial declarado por el extracto bancario (al primer día del periodo).</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal SaldoInicialBanco { get; set; }
    /// <summary>Saldo final declarado por el extracto bancario (al último día del periodo); base del cuadre contra el saldo según libros.</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal SaldoFinalBanco { get; set; }
    [MaxLength(255)] public string? NombreArchivoImportado { get; set; }
    public DateTime? FechaImportacion { get; set; }
    public EstadoConciliacionBancaria Estado { get; set; } = EstadoConciliacionBancaria.ABIERTA;
    public DateTime? FechaCierre { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MovimientoExtractoBancario> Movimientos { get; set; } = new List<MovimientoExtractoBancario>();
}

/// <summary>
/// Línea del extracto bancario importado (un cargo o abono informado por el banco), parte de una
/// <see cref="ConciliacionBancaria"/>. Se calcea contra una línea del Libro Caja y Bancos
/// (<see cref="AsientoDetalle"/>) por fecha e importe, de forma automática o manual.
/// </summary>
public class MovimientoExtractoBancario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConciliacionId { get; set; }
    public ConciliacionBancaria Conciliacion { get; set; } = null!;
    public DateTime Fecha { get; set; }
    [MaxLength(500)] public string? Descripcion { get; set; }
    [MaxLength(100)] public string? NumeroOperacion { get; set; }
    /// <summary>Cargo informado por el banco (salida de dinero de la cuenta, equivalente a un Haber del Libro Caja y Bancos).</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal Cargo { get; set; }
    /// <summary>Abono informado por el banco (entrada de dinero a la cuenta, equivalente a un Debe del Libro Caja y Bancos).</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal Abono { get; set; }
    /// <summary>Saldo corrido informado por el propio extracto, cuando el archivo lo trae (solo referencial).</summary>
    [Column(TypeName = "decimal(12,2)")] public decimal? SaldoExtracto { get; set; }
    public EstadoMovimientoExtracto Estado { get; set; } = EstadoMovimientoExtracto.PENDIENTE;
    /// <summary>Línea del Libro Caja y Bancos (asiento confirmado) con la que se calzó este movimiento, si ya está conciliado.</summary>
    public Guid? AsientoDetalleId { get; set; }
    public AsientoDetalle? AsientoDetalle { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
