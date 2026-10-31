using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Dtos;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;
using Siserp.Api.Services;

namespace Siserp.Api.Controllers;

/// <summary>
/// Órdenes de compra: se emiten para SOLICITAR una compra a un proveedor.
/// La recepción de mercadería (que ingresa stock) se registra aparte, ya sea
/// generando una <see cref="Compra"/> o marcando la orden como recibida.
/// </summary>
[Authorize]
[MenuPermiso("ordenes-compra")]
[ApiController]
[Route("ordenes-compra")]
public class OrdenesCompraController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly IOrdenCompraRepository _ordenCompraRepository;
    private readonly ComprasService _comprasService;
    private readonly PdfService _pdfService;
    private readonly EmailService _emailService;

    public OrdenesCompraController(
        SiserpDbContext db, IOrdenCompraRepository ordenCompraRepository, ComprasService comprasService,
        PdfService pdfService, EmailService emailService)
    {
        _db = db;
        _ordenCompraRepository = ordenCompraRepository;
        _comprasService = comprasService;
        _pdfService = pdfService;
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrdenCompraDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == dto.ProveedorId && p.EmpresaId == empresaId);
        if (proveedor is null) return BadRequest(new { message = "Proveedor no encontrado" });
        if (proveedor.Bloqueado) return BadRequest(new { message = "El proveedor está bloqueado y no admite nuevas órdenes de compra" });
        if (!proveedor.Activo) return BadRequest(new { message = "El proveedor está inactivo" });
        if (dto.Detalles.Count == 0) return BadRequest(new { message = "La orden debe tener al menos un detalle" });

        Guid? comprobanteId = null;
        if (dto.ComprobanteId.HasValue)
        {
            var comprobante = await _db.Comprobantes.FirstOrDefaultAsync(c => c.Id == dto.ComprobanteId && c.EmpresaId == empresaId);
            if (comprobante is null) return BadRequest(new { message = "La venta indicada no existe" });
            comprobanteId = comprobante.Id;
        }

        Guid? empleadoCompradorId = null;
        if (dto.EmpleadoCompradorId.HasValue)
        {
            var comprador = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == dto.EmpleadoCompradorId && e.EmpresaId == empresaId);
            if (comprador is null) return BadRequest(new { message = "El personal comprador indicado no existe" });
            if (!comprador.EsComprador) return BadRequest(new { message = "El personal indicado no tiene el rol de Comprador" });
            if (!comprador.Activo) return BadRequest(new { message = "El personal comprador indicado está inactivo" });
            empleadoCompradorId = comprador.Id;
        }

        var tipoEntrega = Enum.TryParse<TipoEntregaOrdenCompra>(dto.TipoEntrega, out var te) ? te : TipoEntregaOrdenCompra.INMEDIATA;
        if (tipoEntrega == TipoEntregaOrdenCompra.A_DIAS && (dto.PlazoEntregaDias is null || dto.PlazoEntregaDias <= 0))
            return BadRequest(new { message = "Debe indicar el plazo de entrega en días" });

        var fechaEmision = DateTime.UtcNow;
        var fechaEntregaEsperada = dto.FechaEntregaEsperada ?? tipoEntrega switch
        {
            TipoEntregaOrdenCompra.INMEDIATA => fechaEmision,
            TipoEntregaOrdenCompra.A_DIAS => fechaEmision.AddDays(dto.PlazoEntregaDias!.Value),
            _ => (DateTime?)null,
        };

        var correlativo = await _db.OrdenesCompra.CountAsync(o => o.EmpresaId == empresaId) + 1;
        var total = dto.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);

        var cuentasContablesIds = dto.Detalles.Where(d => d.CuentaContableId.HasValue).Select(d => d.CuentaContableId!.Value).Distinct().ToList();
        var cuentasContablesValidas = cuentasContablesIds.Count == 0
            ? new List<Guid>()
            : await _db.CuentasContables.Where(c => c.EmpresaId == empresaId && cuentasContablesIds.Contains(c.Id)).Select(c => c.Id).ToListAsync();
        if (cuentasContablesValidas.Count != cuentasContablesIds.Count)
            return BadRequest(new { message = "Una o más cuentas contables indicadas para los ítems no existen" });

        var orden = new OrdenCompra
        {
            EmpresaId = empresaId,
            ProveedorId = dto.ProveedorId,
            EmpleadoCompradorId = empleadoCompradorId,
            ComprobanteId = comprobanteId,
            Numero = $"OC-{correlativo:D6}",
            FechaEmision = fechaEmision,
            TipoEntrega = tipoEntrega,
            PlazoEntregaDias = tipoEntrega == TipoEntregaOrdenCompra.A_DIAS ? dto.PlazoEntregaDias : null,
            FechaEntregaEsperada = fechaEntregaEsperada,
            Moneda = dto.Moneda ?? "PEN",
            Total = Math.Round(total, 2),
            Observaciones = dto.Observaciones,
            Estado = EstadoOrdenCompra.BORRADOR,
            Detalles = dto.Detalles.Select(d => new OrdenCompraDetalle
            {
                ProductoId = d.ProductoId,
                Cantidad = d.Cantidad,
                PrecioUnitario = d.PrecioUnitario,
                Total = Math.Round(d.Cantidad * d.PrecioUnitario, 2),
                CuentaContableId = d.CuentaContableId,
            }).ToList(),
        };
        await _ordenCompraRepository.RegistrarAsync(orden);

        return Ok(await Obtener(orden.Id, empresaId));
    }

    /// <summary>
    /// Edita una orden de compra en estado BORRADOR: permite actualizar sus datos
    /// generales y reemplazar la lista de ítems (agregar, quitar o modificar detalles).
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateOrdenCompraDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await _db.OrdenesCompra
            .Include(o => o.Detalles)
            .FirstOrDefaultAsync(o => o.Id == id && o.EmpresaId == empresaId);
        if (orden is null) return NotFound(new { message = "Orden de compra no encontrada" });
        if (orden.Estado != EstadoOrdenCompra.BORRADOR)
            return BadRequest(new { message = "Solo se puede editar una orden en estado BORRADOR" });
        if (dto.Detalles.Count == 0) return BadRequest(new { message = "La orden debe tener al menos un detalle" });

        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == dto.ProveedorId && p.EmpresaId == empresaId);
        if (proveedor is null) return BadRequest(new { message = "Proveedor no encontrado" });
        if (proveedor.Bloqueado) return BadRequest(new { message = "El proveedor está bloqueado y no admite nuevas órdenes de compra" });
        if (!proveedor.Activo) return BadRequest(new { message = "El proveedor está inactivo" });

        Guid? comprobanteId = null;
        if (dto.ComprobanteId.HasValue)
        {
            var comprobante = await _db.Comprobantes.FirstOrDefaultAsync(c => c.Id == dto.ComprobanteId && c.EmpresaId == empresaId);
            if (comprobante is null) return BadRequest(new { message = "La venta indicada no existe" });
            comprobanteId = comprobante.Id;
        }

        Guid? empleadoCompradorId = null;
        if (dto.EmpleadoCompradorId.HasValue)
        {
            var comprador = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == dto.EmpleadoCompradorId && e.EmpresaId == empresaId);
            if (comprador is null) return BadRequest(new { message = "El personal comprador indicado no existe" });
            if (!comprador.EsComprador) return BadRequest(new { message = "El personal indicado no tiene el rol de Comprador" });
            if (!comprador.Activo) return BadRequest(new { message = "El personal comprador indicado está inactivo" });
            empleadoCompradorId = comprador.Id;
        }

        var tipoEntrega = Enum.TryParse<TipoEntregaOrdenCompra>(dto.TipoEntrega, out var te) ? te : TipoEntregaOrdenCompra.INMEDIATA;
        if (tipoEntrega == TipoEntregaOrdenCompra.A_DIAS && (dto.PlazoEntregaDias is null || dto.PlazoEntregaDias <= 0))
            return BadRequest(new { message = "Debe indicar el plazo de entrega en días" });

        var cuentasContablesIds = dto.Detalles.Where(d => d.CuentaContableId.HasValue).Select(d => d.CuentaContableId!.Value).Distinct().ToList();
        var cuentasContablesValidas = cuentasContablesIds.Count == 0
            ? new List<Guid>()
            : await _db.CuentasContables.Where(c => c.EmpresaId == empresaId && cuentasContablesIds.Contains(c.Id)).Select(c => c.Id).ToListAsync();
        if (cuentasContablesValidas.Count != cuentasContablesIds.Count)
            return BadRequest(new { message = "Una o más cuentas contables indicadas para los ítems no existen" });

        var fechaEntregaEsperada = dto.FechaEntregaEsperada ?? tipoEntrega switch
        {
            TipoEntregaOrdenCompra.INMEDIATA => orden.FechaEmision,
            TipoEntregaOrdenCompra.A_DIAS => orden.FechaEmision.AddDays(dto.PlazoEntregaDias!.Value),
            _ => (DateTime?)null,
        };

        orden.ProveedorId = dto.ProveedorId;
        orden.EmpleadoCompradorId = empleadoCompradorId;
        orden.ComprobanteId = comprobanteId;
        orden.Moneda = dto.Moneda ?? "PEN";
        orden.Observaciones = dto.Observaciones;
        orden.TipoEntrega = tipoEntrega;
        orden.PlazoEntregaDias = tipoEntrega == TipoEntregaOrdenCompra.A_DIAS ? dto.PlazoEntregaDias : null;
        orden.FechaEntregaEsperada = fechaEntregaEsperada;
        orden.Total = Math.Round(dto.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario), 2);

        // El reemplazo de los ítems (quitar los antiguos e insertar los nuevos) ahora lo
        // resuelve íntegramente dbo.sp_OrdenCompra_Modificar (DELETE + INSERT dentro del
        // mismo procedimiento); aquí solo se arma la colección en memoria que viaja como
        // TVP, sin tocar el change tracker de EF (la escritura ya no pasa por SaveChanges).
        var detalles = dto.Detalles.Select(d => new OrdenCompraDetalle
        {
            OrdenCompraId = orden.Id,
            ProductoId = d.ProductoId,
            Cantidad = d.Cantidad,
            PrecioUnitario = d.PrecioUnitario,
            Total = Math.Round(d.Cantidad * d.PrecioUnitario, 2),
            CuentaContableId = d.CuentaContableId,
        }).ToList();
        orden.Detalles = detalles;

        var encontrada = await _ordenCompraRepository.ActualizarAsync(orden);
        if (!encontrada) return NotFound(new { message = "Orden de compra no encontrada" });

        return Ok(await Obtener(orden.Id, empresaId));
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        var ordenes = await _db.OrdenesCompra
            .Where(o => o.EmpresaId == empresaId)
            .Include(o => o.Proveedor)
            .Include(o => o.Comprobante)
            .Include(o => o.EmpleadoComprador)
            .Include(o => o.Detalles).ThenInclude(d => d.CuentaContable)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return Ok(ordenes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await Obtener(id, empresaId);
        if (orden is null) return NotFound(new { message = "Orden de compra no encontrada" });
        return Ok(orden);
    }

    /// <summary>Marca la orden como enviada al proveedor (para su seguimiento).</summary>
    [HttpPost("{id:guid}/enviar")]
    public async Task<IActionResult> Enviar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await _db.OrdenesCompra.FirstOrDefaultAsync(o => o.Id == id && o.EmpresaId == empresaId);
        if (orden is null) return NotFound(new { message = "Orden de compra no encontrada" });
        if (orden.Estado != EstadoOrdenCompra.BORRADOR) return BadRequest(new { message = "Solo se puede enviar una orden en estado BORRADOR" });
        orden.Estado = EstadoOrdenCompra.ENVIADA;
        orden.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(orden);
    }

    [HttpPost("{id:guid}/aprobar")]
    public async Task<IActionResult> Aprobar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await _db.OrdenesCompra.FirstOrDefaultAsync(o => o.Id == id && o.EmpresaId == empresaId);
        if (orden is null) return NotFound(new { message = "Orden de compra no encontrada" });
        if (orden.Estado != EstadoOrdenCompra.ENVIADA) return BadRequest(new { message = "Solo se puede aprobar una orden ENVIADA" });
        orden.Estado = EstadoOrdenCompra.APROBADA;
        orden.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(orden);
    }

    /// <summary>
    /// Registra la recepción (total o parcial) de mercadería: por cada línea recibida
    /// genera un registro de <see cref="Compra"/> (documento + cuenta por pagar) que,
    /// a su vez, ingresa el stock y el movimiento de Kardex correspondiente.
    /// </summary>
    [HttpPost("{id:guid}/recepcionar")]
    public async Task<IActionResult> Recepcionar(Guid id, RecibirOrdenCompraDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await _db.OrdenesCompra
            .Include(o => o.Detalles)
            .Include(o => o.Proveedor)
            .FirstOrDefaultAsync(o => o.Id == id && o.EmpresaId == empresaId);
        if (orden is null) return NotFound(new { message = "Orden de compra no encontrada" });
        if (orden.Estado is EstadoOrdenCompra.RECIBIDA or EstadoOrdenCompra.CANCELADA)
            return BadRequest(new { message = "La orden ya no admite recepciones" });

        await using var transaction = await _db.Database.BeginTransactionAsync();

        if (!Enum.TryParse<TipoDocumentoCompra>(dto.TipoDocumento, out var tipoDocumento))
            return BadRequest(new { message = "Tipo de documento SUNAT inválido" });
        if (!Enum.TryParse<TipoOperacionSunat>(dto.TipoOperacionSunat, out var tipoOperacion))
            return BadRequest(new { message = "Tipo de operación SUNAT inválido" });

        var centroCostoValido = await _db.CentrosCosto.AnyAsync(c => c.Id == dto.CentroCostoId && c.EmpresaId == empresaId && c.Activo);
        if (!centroCostoValido) return BadRequest(new { message = "El centro de costo/área indicado no existe o está inactivo" });

        if (dto.ConfiguracionCuentaOperacionId.HasValue)
        {
            var codigoOperacionValido = await _db.ConfiguracionesCuentaOperacion.AnyAsync(c =>
                c.Id == dto.ConfiguracionCuentaOperacionId && c.EmpresaId == empresaId && c.Activo
                && c.Movimiento == MovimientoOperacionContable.RECIBIDO);
            if (!codigoOperacionValido) return BadRequest(new { message = "El código de operación indicado no existe, está inactivo o no es de compras (RECIBIDO)" });
        }

        // Reconfirmación del comprador al momento de recepcionar/registrar la compra: si no se
        // indica explícitamente, se reutiliza el comprador ya asignado en la orden de compra.
        Guid? empleadoCompradorRecepcionId = orden.EmpleadoCompradorId;
        if (dto.EmpleadoCompradorId.HasValue)
        {
            var compradorRecepcion = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == dto.EmpleadoCompradorId && e.EmpresaId == empresaId);
            if (compradorRecepcion is null) return BadRequest(new { message = "El personal comprador indicado no existe" });
            if (!compradorRecepcion.EsComprador) return BadRequest(new { message = "El personal indicado no tiene el rol de Comprador" });
            if (!compradorRecepcion.Activo) return BadRequest(new { message = "El personal comprador indicado está inactivo" });
            empleadoCompradorRecepcionId = compradorRecepcion.Id;
        }

        var detallesRecibidos = new List<ComprasService.DetalleCompra>();
        foreach (var item in dto.Detalles)
        {
            var detalle = orden.Detalles.FirstOrDefault(d => d.Id == item.DetalleId);
            if (detalle is null) continue;
            var pendiente = detalle.Cantidad - detalle.CantidadRecibida;
            var cantidad = Math.Min(item.CantidadRecibida, pendiente);
            if (cantidad <= 0) continue;

            detalle.CantidadRecibida += cantidad;
            detallesRecibidos.Add(new ComprasService.DetalleCompra(detalle.ProductoId, cantidad, detalle.PrecioUnitario, item.PorcentajeDescuento ?? 0));
        }

        Compra? compra = null;
        if (detallesRecibidos.Count > 0)
        {
            var documento = new ComprasService.DatosDocumentoCompra(
                tipoDocumento, dto.Serie, dto.Numero, dto.FechaDocumento, dto.FechaVencimiento,
                dto.NumeroGuiaRemision, dto.Referencia, dto.LocalProveedor, tipoOperacion, dto.GlosaComprobante);
            compra = await _comprasService.RegistrarCompraAsync(
                empresaId, orden.Proveedor, dto.AlmacenId, dto.CentroCostoId, orden.Moneda,
                detallesRecibidos, orden.Id, documento, dto.TasaIgv, dto.FechaTipoCambio, dto.GlosaMovimiento, empleadoCompradorRecepcionId, dto.ConfiguracionCuentaOperacionId);
        }

        orden.Estado = orden.Detalles.All(d => d.CantidadRecibida >= d.Cantidad)
            ? EstadoOrdenCompra.RECIBIDA
            : EstadoOrdenCompra.RECIBIDA_PARCIAL;
        orden.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var resultado = await Obtener(orden.Id, empresaId);
        return Ok(new { orden = resultado, compra });
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await _db.OrdenesCompra.FirstOrDefaultAsync(o => o.Id == id && o.EmpresaId == empresaId);
        if (orden is null) return NotFound(new { message = "Orden de compra no encontrada" });
        if (orden.Estado is EstadoOrdenCompra.RECIBIDA) return BadRequest(new { message = "No se puede cancelar una orden ya recibida" });
        orden.Estado = EstadoOrdenCompra.CANCELADA;
        orden.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(orden);
    }

    private async Task<OrdenCompra?> Obtener(Guid id, Guid empresaId) =>
        await _db.OrdenesCompra
            .Where(o => o.Id == id && o.EmpresaId == empresaId)
            .Include(o => o.Proveedor)
            .Include(o => o.Comprobante)
            .Include(o => o.EmpleadoComprador)
            .Include(o => o.Detalles).ThenInclude(d => d.Producto)
            .Include(o => o.Detalles).ThenInclude(d => d.CuentaContable)
            .FirstOrDefaultAsync();

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> Pdf(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await Obtener(id, empresaId);
        if (orden is null) return NotFound(new { message = "Orden de compra no encontrada" });
        var empresa = await _db.Empresas.FirstAsync(e => e.Id == empresaId);
        var pdf = _pdfService.GenerarOrdenCompra(orden, empresa);
        return File(pdf, "application/pdf", $"{orden.Numero}.pdf");
    }

    [HttpPost("{id:guid}/enviar-correo")]
    public async Task<IActionResult> EnviarCorreo(Guid id, EnviarCorreoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var orden = await Obtener(id, empresaId);
        if (orden is null) return NotFound(new { message = "Orden de compra no encontrada" });

        var destino = dto.Para ?? orden.Proveedor.Email;
        if (string.IsNullOrWhiteSpace(destino)) return BadRequest(new { message = "No se indicó un correo destino y el proveedor no tiene uno registrado" });

        var empresa = await _db.Empresas.FirstAsync(e => e.Id == empresaId);
        var pdf = _pdfService.GenerarOrdenCompra(orden, empresa);
        try
        {
            await _emailService.SendAsync(
                destino, dto.Asunto ?? $"Orden de compra {orden.Numero}",
                dto.Mensaje ?? $"Adjuntamos la orden de compra {orden.Numero}.", pdf, $"{orden.Numero}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        return Ok(new { message = "Correo enviado" });
    }
}
