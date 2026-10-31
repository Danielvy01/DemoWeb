using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Siserp.Api.Models;

/// <summary>Área/centro de costo de la empresa (p. ej. Operaciones, Ventas, Sistemas) al que pertenece el personal para efectos de planilla y control de gastos.</summary>
public class CentroCosto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(150)] public string Nombre { get; set; } = null!;
    [MaxLength(500)] public string? Descripcion { get; set; }
    /// <summary>Baja lógica; se conserva el histórico del personal ya asociado.</summary>
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
}

/// <summary>Trabajador registrado, asociado a un centro de costo y con su salario base para planillas.</summary>
public class Empleado
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(20)] public string NumeroDocumento { get; set; } = null!;
    [MaxLength(255)] public string Nombres { get; set; } = null!;
    [MaxLength(255)] public string Apellidos { get; set; } = null!;
    /// <summary>Centro de costo/área (p. ej. Operaciones, Ventas, Sistemas) al que pertenece el trabajador.</summary>
    public Guid CentroCostoId { get; set; }
    public CentroCosto CentroCosto { get; set; } = null!;
    [MaxLength(100)] public string Cargo { get; set; } = null!;
    /// <summary>Marca al trabajador con el rol de Comprador: habilita su selección como responsable en las Órdenes de Compra.</summary>
    public bool EsComprador { get; set; } = false;
    [Column(TypeName = "decimal(12,2)")] public decimal SalarioBase { get; set; }
    [MaxLength(255)] public string? Email { get; set; }
    [MaxLength(30)] public string? Telefono { get; set; }
    [MaxLength(20)] public string? CuentaBancaria { get; set; }
    [MaxLength(50)] public string? BancoNombre { get; set; }
    [MaxLength(20)] public string? CCI { get; set; }
    [MaxLength(30)] public string? TipoContrato { get; set; } // PLANILLA, RECIBOS_HONORARIOS, LOCACION_SERVICIOS
    [MaxLength(10)] public string? RegimenPensionario { get; set; } // ONP, AFP
    public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;
    public bool Activo { get; set; } = true;
    /// <summary>Fecha de baja/cese del trabajador (null mientras siga activo).</summary>
    public DateTime? FechaCese { get; set; }
    [MaxLength(255)] public string? MotivoCese { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PlanillaDetalle> DetallesPlanilla { get; set; } = new List<PlanillaDetalle>();
    /// <summary>Cargos adicionales que desempeña el trabajador, además del cargo principal (campo <see cref="Cargo"/>).</summary>
    public ICollection<EmpleadoCargo> Cargos { get; set; } = new List<EmpleadoCargo>();
}

/// <summary>Cargo asignado a un trabajador; permite que una misma persona ocupe varios cargos simultáneamente (uno marcado como principal).</summary>
public class EmpleadoCargo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;
    [MaxLength(100)] public string Cargo { get; set; } = null!;
    /// <summary>Indica si es el cargo principal (se mantiene sincronizado con <see cref="Models.Empleado.Cargo"/>).</summary>
    public bool EsPrincipal { get; set; } = false;
    /// <summary>Baja lógica del cargo; se conserva el histórico.</summary>
    public bool Activo { get; set; } = true;
    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Planilla mensual: agrupa el pago de todos los empleados de un periodo (AAAA-MM).</summary>
public class Planilla
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    [MaxLength(7)] public string Periodo { get; set; } = null!; // "2026-09"
    [Column(TypeName = "decimal(12,2)")] public decimal TotalBruto { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal TotalDescuentos { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal TotalNeto { get; set; }
    public EstadoPlanilla Estado { get; set; } = EstadoPlanilla.BORRADOR;
    public DateTime? FechaSincronizacion { get; set; }
    [MaxLength(255)] public string? ReferenciaSincronizacion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Asiento de provisión de planilla (Debe: gasto de personal por centro de costo; Haber:
    /// remuneraciones por pagar + tributos/retenciones por pagar), generado automáticamente al
    /// <c>Aprobar</c> la planilla, al estilo CONCAR.
    /// </summary>
    public Guid? AsientoProvisionId { get; set; }
    public AsientoContable? AsientoProvision { get; set; }
    /// <summary>
    /// Asiento del pago efectivo de la planilla (Debe: remuneraciones por pagar; Haber: Caja y
    /// bancos), generado automáticamente al <c>ConfirmarPago</c>.
    /// </summary>
    public Guid? AsientoPagoId { get; set; }
    public AsientoContable? AsientoPago { get; set; }

    public ICollection<PlanillaDetalle> Detalles { get; set; } = new List<PlanillaDetalle>();
}

/// <summary>Línea de pago de un empleado dentro de una planilla, con su estado de pago automático.</summary>
public class PlanillaDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlanillaId { get; set; }
    public Planilla Planilla { get; set; } = null!;
    public Guid EmpleadoId { get; set; }
    public Empleado Empleado { get; set; } = null!;
    [Column(TypeName = "decimal(12,2)")] public decimal SueldoBase { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Bonificaciones { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Descuentos { get; set; }
    [Column(TypeName = "decimal(12,2)")] public decimal Neto { get; set; }
    public EstadoPagoPlanilla EstadoPago { get; set; } = EstadoPagoPlanilla.PENDIENTE;
    [MaxLength(255)] public string? ReferenciaPago { get; set; }
    public DateTime? PagadoAt { get; set; }
}
