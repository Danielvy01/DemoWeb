using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;

namespace Siserp.Api.Controllers;

/// <summary>Reportes gerenciales: por ahora, el reporte de ventas por rango de fechas.</summary>
[Authorize]
[ApiController]
[Route("reportes")]
public class ReportesController : ControllerBase
{
    private readonly SiserpDbContext _db;

    public ReportesController(SiserpDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Reporte de ventas: resumen general, ventas por día y ranking de productos más
    /// vendidos dentro del rango de fechas indicado (por defecto, el mes en curso).
    /// Sólo considera comprobantes no anulados.
    /// </summary>
    [MenuPermiso("reporte-ventas", NivelAcceso.LECTURA)]
    [HttpGet("ventas")]
    public async Task<IActionResult> ReporteVentas([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var fechaInicio = desde ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var fechaFin = (hasta ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var comprobantes = await _db.Comprobantes
            .Where(c => c.EmpresaId == empresaId
                        && c.FechaEmision >= fechaInicio && c.FechaEmision <= fechaFin
                        && c.Estado != EstadoComprobante.ANULADO)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .Include(c => c.Cliente)
            .ToListAsync();

        var totalVentas = comprobantes.Sum(c => c.Total);
        var totalIgv = comprobantes.Sum(c => c.Igv);
        var totalOpGravada = comprobantes.Sum(c => c.OpGravada);
        var cantidadComprobantes = comprobantes.Count;
        var ticketPromedio = cantidadComprobantes > 0 ? totalVentas / cantidadComprobantes : 0;

        var porDia = comprobantes
            .GroupBy(c => c.FechaEmision.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                fecha = g.Key,
                cantidadComprobantes = g.Count(),
                total = Math.Round(g.Sum(c => c.Total), 2),
            })
            .ToList();

        var porProducto = comprobantes
            .SelectMany(c => c.Detalles)
            .GroupBy(d => new { d.ProductoId, d.Producto.Codigo, d.Producto.Descripcion })
            .Select(g => new
            {
                g.Key.ProductoId,
                g.Key.Codigo,
                g.Key.Descripcion,
                cantidadVendida = g.Sum(d => d.Cantidad),
                totalVendido = Math.Round(g.Sum(d => d.Total), 2),
            })
            .OrderByDescending(x => x.totalVendido)
            .Take(20)
            .ToList();

        var porCliente = comprobantes
            .GroupBy(c => new { c.ClienteId, c.Cliente.NombreORazonSocial })
            .Select(g => new
            {
                g.Key.ClienteId,
                cliente = g.Key.NombreORazonSocial,
                cantidadComprobantes = g.Count(),
                total = Math.Round(g.Sum(c => c.Total), 2),
            })
            .OrderByDescending(x => x.total)
            .Take(20)
            .ToList();

        // Estado de integración con SUNAT de los comprobantes del rango (pendientes de envío,
        // enviados, aceptados/observados/rechazados por SUNAT), útil para conciliar el Registro
        // de Ventas e Ingresos Electrónico (RVIE) exigido por SUNAT.
        var porEstadoSunat = comprobantes
            .GroupBy(c => c.Estado)
            .Select(g => new
            {
                estado = g.Key.ToString(),
                cantidad = g.Count(),
                total = Math.Round(g.Sum(c => c.Total), 2),
            })
            .OrderByDescending(x => x.cantidad)
            .ToList();

        return Ok(new
        {
            rango = new { desde = fechaInicio, hasta = fechaFin },
            resumen = new
            {
                totalVentas = Math.Round(totalVentas, 2),
                totalIgv = Math.Round(totalIgv, 2),
                totalOpGravada = Math.Round(totalOpGravada, 2),
                cantidadComprobantes,
                ticketPromedio = Math.Round(ticketPromedio, 2),
            },
            porDia,
            porProducto,
            porCliente,
            porEstadoSunat,
        });
    }

    /// <summary>
    /// Reporte de compras: resumen general, compras por día y ranking de productos e insumos
    /// más comprados dentro del rango de fechas indicado (por defecto, el mes en curso).
    /// Incluye serie/número de cada comprobante de compra para su conciliación con el
    /// Registro de Compras Electrónico (RCE) que exige SUNAT.
    /// </summary>
    [MenuPermiso("reporte-compras", NivelAcceso.LECTURA)]
    [HttpGet("compras")]
    public async Task<IActionResult> ReporteCompras([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var fechaInicio = desde ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var fechaFin = (hasta ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var compras = await _db.Compras
            .Where(c => c.EmpresaId == empresaId && c.FechaEmision >= fechaInicio && c.FechaEmision <= fechaFin)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .Include(c => c.Proveedor)
            .ToListAsync();

        var totalCompras = compras.Sum(c => c.Total);
        var cantidadCompras = compras.Count;
        var promedioCompra = cantidadCompras > 0 ? totalCompras / cantidadCompras : 0;
        var totalPendientePago = compras.Where(c => !c.Pagado).Sum(c => c.Total);

        var porDia = compras
            .GroupBy(c => c.FechaEmision.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                fecha = g.Key,
                cantidadCompras = g.Count(),
                total = Math.Round(g.Sum(c => c.Total), 2),
            })
            .ToList();

        var porProducto = compras
            .SelectMany(c => c.Detalles)
            .GroupBy(d => new { d.ProductoId, d.Producto.Codigo, d.Producto.Descripcion })
            .Select(g => new
            {
                g.Key.ProductoId,
                g.Key.Codigo,
                g.Key.Descripcion,
                cantidadComprada = g.Sum(d => d.Cantidad),
                totalComprado = Math.Round(g.Sum(d => d.Total), 2),
            })
            .OrderByDescending(x => x.totalComprado)
            .Take(20)
            .ToList();

        var porProveedor = compras
            .GroupBy(c => new { c.ProveedorId, c.Proveedor.RazonSocial })
            .Select(g => new
            {
                g.Key.ProveedorId,
                proveedor = g.Key.RazonSocial,
                cantidadCompras = g.Count(),
                total = Math.Round(g.Sum(c => c.Total), 2),
            })
            .OrderByDescending(x => x.total)
            .Take(20)
            .ToList();

        // Detalle de comprobantes de compra (serie/número/RUC proveedor, tipo de documento,
        // guía de remisión, tipo de operación y tipo de cambio) para conciliar contra el
        // Registro de Compras Electrónico (RCE) que SUNAT genera a partir de lo declarado
        // por los proveedores.
        var comprobantesRce = compras
            .OrderByDescending(c => c.FechaEmision)
            .Select(c => new
            {
                c.Id,
                c.Serie,
                c.Numero,
                tipoDocumento = c.TipoDocumento.ToString(),
                proveedor = c.Proveedor.RazonSocial,
                proveedorRuc = c.Proveedor.Ruc,
                c.FechaEmision,
                c.FechaVencimiento,
                numeroGuiaRemision = c.NumeroGuiaRemision,
                tipoOperacionSunat = c.TipoOperacionSunat.ToString(),
                c.Moneda,
                c.Total,
                c.TipoCambio,
                c.Pagado,
                tieneComprobanteFoto = c.ComprobanteFotoUrl != null,
                comprobanteFotoUrl = c.ComprobanteFotoUrl,
            })
            .ToList();

        return Ok(new
        {
            rango = new { desde = fechaInicio, hasta = fechaFin },
            resumen = new
            {
                totalCompras = Math.Round(totalCompras, 2),
                cantidadCompras,
                promedioCompra = Math.Round(promedioCompra, 2),
                totalPendientePago = Math.Round(totalPendientePago, 2),
            },
            porDia,
            porProducto,
            porProveedor,
            comprobantesRce,
        });
    }

    /// <summary>
    /// Reporte de órdenes de compra: resumen general y desglose por proveedor, por categoría
    /// de producto y por insumo/mercadería (producto), dentro del rango de fechas indicado
    /// (por defecto, el mes en curso). No incluye órdenes canceladas.
    /// </summary>
    [MenuPermiso("reporte-ordenes-compra", NivelAcceso.LECTURA)]
    [HttpGet("ordenes-compra")]
    public async Task<IActionResult> ReporteOrdenesCompra([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var fechaInicio = desde ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var fechaFin = (hasta ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var ordenes = await _db.OrdenesCompra
            .Where(o => o.EmpresaId == empresaId
                        && o.FechaEmision >= fechaInicio && o.FechaEmision <= fechaFin
                        && o.Estado != EstadoOrdenCompra.CANCELADA)
            .Include(o => o.Proveedor)
            .Include(o => o.Detalles).ThenInclude(d => d.Producto).ThenInclude(p => p.Categoria)
            .ToListAsync();

        var totalOrdenes = ordenes.Sum(o => o.Total);
        var cantidadOrdenes = ordenes.Count;
        var promedioOrden = cantidadOrdenes > 0 ? totalOrdenes / cantidadOrdenes : 0;

        var porEstado = ordenes
            .GroupBy(o => o.Estado)
            .Select(g => new
            {
                estado = g.Key.ToString(),
                cantidad = g.Count(),
                total = Math.Round(g.Sum(o => o.Total), 2),
            })
            .OrderByDescending(x => x.cantidad)
            .ToList();

        // 1. Órdenes de compra por proveedor.
        var porProveedor = ordenes
            .GroupBy(o => new { o.ProveedorId, o.Proveedor.RazonSocial })
            .Select(g => new
            {
                g.Key.ProveedorId,
                proveedor = g.Key.RazonSocial,
                cantidadOrdenes = g.Count(),
                total = Math.Round(g.Sum(o => o.Total), 2),
            })
            .OrderByDescending(x => x.total)
            .ToList();

        // 2. Órdenes de compra por categoría de producto.
        var porCategoria = ordenes
            .SelectMany(o => o.Detalles)
            .GroupBy(d => new
            {
                CategoriaId = d.Producto.CategoriaId,
                Categoria = d.Producto.Categoria != null ? d.Producto.Categoria.Nombre : "Sin categoría",
            })
            .Select(g => new
            {
                g.Key.CategoriaId,
                categoria = g.Key.Categoria,
                cantidadItems = g.Sum(d => d.Cantidad),
                total = Math.Round(g.Sum(d => d.Total), 2),
            })
            .OrderByDescending(x => x.total)
            .ToList();

        // 3. Órdenes de compra por insumo/mercadería (producto).
        var porProducto = ordenes
            .SelectMany(o => o.Detalles)
            .GroupBy(d => new { d.ProductoId, d.Producto.Codigo, d.Producto.Descripcion })
            .Select(g => new
            {
                g.Key.ProductoId,
                g.Key.Codigo,
                g.Key.Descripcion,
                cantidadSolicitada = g.Sum(d => d.Cantidad),
                totalSolicitado = Math.Round(g.Sum(d => d.Total), 2),
            })
            .OrderByDescending(x => x.totalSolicitado)
            .ToList();

        return Ok(new
        {
            rango = new { desde = fechaInicio, hasta = fechaFin },
            resumen = new
            {
                totalOrdenes = Math.Round(totalOrdenes, 2),
                cantidadOrdenes,
                promedioOrden = Math.Round(promedioOrden, 2),
            },
            porEstado,
            porProveedor,
            porCategoria,
            porProducto,
        });
    }

    /// <summary>
    /// Cuentas por cobrar: comprobantes al crédito (con días de crédito del cliente) aún no pagados,
    /// indicando si ya vencieron y hace cuántos días.
    /// </summary>
    [MenuPermiso("reporte-estado-cuenta-cliente", NivelAcceso.LECTURA)]
    [HttpGet("cuentas-por-cobrar")]
    public async Task<IActionResult> CuentasPorCobrar()
    {
        var empresaId = User.GetEmpresaId();
        var ahora = DateTime.UtcNow;
        var comprobantes = await _db.Comprobantes
            .Where(c => c.EmpresaId == empresaId
                        && c.Estado != EstadoComprobante.ANULADO
                        && !c.Pagado
                        && c.FechaVencimiento != null)
            .Include(c => c.Cliente)
            .OrderBy(c => c.FechaVencimiento)
            .Select(c => new
            {
                c.Id,
                c.Serie,
                c.Correlativo,
                cliente = c.Cliente.NombreORazonSocial,
                c.FechaEmision,
                c.FechaVencimiento,
                c.Total,
                c.Moneda,
                vencido = c.FechaVencimiento < ahora,
                diasVencido = c.FechaVencimiento < ahora ? EF.Functions.DateDiffDay(c.FechaVencimiento!.Value, ahora) : 0,
            })
            .ToListAsync();

        return Ok(new
        {
            total = Math.Round(comprobantes.Sum(c => c.Total), 2),
            cantidad = comprobantes.Count,
            comprobantes,
        });
    }

    /// <summary>
    /// Reporte de planilla de pagos (cuentas por pagar): compras al crédito aún no pagadas,
    /// filtrables por rango de fechas y por proveedor, con el importe de cada factura del
    /// registro de compras expresado tanto en soles como en dólares según el tipo de cambio
    /// vigente a la fecha de cada documento (o el tipo de cambio indicado en <paramref name="tipoCambio"/>
    /// para una conversión puntual).
    /// </summary>
    [MenuPermiso("reporte-planilla-pagos", NivelAcceso.LECTURA)]
    [HttpGet("cuentas-por-pagar")]
    public async Task<IActionResult> CuentasPorPagar(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] Guid? proveedorId, [FromQuery] decimal? tipoCambio)
    {
        var empresaId = User.GetEmpresaId();
        var ahora = DateTime.UtcNow;

        var query = _db.Compras.Where(c => c.EmpresaId == empresaId && !c.Pagado && c.FechaVencimiento != null);
        if (desde is not null) query = query.Where(c => c.FechaEmision >= desde.Value.Date);
        if (hasta is not null) query = query.Where(c => c.FechaEmision <= hasta.Value.Date.AddDays(1).AddTicks(-1));
        if (proveedorId is not null) query = query.Where(c => c.ProveedorId == proveedorId);

        var compras = await query
            .Include(c => c.Proveedor)
            .OrderBy(c => c.FechaVencimiento)
            .ToListAsync();

        // Tipo de cambio a usar por compra en USD: el indicado explícitamente en la consulta o,
        // en su defecto, el capturado al momento de registrar la compra (tipo de cambio venta del día).
        decimal? TipoCambioDe(Models.Compra c) => tipoCambio ?? c.TipoCambio;

        var detalle = compras.Select(c => new
        {
            c.Id,
            c.Serie,
            c.Numero,
            tipoDocumento = c.TipoDocumento.ToString(),
            proveedorId = c.ProveedorId,
            proveedor = c.Proveedor.RazonSocial,
            proveedorRuc = c.Proveedor.Ruc,
            c.FechaEmision,
            c.FechaVencimiento,
            c.Moneda,
            c.Total,
            tipoCambio = TipoCambioDe(c),
            totalPen = c.Moneda == "USD" ? (TipoCambioDe(c) is decimal tc ? Math.Round(c.Total * tc, 2) : (decimal?)null) : c.Total,
            totalUsd = c.Moneda == "USD" ? c.Total : (TipoCambioDe(c) is decimal tc2 && tc2 > 0 ? Math.Round(c.Total / tc2, 2) : (decimal?)null),
            vencido = c.FechaVencimiento < ahora,
            diasVencido = c.FechaVencimiento < ahora ? (int)(ahora - c.FechaVencimiento!.Value).TotalDays : 0,
        }).ToList();

        var porProveedor = compras
            .GroupBy(c => new { c.ProveedorId, c.Proveedor.RazonSocial })
            .Select(g => new
            {
                proveedorId = g.Key.ProveedorId,
                proveedor = g.Key.RazonSocial,
                cantidad = g.Count(),
                totalPen = Math.Round(g.Sum(c => c.Moneda == "USD" ? c.Total * (TipoCambioDe(c) ?? 0) : c.Total), 2),
            })
            .OrderByDescending(x => x.totalPen)
            .ToList();

        return Ok(new
        {
            rango = new { desde, hasta },
            resumen = new
            {
                cantidad = compras.Count,
                totalPen = Math.Round(detalle.Sum(d => d.totalPen ?? 0), 2),
                totalUsd = Math.Round(detalle.Sum(d => d.totalUsd ?? 0), 2),
            },
            porProveedor,
            compras = detalle,
        });
    }

    /// <summary>
    /// Reporte de cuenta corriente por proveedor: estado de cuenta cronológico (estilo libro
    /// mayor) con las compras registradas (cargo/debe) y los pagos efectuados (abono/haber) al
    /// proveedor dentro del rango de fechas indicado (por defecto, el mes en curso), partiendo
    /// del saldo arrastrado de movimientos anteriores al rango.
    /// </summary>
    [MenuPermiso("reporte-cuenta-corriente-proveedor", NivelAcceso.LECTURA)]
    [HttpGet("cuenta-corriente-proveedor")]
    public async Task<IActionResult> CuentaCorrienteProveedor(
        [FromQuery] Guid proveedorId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        if (proveedorId == Guid.Empty) return BadRequest(new { mensaje = "Debe indicar el proveedor." });

        var empresaId = User.GetEmpresaId();
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == proveedorId && p.EmpresaId == empresaId);
        if (proveedor is null) return NotFound(new { mensaje = "Proveedor no encontrado." });

        var fechaInicio = desde ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var fechaFin = (hasta ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var compras = await _db.Compras
            .Where(c => c.EmpresaId == empresaId && c.ProveedorId == proveedorId)
            .ToListAsync();

        // Saldo inicial: cargos (compras emitidas) menos abonos (pagos realizados) antes del rango.
        var cargosAntes = compras.Where(c => c.FechaEmision < fechaInicio).Sum(c => c.Total);
        var abonosAntes = compras.Where(c => c.Pagado && c.FechaPago != null && c.FechaPago < fechaInicio).Sum(c => c.Total);
        var saldoInicial = Math.Round(cargosAntes - abonosAntes, 2);

        // Movimientos dentro del rango: un cargo por cada compra emitida y un abono por cada pago realizado.
        var movimientos = compras
            .Where(c => c.FechaEmision >= fechaInicio && c.FechaEmision <= fechaFin)
            .Select(c => new
            {
                fecha = c.FechaEmision,
                tipo = "Compra",
                documento = c.Serie is not null ? $"{c.Serie}-{c.Numero}" : c.Numero,
                concepto = c.TipoDocumento.ToString(),
                debe = c.Total,
                haber = 0m,
            })
            .Concat(compras
                .Where(c => c.Pagado && c.FechaPago != null && c.FechaPago >= fechaInicio && c.FechaPago <= fechaFin)
                .Select(c => new
                {
                    fecha = c.FechaPago!.Value,
                    tipo = "Pago",
                    documento = c.Serie is not null ? $"{c.Serie}-{c.Numero}" : c.Numero,
                    concepto = "Pago a proveedor",
                    debe = 0m,
                    haber = c.Total,
                }))
            .OrderBy(m => m.fecha)
            .ToList();

        var saldo = saldoInicial;
        var detalle = new List<object>();
        foreach (var m in movimientos)
        {
            saldo += m.debe - m.haber;
            detalle.Add(new
            {
                m.fecha,
                m.tipo,
                m.documento,
                m.concepto,
                debe = Math.Round(m.debe, 2),
                haber = Math.Round(m.haber, 2),
                saldo = Math.Round(saldo, 2),
            });
        }

        return Ok(new
        {
            proveedor = new { proveedor.Id, proveedor.RazonSocial, proveedor.Ruc },
            rango = new { desde = fechaInicio, hasta = fechaFin },
            saldoInicial,
            totalCargos = Math.Round(movimientos.Sum(m => m.debe), 2),
            totalAbonos = Math.Round(movimientos.Sum(m => m.haber), 2),
            saldoFinal = Math.Round(saldo, 2),
            movimientos = detalle,
        });
    }

    /// <summary>
    /// Indicadores del panel principal (dashboard): ventas del mes en curso, stock disponible,
    /// facturas por cobrar pendientes y cantidad de clientes activos. Se agrupan en un solo
    /// endpoint para que el dashboard cargue todo con una única llamada.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var empresaId = User.GetEmpresaId();
        var ahora = DateTime.UtcNow;
        var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);

        var ventasMesQuery = _db.Comprobantes.Where(c =>
            c.EmpresaId == empresaId
            && c.Estado != EstadoComprobante.ANULADO
            && c.FechaEmision >= inicioMes && c.FechaEmision <= ahora);

        var totalVentasMes = await ventasMesQuery.SumAsync(c => (decimal?)c.Total) ?? 0;
        var cantidadVentasMes = await ventasMesQuery.CountAsync();

        var stockActual = await _db.Stocks
            .Where(s => s.Producto.EmpresaId == empresaId && s.Producto.Activo && !s.Producto.EsServicio)
            .SumAsync(s => (decimal?)s.Cantidad) ?? 0;
        var productosStockBajo = await _db.Productos
            .Where(p => p.EmpresaId == empresaId && p.Activo && !p.EsServicio)
            .Include(p => p.Stocks)
            .Where(p => p.Stocks.Sum(s => s.Cantidad) <= p.StockMinimo)
            .CountAsync();

        var porCobrarQuery = _db.Comprobantes.Where(c =>
            c.EmpresaId == empresaId && c.Estado != EstadoComprobante.ANULADO && !c.Pagado && c.FechaVencimiento != null);
        var totalPorCobrar = await porCobrarQuery.SumAsync(c => (decimal?)c.Total) ?? 0;
        var cantidadPorCobrar = await porCobrarQuery.CountAsync();
        var cantidadVencidas = await porCobrarQuery.CountAsync(c => c.FechaVencimiento < ahora);

        var cantidadClientes = await _db.Clientes.CountAsync(c => c.EmpresaId == empresaId && c.Activo);

        return Ok(new
        {
            ventasMes = new
            {
                total = Math.Round(totalVentasMes, 2),
                cantidadComprobantes = cantidadVentasMes,
                desde = inicioMes,
                hasta = ahora,
            },
            stock = new
            {
                cantidadTotal = stockActual,
                productosStockBajo,
            },
            porCobrar = new
            {
                total = Math.Round(totalPorCobrar, 2),
                cantidad = cantidadPorCobrar,
                cantidadVencidas,
            },
            clientes = new
            {
                cantidad = cantidadClientes,
            },
        });
    }

    /// <summary>Proyección común usada en los reportes de comprobantes (vigentes/anulados) del listado de ventas.</summary>
    private static IQueryable<object> ProyeccionComprobantes(IQueryable<Comprobante> query) =>
        query
            .OrderByDescending(c => c.FechaEmision)
            .Select(c => new
            {
                c.Id,
                tipoComprobante = c.TipoComprobante.ToString(),
                c.Serie,
                c.Correlativo,
                c.FechaEmision,
                cliente = c.Cliente.NombreORazonSocial,
                clienteDocumento = c.Cliente.NumeroDocumento,
                almacen = c.Almacen != null ? c.Almacen.Nombre : null,
                vendedor = c.Vendedor != null ? c.Vendedor.Nombre : null,
                canalVenta = c.CanalVenta.ToString(),
                c.Moneda,
                c.Total,
                estado = c.Estado.ToString(),
                c.Observaciones,
            });

    /// <summary>
    /// 1. Reporte de comprobantes vigentes (no anulados): permite ubicar cualquier comprobante emitido
    /// dentro del rango de fechas indicado, para su reimpresión (A4/ticket 80mm, ver GET ventas/{id}/pdf)
    /// o exportación a Excel.
    /// </summary>
    [MenuPermiso("reporte-comprobantes-vigentes", NivelAcceso.LECTURA)]
    [HttpGet("comprobantes-vigentes")]
    public async Task<IActionResult> ComprobantesVigentes(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] Guid? almacenId,
        [FromQuery] string? tipoComprobante, [FromQuery] string? buscar)
    {
        var empresaId = User.GetEmpresaId();
        var fechaInicio = desde ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var fechaFin = (hasta ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var query = _db.Comprobantes
            .Where(c => c.EmpresaId == empresaId && c.Estado != EstadoComprobante.ANULADO
                        && c.FechaEmision >= fechaInicio && c.FechaEmision <= fechaFin)
            .Include(c => c.Cliente).Include(c => c.Almacen).Include(c => c.Vendedor)
            .AsQueryable();

        if (almacenId.HasValue) query = query.Where(c => c.AlmacenId == almacenId);
        if (!string.IsNullOrWhiteSpace(tipoComprobante) && Enum.TryParse<TipoComprobante>(tipoComprobante, true, out var tipo))
            query = query.Where(c => c.TipoComprobante == tipo);
        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(c => c.Cliente.NombreORazonSocial.Contains(buscar) || c.Serie.Contains(buscar));

        var totalVigente = await query.SumAsync(c => c.Total);
        var comprobantes = await ProyeccionComprobantes(query).ToListAsync();
        return Ok(new
        {
            rango = new { desde = fechaInicio, hasta = fechaFin },
            cantidad = comprobantes.Count,
            total = Math.Round(totalVigente, 2),
            comprobantes,
        });
    }

    /// <summary>
    /// 2. Reporte de comprobantes anulados: mismo detalle que el reporte de vigentes, filtrado
    /// únicamente a los comprobantes dados de baja (Estado = ANULADO), para su reimpresión o auditoría.
    /// </summary>
    [MenuPermiso("reporte-comprobantes-anulados", NivelAcceso.LECTURA)]
    [HttpGet("comprobantes-anulados")]
    public async Task<IActionResult> ComprobantesAnulados(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] Guid? almacenId,
        [FromQuery] string? tipoComprobante, [FromQuery] string? buscar)
    {
        var empresaId = User.GetEmpresaId();
        var fechaInicio = desde ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var fechaFin = (hasta ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var query = _db.Comprobantes
            .Where(c => c.EmpresaId == empresaId && c.Estado == EstadoComprobante.ANULADO
                        && c.FechaEmision >= fechaInicio && c.FechaEmision <= fechaFin)
            .Include(c => c.Cliente).Include(c => c.Almacen).Include(c => c.Vendedor)
            .AsQueryable();

        if (almacenId.HasValue) query = query.Where(c => c.AlmacenId == almacenId);
        if (!string.IsNullOrWhiteSpace(tipoComprobante) && Enum.TryParse<TipoComprobante>(tipoComprobante, true, out var tipo))
            query = query.Where(c => c.TipoComprobante == tipo);
        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(c => c.Cliente.NombreORazonSocial.Contains(buscar) || c.Serie.Contains(buscar));

        var totalAnulado = await query.SumAsync(c => c.Total);
        var comprobantes = await ProyeccionComprobantes(query).ToListAsync();
        return Ok(new
        {
            rango = new { desde = fechaInicio, hasta = fechaFin },
            cantidad = comprobantes.Count,
            total = Math.Round(totalAnulado, 2),
            comprobantes,
        });
    }

    /// <summary>
    /// 3. Estado de cuenta del cliente: comprobantes al crédito del cliente en el rango de fechas,
    /// indicando si están Pendiente o Cobrado, con el detalle de la venta (ítems) y de los pagos
    /// registrados (para las ventas al contado con varios métodos de pago).
    /// </summary>
    [MenuPermiso("reporte-estado-cuenta-cliente", NivelAcceso.LECTURA)]
    [HttpGet("estado-cuenta-cliente")]
    public async Task<IActionResult> EstadoCuentaCliente(
        [FromQuery] Guid clienteId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId && c.EmpresaId == empresaId);
        if (cliente is null) return NotFound(new { message = "Cliente no encontrado" });

        var fechaInicio = desde ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var fechaFin = (hasta ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var comprobantes = await _db.Comprobantes
            .Where(c => c.EmpresaId == empresaId && c.ClienteId == clienteId && c.Estado != EstadoComprobante.ANULADO
                        && c.FechaEmision >= fechaInicio && c.FechaEmision <= fechaFin)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .Include(c => c.Pagos)
            .OrderBy(c => c.FechaEmision)
            .ToListAsync();

        var detalle = comprobantes.Select(c => new
        {
            c.Id,
            tipoComprobante = c.TipoComprobante.ToString(),
            c.Serie,
            c.Correlativo,
            c.FechaEmision,
            condicionPago = c.CondicionPago.ToString(),
            c.FechaVencimiento,
            c.Moneda,
            c.Total,
            estadoCobro = c.CondicionPago == CondicionPago.CONTADO || c.Pagado ? "COBRADO" : "PENDIENTE",
            c.Pagado,
            c.FechaPago,
            items = c.Detalles.Select(d => new
            {
                producto = d.Producto.Descripcion,
                d.Cantidad,
                d.PrecioUnitario,
                d.Total,
            }),
            pagos = c.Pagos.Select(p => new { metodoPago = p.MetodoPago.ToString(), p.Monto, p.Referencia }),
        }).ToList();

        var totalVentas = comprobantes.Sum(c => c.Total);
        var totalPendiente = comprobantes.Where(c => c.CondicionPago == CondicionPago.CREDITO && !c.Pagado).Sum(c => c.Total);
        var totalCobrado = totalVentas - totalPendiente;

        return Ok(new
        {
            cliente = new { cliente.Id, cliente.NombreORazonSocial, cliente.NumeroDocumento, cliente.DiasCredito },
            rango = new { desde = fechaInicio, hasta = fechaFin },
            resumen = new
            {
                cantidadComprobantes = comprobantes.Count,
                totalVentas = Math.Round(totalVentas, 2),
                totalPendiente = Math.Round(totalPendiente, 2),
                totalCobrado = Math.Round(totalCobrado, 2),
            },
            comprobantes = detalle,
        });
    }

    /// <summary>
    /// 4. Reporte de ventas detalladas: línea por línea (producto vendido) de todos los
    /// comprobantes no anulados de un vendedor dentro de un rango de fechas.
    /// </summary>
    [MenuPermiso("reporte-ventas-detalladas", NivelAcceso.LECTURA)]
    [HttpGet("ventas-detalladas")]
    public async Task<IActionResult> VentasDetalladas(
        [FromQuery] Guid? vendedorId, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var fechaInicio = desde ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var fechaFin = (hasta ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var query = _db.Comprobantes
            .Where(c => c.EmpresaId == empresaId && c.Estado != EstadoComprobante.ANULADO
                        && c.FechaEmision >= fechaInicio && c.FechaEmision <= fechaFin)
            .Include(c => c.Cliente).Include(c => c.Vendedor).Include(c => c.Almacen)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .AsQueryable();
        if (vendedorId.HasValue) query = query.Where(c => c.VendedorId == vendedorId);

        var comprobantes = await query.OrderBy(c => c.FechaEmision).ToListAsync();

        var detalle = comprobantes
            .SelectMany(c => c.Detalles.Select(d => new
            {
                c.Id,
                tipoComprobante = c.TipoComprobante.ToString(),
                c.Serie,
                c.Correlativo,
                c.FechaEmision,
                vendedor = c.Vendedor != null ? c.Vendedor.Nombre : "Sin vendedor",
                almacen = c.Almacen != null ? c.Almacen.Nombre : null,
                cliente = c.Cliente.NombreORazonSocial,
                producto = d.Producto.Descripcion,
                codigoProducto = d.Producto.Codigo,
                d.Cantidad,
                d.PrecioUnitario,
                d.Total,
            }))
            .ToList();

        var porVendedor = comprobantes
            .GroupBy(c => new { c.VendedorId, Nombre = c.Vendedor != null ? c.Vendedor.Nombre : "Sin vendedor" })
            .Select(g => new
            {
                vendedorId = g.Key.VendedorId,
                vendedor = g.Key.Nombre,
                cantidadComprobantes = g.Count(),
                totalVendido = Math.Round(g.Sum(c => c.Total), 2),
            })
            .OrderByDescending(x => x.totalVendido)
            .ToList();

        return Ok(new
        {
            rango = new { desde = fechaInicio, hasta = fechaFin },
            resumen = new
            {
                cantidadComprobantes = comprobantes.Count,
                totalVendido = Math.Round(comprobantes.Sum(c => c.Total), 2),
            },
            porVendedor,
            detalle,
        });
    }

    /// <summary>
    /// 5. Registro de ventas (formato exigido por SUNAT para el Registro de Ventas e Ingresos):
    /// se filtra por año, mes y local (almacén); opcionalmente incluye la Nota de venta (documento
    /// de control interno que normalmente no se declara a SUNAT) cuando <paramref name="incluirNotaVenta"/> es true.
    /// </summary>
    [MenuPermiso("reporte-registro-ventas", NivelAcceso.LECTURA)]
    [HttpGet("registro-ventas")]
    public async Task<IActionResult> RegistroVentas(
        [FromQuery] int anio, [FromQuery] int mes, [FromQuery] Guid? almacenId, [FromQuery] bool incluirNotaVenta = false)
    {
        if (anio < 2000 || mes < 1 || mes > 12) return BadRequest(new { message = "Año/mes inválido" });
        var empresaId = User.GetEmpresaId();
        var fechaInicio = new DateTime(anio, mes, 1);
        var fechaFin = fechaInicio.AddMonths(1).AddTicks(-1);

        var query = _db.Comprobantes
            .Where(c => c.EmpresaId == empresaId && c.FechaEmision >= fechaInicio && c.FechaEmision <= fechaFin)
            .Include(c => c.Cliente)
            .AsQueryable();

        if (almacenId.HasValue) query = query.Where(c => c.AlmacenId == almacenId);
        if (!incluirNotaVenta) query = query.Where(c => c.TipoComprobante != TipoComprobante.NOTA_VENTA);

        var comprobantes = await query.OrderBy(c => c.FechaEmision).ThenBy(c => c.Serie).ThenBy(c => c.Correlativo).ToListAsync();

        // Columnas equivalentes a las exigidas por SUNAT en el Registro de Ventas e Ingresos (RVIE):
        // periodo, fecha de emisión, tipo/serie/número de comprobante, datos del cliente, base
        // imponible desagregada, IGV y estado del comprobante.
        var detalle = comprobantes.Select(c => new
        {
            c.Id,
            periodo = $"{anio:D4}-{mes:D2}",
            c.FechaEmision,
            tipoComprobante = c.TipoComprobante.ToString(),
            c.Serie,
            c.Correlativo,
            tipoDocumentoCliente = c.Cliente.TipoDocumento.ToString(),
            numeroDocumentoCliente = c.Cliente.NumeroDocumento,
            cliente = c.Cliente.NombreORazonSocial,
            c.OpGravada,
            c.OpExonerada,
            c.OpInafecta,
            c.Igv,
            c.Total,
            c.Moneda,
            estado = c.Estado.ToString(),
        }).ToList();

        return Ok(new
        {
            periodo = new { anio, mes },
            incluirNotaVenta,
            cantidad = detalle.Count,
            totales = new
            {
                opGravada = Math.Round(comprobantes.Sum(c => c.OpGravada), 2),
                opExonerada = Math.Round(comprobantes.Sum(c => c.OpExonerada), 2),
                opInafecta = Math.Round(comprobantes.Sum(c => c.OpInafecta), 2),
                igv = Math.Round(comprobantes.Sum(c => c.Igv), 2),
                total = Math.Round(comprobantes.Sum(c => c.Total), 2),
            },
            comprobantes = detalle,
        });
    }

    /// <summary>
    /// 6. Canal de ventas: se filtra por local (almacén), canal de venta (LOCAL/WEB/CORPORATIVO/
    /// PEDIDOS_YA/RAPPI) y rango de fechas; agrupa el total vendido por canal.
    /// </summary>
    [MenuPermiso("reporte-canal-ventas", NivelAcceso.LECTURA)]
    [HttpGet("canal-ventas")]
    public async Task<IActionResult> CanalVentas(
        [FromQuery] Guid? almacenId, [FromQuery] string? canal, [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var fechaInicio = desde ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var fechaFin = (hasta ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

        var query = _db.Comprobantes
            .Where(c => c.EmpresaId == empresaId && c.Estado != EstadoComprobante.ANULADO
                        && c.FechaEmision >= fechaInicio && c.FechaEmision <= fechaFin)
            .Include(c => c.Cliente).Include(c => c.Almacen).Include(c => c.Vendedor)
            .AsQueryable();

        if (almacenId.HasValue) query = query.Where(c => c.AlmacenId == almacenId);
        if (!string.IsNullOrWhiteSpace(canal) && Enum.TryParse<CanalVenta>(canal, true, out var canalVenta))
            query = query.Where(c => c.CanalVenta == canalVenta);

        var comprobantes = await query.OrderByDescending(c => c.FechaEmision).ToListAsync();

        var porCanal = comprobantes
            .GroupBy(c => c.CanalVenta)
            .Select(g => new
            {
                canal = g.Key.ToString(),
                cantidadComprobantes = g.Count(),
                totalVendido = Math.Round(g.Sum(c => c.Total), 2),
            })
            .OrderByDescending(x => x.totalVendido)
            .ToList();

        var detalle = comprobantes.Select(c => new
        {
            c.Id,
            tipoComprobante = c.TipoComprobante.ToString(),
            c.Serie,
            c.Correlativo,
            c.FechaEmision,
            almacen = c.Almacen != null ? c.Almacen.Nombre : null,
            vendedor = c.Vendedor != null ? c.Vendedor.Nombre : null,
            canalVenta = c.CanalVenta.ToString(),
            cliente = c.Cliente.NombreORazonSocial,
            c.Moneda,
            c.Total,
            estado = c.Estado.ToString(),
        }).ToList();

        return Ok(new
        {
            rango = new { desde = fechaInicio, hasta = fechaFin },
            resumen = new
            {
                cantidadComprobantes = comprobantes.Count,
                totalVendido = Math.Round(comprobantes.Sum(c => c.Total), 2),
            },
            porCanal,
            comprobantes = detalle,
        });
    }
}
