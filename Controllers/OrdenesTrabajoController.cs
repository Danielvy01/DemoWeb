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

/// <summary>
/// Órdenes de trabajo: se emiten para iniciar la PRODUCCIÓN de un producto
/// terminado. Al completarse consumen los insumos indicados y generan stock
/// del producto fabricado en el almacén indicado.
/// </summary>
[Authorize]
[MenuPermiso("ordenes-trabajo")]
[ApiController]
[Route("ordenes-trabajo")]
public class OrdenesTrabajoController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly ProductosService _productosService;
    private readonly OrdenSalidaAlmacenService _ordenSalidaService;
    private readonly PdfService _pdfService;
    private readonly EmailService _emailService;

    public OrdenesTrabajoController(
        SiserpDbContext db, ProductosService productosService, OrdenSalidaAlmacenService ordenSalidaService,
        PdfService pdfService, EmailService emailService)
    {
        _db = db;
        _productosService = productosService;
        _ordenSalidaService = ordenSalidaService;
        _pdfService = pdfService;
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrdenTrabajoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var almacen = await _db.Almacenes.FirstOrDefaultAsync(a => a.Id == dto.AlmacenId && a.EmpresaId == empresaId);
        if (almacen is null) return BadRequest(new { message = "Almacén no encontrado" });

        Guid productoId;
        List<CreateOrdenTrabajoInsumoDto> insumos;
        Guid? plantillaId = null;

        if (dto.PlantillaId is not null)
        {
            var plantilla = await _db.PlantillasOrdenTrabajo
                .Include(p => p.Insumos)
                .FirstOrDefaultAsync(p => p.Id == dto.PlantillaId && p.EmpresaId == empresaId && p.Activo);
            if (plantilla is null) return BadRequest(new { message = "Plantilla no encontrada o inactiva" });

            productoId = plantilla.ProductoId;
            plantillaId = plantilla.Id;
            insumos = plantilla.Insumos
                .Select(i => new CreateOrdenTrabajoInsumoDto(i.ProductoId, Math.Round(i.CantidadPorUnidad * dto.CantidadPlanificada, 2)))
                .ToList();
        }
        else
        {
            if (dto.ProductoId is null) return BadRequest(new { message = "Debe indicar un producto o una plantilla" });
            var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == dto.ProductoId && p.EmpresaId == empresaId);
            if (producto is null) return BadRequest(new { message = "Producto a fabricar no encontrado" });
            productoId = producto.Id;
            insumos = dto.Insumos ?? new List<CreateOrdenTrabajoInsumoDto>();
            if (insumos.Count == 0) return BadRequest(new { message = "Debe indicar los insumos o una plantilla predefinida" });
        }

        Guid? ordenCompraId = null;
        if (dto.OrdenCompraId.HasValue)
        {
            var ordenCompra = await _db.OrdenesCompra.FirstOrDefaultAsync(o => o.Id == dto.OrdenCompraId && o.EmpresaId == empresaId);
            if (ordenCompra is null) return BadRequest(new { message = "La orden de compra indicada no existe" });
            ordenCompraId = ordenCompra.Id;
        }

        var correlativo = await _db.OrdenesTrabajo.CountAsync(o => o.EmpresaId == empresaId) + 1;

        var orden = new OrdenTrabajo
        {
            EmpresaId = empresaId,
            Numero = $"OT-{correlativo:D6}",
            OrdenCompraId = ordenCompraId,
            PlantillaId = plantillaId,
            ProductoId = productoId,
            AlmacenId = dto.AlmacenId,
            CantidadPlanificada = dto.CantidadPlanificada,
            Observaciones = dto.Observaciones,
            Estado = EstadoOrdenTrabajo.PLANIFICADA,
            Insumos = insumos.Select(i => new OrdenTrabajoInsumo
            {
                ProductoId = i.ProductoId,
                CantidadRequerida = i.CantidadRequerida,
            }).ToList(),
        };
        _db.OrdenesTrabajo.Add(orden);
        await _db.SaveChangesAsync();

        return Ok(await Obtener(orden.Id, empresaId));
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        var ordenes = await _db.OrdenesTrabajo
            .Where(o => o.EmpresaId == empresaId)
            .Include(o => o.Producto)
            .Include(o => o.Almacen)
            .Include(o => o.OrdenCompra)
            .Include(o => o.Insumos)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return Ok(ordenes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await Obtener(id, empresaId);
        if (orden is null) return NotFound(new { message = "Orden de trabajo no encontrada" });
        return Ok(orden);
    }

    /// <summary>
    /// Inicia la orden: genera y confirma una orden de salida de almacén con
    /// los insumos requeridos (es esa orden de salida la que efectivamente
    /// descuenta el stock y actualiza el Kardex, costeado por promedio).
    /// </summary>
    [HttpPost("{id:guid}/iniciar")]
    public async Task<IActionResult> Iniciar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await _db.OrdenesTrabajo
            .Include(o => o.Insumos)
            .FirstOrDefaultAsync(o => o.Id == id && o.EmpresaId == empresaId);
        if (orden is null) return NotFound(new { message = "Orden de trabajo no encontrada" });
        if (orden.Estado != EstadoOrdenTrabajo.PLANIFICADA) return BadRequest(new { message = "Solo se puede iniciar una orden PLANIFICADA" });

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var ordenSalida = await _ordenSalidaService.CrearAsync(
            empresaId, orden.AlmacenId, $"Consumo de insumos - orden de trabajo {orden.Numero}", null, orden.Id,
            orden.Insumos.Select(i => (i.ProductoId, i.CantidadRequerida)));

        try
        {
            await _ordenSalidaService.ConfirmarAsync(ordenSalida);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        foreach (var insumo in orden.Insumos)
        {
            insumo.CantidadConsumida = insumo.CantidadRequerida;
        }

        orden.OrdenSalidaAlmacenId = ordenSalida.Id;
        orden.Estado = EstadoOrdenTrabajo.EN_PROCESO;
        orden.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(await Obtener(orden.Id, empresaId));
    }

    /// <summary>Completa la orden: ingresa al almacén el producto fabricado.</summary>
    [HttpPost("{id:guid}/completar")]
    public async Task<IActionResult> Completar(Guid id, CompletarOrdenTrabajoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await _db.OrdenesTrabajo.FirstOrDefaultAsync(o => o.Id == id && o.EmpresaId == empresaId);
        if (orden is null) return NotFound(new { message = "Orden de trabajo no encontrada" });
        if (orden.Estado != EstadoOrdenTrabajo.EN_PROCESO) return BadRequest(new { message = "Solo se puede completar una orden EN_PROCESO" });

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var stock = await _db.Stocks.FirstOrDefaultAsync(s => s.ProductoId == orden.ProductoId && s.AlmacenId == orden.AlmacenId);
        if (stock is null)
            _db.Stocks.Add(new Stock { ProductoId = orden.ProductoId, AlmacenId = orden.AlmacenId, Cantidad = dto.CantidadProducida });
        else
        {
            stock.Cantidad += dto.CantidadProducida;
            stock.UpdatedAt = DateTime.UtcNow;
        }

        _db.MovimientosInventario.Add(new MovimientoInventario
        {
            ProductoId = orden.ProductoId,
            AlmacenId = orden.AlmacenId,
            Tipo = TipoMovimiento.ENTRADA,
            Cantidad = dto.CantidadProducida,
            Referencia = $"Producción orden de trabajo {orden.Numero}",
        });

        orden.CantidadProducida = dto.CantidadProducida;
        orden.Estado = EstadoOrdenTrabajo.COMPLETADA;
        orden.FechaFin = DateTime.UtcNow;
        orden.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        await _productosService.VerificarUmbralStockAsync(orden.ProductoId);

        return Ok(await Obtener(orden.Id, empresaId));
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await _db.OrdenesTrabajo.FirstOrDefaultAsync(o => o.Id == id && o.EmpresaId == empresaId);
        if (orden is null) return NotFound(new { message = "Orden de trabajo no encontrada" });
        if (orden.Estado == EstadoOrdenTrabajo.COMPLETADA) return BadRequest(new { message = "No se puede cancelar una orden ya completada" });
        orden.Estado = EstadoOrdenTrabajo.CANCELADA;
        orden.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(orden);
    }

    private async Task<OrdenTrabajo?> Obtener(Guid id, Guid empresaId) =>
        await _db.OrdenesTrabajo
            .Where(o => o.Id == id && o.EmpresaId == empresaId)
            .Include(o => o.Producto)
            .Include(o => o.Almacen)
            .Include(o => o.OrdenCompra)
            .Include(o => o.Plantilla)
            .Include(o => o.Insumos).ThenInclude(i => i.Producto)
            .FirstOrDefaultAsync();

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await Obtener(id, empresaId);
        if (orden is null) return NotFound(new { message = "Orden de trabajo no encontrada" });
        var empresa = await _db.Empresas.FirstAsync(e => e.Id == empresaId);
        var pdf = _pdfService.GenerarOrdenTrabajo(orden, empresa);
        return File(pdf, "application/pdf", $"{orden.Numero}.pdf");
    }

    [HttpPost("{id:guid}/enviar-correo")]
    public async Task<IActionResult> EnviarCorreo(Guid id, EnviarCorreoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await Obtener(id, empresaId);
        if (orden is null) return NotFound(new { message = "Orden de trabajo no encontrada" });
        if (string.IsNullOrWhiteSpace(dto.Para)) return BadRequest(new { message = "Debe indicar un correo destino" });

        var empresa = await _db.Empresas.FirstAsync(e => e.Id == empresaId);
        var pdf = _pdfService.GenerarOrdenTrabajo(orden, empresa);
        try
        {
            await _emailService.SendAsync(
                dto.Para, dto.Asunto ?? $"Orden de trabajo {orden.Numero}",
                dto.Mensaje ?? $"Adjuntamos la orden de trabajo {orden.Numero}.", pdf, $"{orden.Numero}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        return Ok(new { message = "Correo enviado" });
    }
}
