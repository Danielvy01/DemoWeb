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

[Authorize]
[MenuPermiso("ventas")]
[ApiController]
[Route("ventas")]
public class VentasController : ControllerBase
{
    private const decimal IgvTasa = 0.18m;

    /// <summary>Tipos de comprobante que efectivamente se declaran/envían a SUNAT (la Nota de venta es solo un control interno).</summary>
    private static readonly HashSet<TipoComprobante> TiposDeclaradosSunat = new()
    {
        TipoComprobante.FACTURA, TipoComprobante.BOLETA, TipoComprobante.NOTA_CREDITO, TipoComprobante.NOTA_DEBITO,
    };

    private static readonly Dictionary<TipoComprobante, string> SeriePorTipo = new()
    {
        [TipoComprobante.FACTURA] = "F001",
        [TipoComprobante.BOLETA] = "B001",
        [TipoComprobante.NOTA_CREDITO] = "FC01",
        [TipoComprobante.NOTA_DEBITO] = "FD01",
        [TipoComprobante.NOTA_VENTA] = "NV01",
    };

    private readonly SiserpDbContext _db;
    private readonly ProductosService _productosService;
    private readonly PdfService _pdfService;
    private readonly EmailService _emailService;
    private readonly ContabilidadService _contabilidadService;
    private readonly AutorizacionAdminService _autorizacionAdminService;

    public VentasController(
        SiserpDbContext db, ProductosService productosService, PdfService pdfService,
        EmailService emailService, ContabilidadService contabilidadService, AutorizacionAdminService autorizacionAdminService)
    {
        _db = db;
        _productosService = productosService;
        _pdfService = pdfService;
        _emailService = emailService;
        _contabilidadService = contabilidadService;
        _autorizacionAdminService = autorizacionAdminService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateComprobanteDto dto)
    {
        if (dto.Items is null || dto.Items.Count == 0)
        {
            return BadRequest(new { message = "El comprobante debe tener al menos un item" });
        }

        var empresaId = User.GetEmpresaId();
        if (!Enum.TryParse<TipoComprobante>(dto.TipoComprobante, true, out var tipoComprobante))
            return BadRequest(new { message = "Tipo de comprobante inválido" });

        var cliente = await _db.Clientes.FirstOrDefaultAsync(c => c.Id == dto.ClienteId && c.EmpresaId == empresaId);
        if (cliente is null) return BadRequest(new { message = "Cliente no encontrado" });
        if (cliente.Bloqueado) return BadRequest(new { message = "El cliente está bloqueado y no admite nuevas ventas" });
        if (!cliente.Activo) return BadRequest(new { message = "El cliente está inactivo" });

        // --- Condición de pago: al crédito exige fecha de vencimiento y no pide métodos de pago;
        // al contado exige uno o más métodos de pago que cubran el total. ---
        var condicionPago = string.IsNullOrWhiteSpace(dto.CondicionPago)
            ? CondicionPago.CONTADO
            : Enum.Parse<CondicionPago>(dto.CondicionPago, true);
        if (condicionPago == CondicionPago.CREDITO && !dto.FechaVencimiento.HasValue)
            return BadRequest(new { message = "La venta al crédito requiere una fecha de vencimiento" });

        // --- Vendedor (opcional pero validado si se envía) ---
        Usuario? vendedor = null;
        if (dto.VendedorId.HasValue)
        {
            vendedor = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == dto.VendedorId && u.EmpresaId == empresaId && u.Activo);
            if (vendedor is null) return BadRequest(new { message = "El vendedor indicado no existe o está inactivo" });
        }

        var tipoOperacion = string.IsNullOrWhiteSpace(dto.TipoOperacion)
            ? TipoOperacionVenta.VENTA_INTERNA
            : Enum.Parse<TipoOperacionVenta>(dto.TipoOperacion, true);

        var canalVenta = CanalVenta.LOCAL;
        if (!string.IsNullOrWhiteSpace(dto.CanalVenta) && !Enum.TryParse(dto.CanalVenta, true, out canalVenta))
            return BadRequest(new { message = "Canal de venta inválido" });

        var productoIds = dto.Items.Select(i => i.ProductoId).ToList();
        var productos = await _db.Productos
            .Where(p => productoIds.Contains(p.Id) && p.EmpresaId == empresaId)
            .Include(p => p.Componentes)
            .ToListAsync();
        if (productos.Count != dto.Items.Count)
        {
            return NotFound(new { message = "Uno o mas productos no existen para esta empresa" });
        }

        var moneda = dto.Moneda ?? "PEN";

        decimal opGravada = 0;
        decimal opInafecta = 0;
        decimal igv = 0;
        var detalles = new List<ComprobanteDetalle>();
        foreach (var item in dto.Items)
        {
            var producto = productos.First(p => p.Id == item.ProductoId);
            var precioUnitario = item.PrecioUnitario ?? ProductosService.CalcularPrecioUnitario(producto, cliente);
            var totalLinea = precioUnitario * item.Cantidad;
            var valorVentaLinea = producto.AfectoIGV ? totalLinea / (1 + IgvTasa) : totalLinea;
            var igvLinea = producto.AfectoIGV ? totalLinea - valorVentaLinea : 0;

            if (producto.AfectoIGV) opGravada += valorVentaLinea; else opInafecta += valorVentaLinea;
            igv += igvLinea;

            detalles.Add(new ComprobanteDetalle
            {
                ProductoId = producto.Id,
                Cantidad = item.Cantidad,
                PrecioUnitario = precioUnitario,
                ValorVenta = Math.Round(valorVentaLinea, 2),
                Igv = Math.Round(igvLinea, 2),
                Total = Math.Round(totalLinea, 2),
            });
        }

        var total = Math.Round(opGravada + opInafecta + igv, 2);

        // --- Métodos de pago (solo aplica al contado) ---
        var pagos = new List<ComprobantePago>();
        if (condicionPago == CondicionPago.CONTADO)
        {
            if (dto.Pagos is null || dto.Pagos.Count == 0)
                return BadRequest(new { message = "La venta al contado requiere al menos un método de pago" });
            foreach (var p in dto.Pagos)
            {
                if (!Enum.TryParse<MetodoPago>(p.MetodoPago, true, out var metodo))
                    return BadRequest(new { message = $"Método de pago inválido: {p.MetodoPago}" });
                pagos.Add(new ComprobantePago { MetodoPago = metodo, Monto = p.Monto, Referencia = p.Referencia });
            }
            var sumaPagos = Math.Round(pagos.Sum(p => p.Monto), 2);
            if (Math.Abs(sumaPagos - total) > 0.05m)
                return BadRequest(new { message = $"La suma de los métodos de pago ({sumaPagos}) no coincide con el total ({total})" });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var serieNombre = SeriePorTipo[tipoComprobante];
        var serie = await _db.SeriesComprobante.FirstOrDefaultAsync(s =>
            s.EmpresaId == empresaId && s.TipoComprobante == tipoComprobante && s.Serie == serieNombre);
        if (serie is null)
        {
            serie = new SerieComprobante { EmpresaId = empresaId, TipoComprobante = tipoComprobante, Serie = serieNombre, CorrelativoActual = 0 };
            _db.SeriesComprobante.Add(serie);
            await _db.SaveChangesAsync();
        }

        serie.CorrelativoActual += 1;
        await _db.SaveChangesAsync();

        var esDeclaradoSunat = TiposDeclaradosSunat.Contains(tipoComprobante);

        var comprobante = new Comprobante
        {
            EmpresaId = empresaId,
            ClienteId = dto.ClienteId,
            SerieId = serie.Id,
            AlmacenId = dto.AlmacenId,
            VendedorId = vendedor?.Id,
            CentroCostoId = dto.CentroCostoId,
            TipoComprobante = tipoComprobante,
            TipoOperacion = tipoOperacion,
            CondicionPago = condicionPago,
            CanalVenta = canalVenta,
            Serie = serieNombre,
            Correlativo = serie.CorrelativoActual,
            Moneda = moneda,
            OpGravada = Math.Round(opGravada, 2),
            OpInafecta = Math.Round(opInafecta, 2),
            Igv = Math.Round(igv, 2),
            Total = total,
            Estado = EstadoComprobante.BORRADOR,
            FechaVencimiento = condicionPago == CondicionPago.CREDITO ? dto.FechaVencimiento : null,
            Observaciones = dto.Observaciones,
            Detalles = detalles,
            Pagos = pagos,
        };
        _db.Comprobantes.Add(comprobante);
        await _db.SaveChangesAsync();

        // Descuenta stock del almacen indicado por cada linea vendida, y, si el producto es
        // un producto terminado con composición (BOM), también descuenta los insumos que lo componen.
        foreach (var detalle in detalles)
        {
            var producto = productos.First(p => p.Id == detalle.ProductoId);
            await _productosService.AjustarStockAsync(
                detalle.ProductoId, dto.AlmacenId, detalle.Cantidad, TipoMovimiento.SALIDA,
                $"{serieNombre}-{serie.CorrelativoActual}");

            foreach (var componente in producto.Componentes)
            {
                await _productosService.AjustarStockAsync(
                    componente.InsumoId, dto.AlmacenId, componente.Cantidad * detalle.Cantidad, TipoMovimiento.SALIDA,
                    $"{serieNombre}-{serie.CorrelativoActual} (insumo de {producto.Descripcion})");
            }
        }

        if (esDeclaradoSunat)
        {
            // Encola la sincronizacion con SUNAT (si la empresa no esta en modo LOCAL
            // el worker de SunatQueueProcessor la tomara automaticamente; si esta en
            // modo LOCAL, el item se marcara como completado sin salir del sistema).
            _db.SunatSyncQueue.Add(new SunatSyncQueue { ComprobanteId = comprobante.Id });
            comprobante.Estado = EstadoComprobante.PENDIENTE_ENVIO;
        }
        else
        {
            // Nota de venta: documento de control interno, no se declara a SUNAT.
            comprobante.Estado = EstadoComprobante.EMITIDO_LOCAL;
        }
        await _db.SaveChangesAsync();

        // Genera el asiento contable de la venta (Libro Diario) y lo vincula al comprobante
        // (vinculación asiento ↔ comprobante SUNAT, al estilo CONCAR); si el plan de cuentas no
        // está configurado, se omite silenciosamente para no bloquear la emisión de la venta.
        try
        {
            var asiento = await _contabilidadService.RegistrarAsientoVentaAsync(comprobante);
            if (asiento is not null)
            {
                comprobante.AsientoContableId = asiento.Id;
                await _db.SaveChangesAsync();
            }
        }
        catch
        {
            // La contabilización es una conveniencia automática; un fallo aquí no debe revertir la venta.
        }

        await transaction.CommitAsync();

        var resultado = await _db.Comprobantes
            .Include(c => c.Detalles)
            .Include(c => c.Pagos)
            .FirstAsync(c => c.Id == comprobante.Id);
        return Ok(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll([FromQuery] Guid? almacenId)
    {
        var empresaId = User.GetEmpresaId();
        var query = _db.Comprobantes.Where(c => c.EmpresaId == empresaId);
        if (almacenId.HasValue) query = query.Where(c => c.AlmacenId == almacenId);
        var comprobantes = await query
            .Include(c => c.Cliente)
            .Include(c => c.Vendedor)
            .Include(c => c.Almacen)
            .Include(c => c.Detalles)
            .Include(c => c.Pagos)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(comprobantes);
    }

    /// <summary>Movimientos de venta agrupados/filtrados por almacén (para ver por almacén todas las ventas realizadas).</summary>
    [HttpGet("por-almacen")]
    public async Task<IActionResult> PorAlmacen([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
    {
        var empresaId = User.GetEmpresaId();
        var query = _db.Comprobantes.Where(c => c.EmpresaId == empresaId && c.Estado != EstadoComprobante.ANULADO);
        if (desde.HasValue) query = query.Where(c => c.FechaEmision >= desde.Value);
        if (hasta.HasValue) query = query.Where(c => c.FechaEmision <= hasta.Value);

        var comprobantes = await query
            .Include(c => c.Cliente)
            .Include(c => c.Almacen)
            .Include(c => c.Vendedor)
            .Include(c => c.Detalles)
            .ToListAsync();

        var agrupado = comprobantes
            .GroupBy(c => new { c.AlmacenId, Nombre = c.Almacen != null ? c.Almacen.Nombre : "Sin almacén" })
            .Select(g => new
            {
                almacenId = g.Key.AlmacenId,
                almacen = g.Key.Nombre,
                cantidadVentas = g.Count(),
                totalVendido = g.Sum(c => c.Total),
                comprobantes = g.Select(c => new
                {
                    c.Id,
                    c.TipoComprobante,
                    c.Serie,
                    c.Correlativo,
                    c.FechaEmision,
                    c.Total,
                    c.Estado,
                    Cliente = c.Cliente.NombreORazonSocial,
                    Vendedor = c.Vendedor != null ? c.Vendedor.Nombre : null,
                }),
            })
            .OrderByDescending(g => g.totalVendido)
            .ToList();

        return Ok(agrupado);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var comprobante = await _db.Comprobantes
            .Where(c => c.Id == id && c.EmpresaId == empresaId)
            .Include(c => c.Cliente)
            .Include(c => c.Vendedor)
            .Include(c => c.Almacen)
            .Include(c => c.Pagos)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync();
        if (comprobante is null) return NotFound(new { message = "Comprobante no encontrado" });
        return Ok(comprobante);
    }

    /// <summary>Registra el pago de la cuenta por cobrar de un comprobante (factura/boleta al crédito).</summary>
    [HttpPost("{id:guid}/marcar-pagado")]
    public async Task<IActionResult> MarcarPagado(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var comprobante = await _db.Comprobantes.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (comprobante is null) return NotFound(new { message = "Comprobante no encontrado" });
        comprobante.Pagado = true;
        comprobante.FechaPago = DateTime.UtcNow;
        comprobante.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(comprobante);
    }

    /// <summary>
    /// Anula (baja lógica) un comprobante: no se elimina el registro para conservar el correlativo
    /// y el histórico, solo cambia su estado a ANULADO. Solo se permite mientras no haya sido
    /// ACEPTADO por SUNAT (una vez aceptado, la corrección legal correcta es una Nota de Crédito).
    /// Si el comprobante ya había descontado stock, se revierte devolviendo la mercadería al almacén.
    /// </summary>
    [HttpPost("{id:guid}/anular")]
    public async Task<IActionResult> Anular(Guid id, AnularComprobanteDto dto)
    {
        var empresaId = User.GetEmpresaId();
        try
        {
            await _autorizacionAdminService.ValidarAsync(empresaId, User.GetUsuarioId(), dto.AdminEmail, dto.AdminPassword);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }

        var comprobante = await _db.Comprobantes
            .Include(c => c.Detalles).ThenInclude(d => d.Producto).ThenInclude(p => p.Componentes)
            .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (comprobante is null) return NotFound(new { message = "Comprobante no encontrado" });
        if (comprobante.Estado == EstadoComprobante.ANULADO) return BadRequest(new { message = "El comprobante ya está anulado" });
        if (comprobante.Estado is EstadoComprobante.ACEPTADO or EstadoComprobante.ACEPTADO_CON_OBS)
            return BadRequest(new { message = "El comprobante ya fue aceptado por SUNAT; emite una Nota de Crédito para corregirlo" });

        var almacenId = dto.AlmacenId ?? comprobante.AlmacenId;
        if (!almacenId.HasValue || almacenId == Guid.Empty)
            return BadRequest(new { message = "Debe indicar el almacén al que se devuelve la mercadería" });

        await using var transaction = await _db.Database.BeginTransactionAsync();

        await RevertirStockAsync(comprobante, almacenId.Value, $"Anulación {comprobante.Serie}-{comprobante.Correlativo}");

        comprobante.Estado = EstadoComprobante.ANULADO;
        comprobante.Observaciones = string.IsNullOrWhiteSpace(dto.Motivo)
            ? comprobante.Observaciones
            : $"{comprobante.Observaciones}\nAnulado: {dto.Motivo}".Trim();
        comprobante.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Anula también el asiento contable vinculado (si lo hubo) para que el Libro Diario/Mayor
        // y la exportación PLE queden consistentes con la anulación del comprobante.
        if (comprobante.AsientoContableId.HasValue)
        {
            await _contabilidadService.AnularAsientoAsync(empresaId, comprobante.AsientoContableId.Value, User.GetUsuarioId(), "Anulación de venta");
        }

        await transaction.CommitAsync();

        return Ok(comprobante);
    }

    /// <summary>
    /// Elimina definitivamente una venta (opción de menú "Eliminar venta"). Solo se permite mientras
    /// el comprobante no haya sido declarado/aceptado por SUNAT; devuelve el stock (producto e
    /// insumos de su composición) al almacén de origen antes de borrar el registro.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid? almacenId)
    {
        var empresaId = User.GetEmpresaId();
        var comprobante = await _db.Comprobantes
            .Include(c => c.Detalles).ThenInclude(d => d.Producto).ThenInclude(p => p.Componentes)
            .Include(c => c.Pagos)
            .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (comprobante is null) return NotFound(new { message = "Comprobante no encontrado" });
        if (comprobante.Estado is EstadoComprobante.ACEPTADO or EstadoComprobante.ACEPTADO_CON_OBS)
            return BadRequest(new { message = "No se puede eliminar un comprobante ya aceptado por SUNAT; anúlalo o emite una Nota de Crédito" });

        var destinoAlmacen = almacenId ?? comprobante.AlmacenId;
        if (comprobante.Estado != EstadoComprobante.ANULADO)
        {
            if (!destinoAlmacen.HasValue || destinoAlmacen == Guid.Empty)
                return BadRequest(new { message = "Debe indicar el almacén al que se devuelve la mercadería" });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        if (comprobante.Estado != EstadoComprobante.ANULADO)
        {
            await RevertirStockAsync(comprobante, destinoAlmacen!.Value, $"Eliminación de venta {comprobante.Serie}-{comprobante.Correlativo}");
        }

        var pendientesCola = _db.SunatSyncQueue.Where(s => s.ComprobanteId == comprobante.Id);
        _db.SunatSyncQueue.RemoveRange(pendientesCola);
        _db.ComprobantePagos.RemoveRange(comprobante.Pagos);
        _db.ComprobanteDetalles.RemoveRange(comprobante.Detalles);
        _db.Comprobantes.Remove(comprobante);
        await _db.SaveChangesAsync();

        // Anula también el asiento contable vinculado (si lo hubo), igual que al anular una venta.
        if (comprobante.AsientoContableId.HasValue)
        {
            await _contabilidadService.AnularAsientoAsync(empresaId, comprobante.AsientoContableId.Value, User.GetUsuarioId(), "Eliminación de venta");
        }

        await transaction.CommitAsync();

        return Ok(new { message = "Venta eliminada" });
    }

    /// <summary>Devuelve al stock (producto e insumos de su composición) lo descontado por un comprobante.</summary>
    private async Task RevertirStockAsync(Comprobante comprobante, Guid almacenId, string referencia)
    {
        foreach (var detalle in comprobante.Detalles)
        {
            await _productosService.AjustarStockAsync(detalle.ProductoId, almacenId, detalle.Cantidad, TipoMovimiento.ENTRADA, referencia);
            foreach (var componente in detalle.Producto.Componentes)
            {
                await _productosService.AjustarStockAsync(
                    componente.InsumoId, almacenId, componente.Cantidad * detalle.Cantidad, TipoMovimiento.ENTRADA,
                    $"{referencia} (insumo de {detalle.Producto.Descripcion})");
            }
        }
    }

    /// <summary>
    /// Emite una Nota de Crédito que afecta a un comprobante (Factura/Boleta/Nota de venta) ya
    /// emitido: repone el stock devuelto y, si el original se declara a SUNAT, encola la NC.
    /// </summary>
    [HttpPost("{id:guid}/notas-credito")]
    public async Task<IActionResult> CrearNotaCredito(Guid id, CreateNotaCreditoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        try
        {
            await _autorizacionAdminService.ValidarAsync(empresaId, User.GetUsuarioId(), dto.AdminEmail, dto.AdminPassword);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }

        var original = await _db.Comprobantes
            .Include(c => c.Detalles).ThenInclude(d => d.Producto).ThenInclude(p => p.Componentes)
            .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (original is null) return NotFound(new { message = "Comprobante no encontrado" });
        if (original.TipoComprobante is TipoComprobante.NOTA_CREDITO or TipoComprobante.NOTA_DEBITO)
            return BadRequest(new { message = "No se puede emitir una Nota de Crédito sobre otra nota" });
        if (string.IsNullOrWhiteSpace(dto.Motivo))
            return BadRequest(new { message = "Debe indicar el motivo de la Nota de Crédito" });

        var itemsAAplicar = dto.Items is { Count: > 0 } ? dto.Items : original.Detalles.Select(d => new ItemComprobanteDto(d.ProductoId, d.Cantidad)).ToList();

        var detalles = new List<ComprobanteDetalle>();
        decimal opGravada = 0, opInafecta = 0, igv = 0;
        foreach (var item in itemsAAplicar)
        {
            var detalleOriginal = original.Detalles.FirstOrDefault(d => d.ProductoId == item.ProductoId);
            if (detalleOriginal is null)
                return BadRequest(new { message = "El producto indicado no forma parte del comprobante original" });
            if (item.Cantidad <= 0 || item.Cantidad > detalleOriginal.Cantidad)
                return BadRequest(new { message = $"La cantidad a acreditar de {detalleOriginal.Producto.Descripcion} excede lo vendido originalmente" });

            var proporcion = item.Cantidad / detalleOriginal.Cantidad;
            var valorVentaLinea = Math.Round(detalleOriginal.ValorVenta * proporcion, 2);
            var igvLinea = Math.Round(detalleOriginal.Igv * proporcion, 2);
            var totalLinea = Math.Round(detalleOriginal.Total * proporcion, 2);

            if (detalleOriginal.Producto.AfectoIGV) opGravada += valorVentaLinea; else opInafecta += valorVentaLinea;
            igv += igvLinea;

            detalles.Add(new ComprobanteDetalle
            {
                ProductoId = item.ProductoId,
                Cantidad = item.Cantidad,
                PrecioUnitario = detalleOriginal.PrecioUnitario,
                ValorVenta = valorVentaLinea,
                Igv = igvLinea,
                Total = totalLinea,
            });
        }

        var total = Math.Round(opGravada + opInafecta + igv, 2);

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var serieNombre = original.TipoComprobante == TipoComprobante.BOLETA ? "BC01" : SeriePorTipo[TipoComprobante.NOTA_CREDITO];
        var serie = await _db.SeriesComprobante.FirstOrDefaultAsync(s =>
            s.EmpresaId == empresaId && s.TipoComprobante == TipoComprobante.NOTA_CREDITO && s.Serie == serieNombre);
        if (serie is null)
        {
            serie = new SerieComprobante { EmpresaId = empresaId, TipoComprobante = TipoComprobante.NOTA_CREDITO, Serie = serieNombre, CorrelativoActual = 0 };
            _db.SeriesComprobante.Add(serie);
            await _db.SaveChangesAsync();
        }
        serie.CorrelativoActual += 1;
        await _db.SaveChangesAsync();

        var esDeclaradoSunat = TiposDeclaradosSunat.Contains(original.TipoComprobante);

        var notaCredito = new Comprobante
        {
            EmpresaId = empresaId,
            ClienteId = original.ClienteId,
            SerieId = serie.Id,
            AlmacenId = dto.AlmacenId,
            VendedorId = original.VendedorId,
            CentroCostoId = original.CentroCostoId,
            TipoComprobante = TipoComprobante.NOTA_CREDITO,
            TipoOperacion = original.TipoOperacion,
            CondicionPago = original.CondicionPago,
            ComprobanteReferenciaId = original.Id,
            MotivoNota = dto.Motivo,
            Serie = serieNombre,
            Correlativo = serie.CorrelativoActual,
            Moneda = original.Moneda,
            OpGravada = Math.Round(opGravada, 2),
            OpInafecta = Math.Round(opInafecta, 2),
            Igv = Math.Round(igv, 2),
            Total = total,
            Estado = esDeclaradoSunat ? EstadoComprobante.BORRADOR : EstadoComprobante.EMITIDO_LOCAL,
            Detalles = detalles,
        };
        _db.Comprobantes.Add(notaCredito);
        await _db.SaveChangesAsync();

        // Repone al stock la mercaderia devuelta (y, si aplica, los insumos de su composición).
        foreach (var detalle in detalles)
        {
            var producto = original.Detalles.First(d => d.ProductoId == detalle.ProductoId).Producto;
            await _productosService.AjustarStockAsync(
                detalle.ProductoId, dto.AlmacenId, detalle.Cantidad, TipoMovimiento.ENTRADA,
                $"NC {serieNombre}-{serie.CorrelativoActual}");
            foreach (var componente in producto.Componentes)
            {
                await _productosService.AjustarStockAsync(
                    componente.InsumoId, dto.AlmacenId, componente.Cantidad * detalle.Cantidad, TipoMovimiento.ENTRADA,
                    $"NC {serieNombre}-{serie.CorrelativoActual} (insumo de {producto.Descripcion})");
            }
        }

        if (esDeclaradoSunat)
        {
            _db.SunatSyncQueue.Add(new SunatSyncQueue { ComprobanteId = notaCredito.Id });
            notaCredito.Estado = EstadoComprobante.PENDIENTE_ENVIO;
            await _db.SaveChangesAsync();
        }

        try
        {
            var asientoNc = await _contabilidadService.RegistrarAsientoNotaCreditoAsync(notaCredito, original.CondicionPago);
            if (asientoNc is not null)
            {
                notaCredito.AsientoContableId = asientoNc.Id;
                await _db.SaveChangesAsync();
            }
        }
        catch
        {
            // La contabilización es una conveniencia automática; un fallo aquí no debe revertir la NC.
        }

        await transaction.CommitAsync();

        var resultado = await _db.Comprobantes.Include(c => c.Detalles).FirstAsync(c => c.Id == notaCredito.Id);
        return Ok(resultado);
    }

    private async Task<Comprobante?> ObtenerParaImpresion(Guid id, Guid empresaId) =>
        await _db.Comprobantes
            .Where(c => c.Id == id && c.EmpresaId == empresaId)
            .Include(c => c.Cliente)
            .Include(c => c.Vendedor)
            .Include(c => c.Pagos)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync();

    /// <summary>Genera el PDF del comprobante. <paramref name="formato"/>: "A4" (por defecto) o "TICKET80" (ticketera térmica 80mm).</summary>
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id, [FromQuery] string formato = "A4")
    {
        var empresaId = User.GetEmpresaId();
        var comprobante = await ObtenerParaImpresion(id, empresaId);
        if (comprobante is null) return NotFound(new { message = "Comprobante no encontrado" });
        var empresa = await _db.Empresas.FirstAsync(e => e.Id == empresaId);
        var pdf = string.Equals(formato, "TICKET80", StringComparison.OrdinalIgnoreCase)
            ? _pdfService.GenerarComprobanteTicket80mm(comprobante, empresa)
            : _pdfService.GenerarComprobante(comprobante, empresa);
        return File(pdf, "application/pdf", $"{comprobante.Serie}-{comprobante.Correlativo:D8}.pdf");
    }

    [HttpPost("{id:guid}/enviar-correo")]
    public async Task<IActionResult> EnviarCorreo(Guid id, EnviarCorreoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var comprobante = await ObtenerParaImpresion(id, empresaId);
        if (comprobante is null) return NotFound(new { message = "Comprobante no encontrado" });

        var destino = dto.Para ?? comprobante.Cliente.Email;
        if (string.IsNullOrWhiteSpace(destino)) return BadRequest(new { message = "No se indicó un correo destino y el cliente no tiene uno registrado" });

        var empresa = await _db.Empresas.FirstAsync(e => e.Id == empresaId);
        var pdf = _pdfService.GenerarComprobante(comprobante, empresa);
        var numero = $"{comprobante.Serie}-{comprobante.Correlativo:D8}";
        try
        {
            await _emailService.SendAsync(
                destino, dto.Asunto ?? $"Comprobante {numero}",
                dto.Mensaje ?? $"Adjuntamos el comprobante {numero}.", pdf, $"{numero}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        return Ok(new { message = "Correo enviado" });
    }
}
