using System.ComponentModel.DataAnnotations;

namespace Siserp.Api.Models;

/// <summary>
/// Plantilla de un reporte financiero (Balance General, Estado de Resultados o Nota a los EEFF
/// NIIF) configurable por el usuario: módulo "NIIF / Dinámica de Cuentas". Reemplaza la lógica
/// que antes venía fija en código (agrupando únicamente por <see cref="TipoCuentaContable"/>):
/// aquí el usuario arma libremente la estructura de líneas/subtotales del reporte
/// (<see cref="ReporteFinancieroSeccion"/>) y decide qué cuentas o rangos de códigos del plan de
/// cuentas ("dinámica de cuentas") alimentan cada línea (<see cref="ReporteFinancieroCuenta"/>).
/// </summary>
public class ReporteFinancieroPlantilla
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public Empresa Empresa { get; set; } = null!;
    /// <summary>Código corto único por empresa (p. ej. "BG-NIIF", "ER-NIIF", "NOTA-01").</summary>
    [MaxLength(20)] public string Codigo { get; set; } = null!;
    [MaxLength(255)] public string Nombre { get; set; } = null!;
    public TipoReporteFinanciero Tipo { get; set; }
    [MaxLength(1000)] public string? Descripcion { get; set; }
    /// <summary>
    /// Marca la plantilla que se usa por defecto para su <see cref="Tipo"/> (p. ej. la plantilla
    /// de Balance General que se genera al entrar al reporte sin elegir una explícitamente); solo
    /// puede haber una predeterminada activa por empresa y tipo.
    /// </summary>
    public bool EsPredeterminada { get; set; }
    public int Orden { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ReporteFinancieroSeccion> Secciones { get; set; } = new List<ReporteFinancieroSeccion>();
}

/// <summary>
/// Línea/sección de una <see cref="ReporteFinancieroPlantilla"/> (p. ej. "Activo Corriente",
/// "Efectivo y Equivalentes de Efectivo", "Utilidad Bruta"). Forma un árbol vía
/// <see cref="SeccionPadreId"/>: el total de una sección es la suma de sus
/// <see cref="ReporteFinancieroCuenta"/> directas más la suma de los totales de sus
/// <see cref="Hijas"/> (cada una ponderada por su propio <see cref="Signo"/>), lo que permite
/// reproducir tanto agrupaciones simples (Activo = suma de sus rubros) como fórmulas con
/// subtotales intermedios (Utilidad Bruta = Ingresos - Costos; Utilidad Neta = Utilidad Bruta -
/// Gastos) sin necesitar código específico por cada reporte.
/// </summary>
public class ReporteFinancieroSeccion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlantillaId { get; set; }
    public ReporteFinancieroPlantilla Plantilla { get; set; } = null!;
    public Guid? SeccionPadreId { get; set; }
    public ReporteFinancieroSeccion? SeccionPadre { get; set; }
    /// <summary>Etiqueta corta de orden/numeración (p. ej. "1.1"), de uso libre para el usuario.</summary>
    [MaxLength(20)] public string Codigo { get; set; } = null!;
    [MaxLength(255)] public string Nombre { get; set; } = null!;
    public int Orden { get; set; }
    /// <summary>
    /// Multiplicador (+1/-1) con el que el total de esta sección se acumula dentro de su sección
    /// padre (o dentro del total general del reporte, si es una sección raíz); permite restar
    /// líneas, p. ej. Costos o Gastos dentro de un subtotal de utilidad, o Pasivo/Patrimonio
    /// restando contra Activo para validar el cuadre del Balance General.
    /// </summary>
    public int Signo { get; set; } = 1;
    /// <summary>Resalta la línea como subtotal/total destacado en la presentación del reporte.</summary>
    public bool EsTotalDestacado { get; set; }
    /// <summary>
    /// Si está activo, el total de esta sección no se calcula sumando cuentas/subsecciones sino
    /// como el Resultado del Ejercicio acumulado (Ingresos - Costos - Gastos) hasta la fecha de
    /// corte del reporte; se usa típicamente para insertar la utilidad/pérdida del periodo dentro
    /// del Patrimonio de un Balance General, igual que hacía antes el código fijo.
    /// </summary>
    public bool EsResultadoDelEjercicio { get; set; }
    /// <summary>Número/etiqueta de la nota a los EEFF que detalla esta línea (p. ej. "5"), solo informativo.</summary>
    [MaxLength(20)] public string? ReferenciaNota { get; set; }
    /// <summary>Plantilla de tipo <see cref="TipoReporteFinanciero.NOTA_EEFF"/> que desarrolla el detalle de esta línea, para poder "entrar" a la nota desde el reporte principal.</summary>
    public Guid? PlantillaNotaVinculadaId { get; set; }
    public ReporteFinancieroPlantilla? PlantillaNotaVinculada { get; set; }

    public ICollection<ReporteFinancieroSeccion> Hijas { get; set; } = new List<ReporteFinancieroSeccion>();
    public ICollection<ReporteFinancieroCuenta> Cuentas { get; set; } = new List<ReporteFinancieroCuenta>();
}

/// <summary>
/// Mapeo de una cuenta (o un rango de cuentas por prefijo de código) hacia una
/// <see cref="ReporteFinancieroSeccion"/>: esta es la "dinámica de cuentas" propiamente dicha,
/// equivalente a la dinámica de cuentas de CONCAR, que define para cada línea del reporte qué
/// cuentas del plan de cuentas (PCGE u otro) la componen.
/// </summary>
public class ReporteFinancieroCuenta
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SeccionId { get; set; }
    public ReporteFinancieroSeccion Seccion { get; set; } = null!;
    /// <summary>Cuenta específica mapeada a esta sección; null cuando se usa <see cref="CodigoPrefijo"/> para mapear un rango completo.</summary>
    public Guid? CuentaId { get; set; }
    public CuentaContable? Cuenta { get; set; }
    /// <summary>
    /// Prefijo de código de cuenta (p. ej. "10", "701", "6911") que agrupa en esta sección todas
    /// las cuentas del plan de cuentas cuyo código empiece con él; se usa cuando
    /// <see cref="CuentaId"/> es null.
    /// </summary>
    [MaxLength(20)] public string? CodigoPrefijo { get; set; }
    /// <summary>Multiplicador (+1/-1) aplicado al saldo de la cuenta (ya normalizado según su naturaleza deudora/acreedora) al sumarlo en esta sección.</summary>
    public int Signo { get; set; } = 1;
}
