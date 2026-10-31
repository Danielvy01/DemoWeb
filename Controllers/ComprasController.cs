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
[MenuPermiso("compras")]
[ApiController]
[Route("compras")]
public class ComprasController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly ComprasService _comprasService;
    private readonly AutorizacionAdminService _autorizacionAdminService;
    private readonly ILogger<ComprasController> _logger;
    private readonly IWebHostEnvironment _env;

    public ComprasController(
        SiserpDbContext db, ComprasService comprasService, AutorizacionAdminService autorizacionAdminService,
        ILogger<ComprasController> logger, IWebHostEnvironment env)
    {
        _db = db;
        _comprasService = comprasService;
        _autorizacionAdminService = autorizacionAdminService;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// Registra la compra (documento + cuenta por pagar) correspondiente a una orden de
    /// compra ya registrada y aprobada, indicando los datos del comprobante SUNAT recibido
    /// del proveedor. Ingresa stock y genera el movimiento de Kardex de cada línea.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateCompraDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var proveedor = await _db.Proveedores.FirstOrDefaultAsync(p => p.Id == dto.ProveedorId && p.EmpresaId == empresaId);
        if (proveedor is null) return BadRequest(new { message = "Proveedor no encontrado" });
        if (proveedor.Bloqueado) return BadRequest(new { message = "El proveedor está bloqueado y no admite nuevas compras" });
        if (!proveedor.Activo) return BadRequest(new { message = "El proveedor está inactivo" });

        var orden = await _db.OrdenesCompra.FirstOrDefaultAsync(o => o.Id == dto.OrdenCompraId && o.EmpresaId == empresaId);
        if (orden is null) return BadRequest(new { message = "Toda compra debe registrarse a partir de una orden de compra existente" });
        if (orden.Estado is EstadoOrdenCompra.BORRADOR or EstadoOrdenCompra.CANCELADA)
            return BadRequest(new { message = "La orden de compra debe estar aprobada/enviada para registrar la compra" });

        var centroCostoValido = await _db.CentrosCosto.AnyAsync(c => c.Id == dto.CentroCostoId && c.EmpresaId == empresaId && c.Activo);
        if (!centroCostoValido) return BadRequest(new { message = "El centro de costo/área indicado no existe o está inactivo" });

        if (dto.ConfiguracionCuentaOperacionId.HasValue)
        {
            var codigoOperacionValido = await _db.ConfiguracionesCuentaOperacion.AnyAsync(c =>
                c.Id == dto.ConfiguracionCuentaOperacionId && c.EmpresaId == empresaId && c.Activo
                && c.Movimiento == MovimientoOperacionContable.RECIBIDO);
            if (!codigoOperacionValido) return BadRequest(new { message = "El código de operación indicado no existe, está inactivo o no es de compras (RECIBIDO)" });
        }

        // Reconfirmación del comprador: quien registra la compra debe reafirmar/indicar qué
        // personal con rol de Comprador la realizó, aun si difiere del indicado en la orden de compra.
        var comprador = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == dto.EmpleadoCompradorId && e.EmpresaId == empresaId);
        if (comprador is null) return BadRequest(new { message = "El personal comprador indicado no existe" });
        if (!comprador.EsComprador) return BadRequest(new { message = "El personal indicado no tiene el rol de Comprador" });
        if (!comprador.Activo) return BadRequest(new { message = "El personal comprador indicado está inactivo" });

        if (!Enum.TryParse<TipoDocumentoCompra>(dto.TipoDocumento, out var tipoDocumento))
            return BadRequest(new { message = "Tipo de documento SUNAT inválido" });
        if (!Enum.TryParse<TipoOperacionSunat>(dto.TipoOperacionSunat, out var tipoOperacion))
            return BadRequest(new { message = "Tipo de operación SUNAT inválido" });

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var documento = new ComprasService.DatosDocumentoCompra(
            tipoDocumento, dto.Serie, dto.Numero, dto.FechaDocumento, dto.FechaVencimiento,
            dto.NumeroGuiaRemision, dto.Referencia, dto.LocalProveedor, tipoOperacion, dto.GlosaComprobante);

        var compra = await _comprasService.RegistrarCompraAsync(
            empresaId, proveedor, dto.AlmacenId, dto.CentroCostoId, dto.Moneda ?? "PEN",
            dto.Detalles.Select(d => new ComprasService.DetalleCompra(d.ProductoId, d.Cantidad, d.PrecioUnitario, d.PorcentajeDescuento ?? 0, d.CuentaContableId)).ToList(),
            orden.Id, documento, dto.TasaIgv, dto.FechaTipoCambio, dto.GlosaMovimiento, comprador.Id, dto.ConfiguracionCuentaOperacionId);

        await transaction.CommitAsync();

        var resultado = await _db.Compras.Include(c => c.Detalles).ThenInclude(d => d.CuentaContable).Include(c => c.CentroCosto).Include(c => c.EmpleadoComprador)
            .Include(c => c.ConfiguracionCuentaOperacion).ThenInclude(o => o!.CuentaContable).FirstAsync(c => c.Id == compra.Id);
        return Ok(resultado);
    }

    /// <summary>
    /// Edita una compra ya registrada (datos del comprobante, centro de costo/comprador e
    /// ítems). Solo se permite mientras no esté pagada; al guardar se revierte el
    /// stock/Kardex previamente ingresado y se vuelve a ingresar con los datos nuevos.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCompraDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var compra = await _db.Compras
            .Include(c => c.Detalles)
            .Include(c => c.Proveedor)
            .FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (compra is null) return NotFound(new { message = "Compra no encontrada" });
        if (compra.Pagado) return BadRequest(new { message = "No se puede editar una compra que ya fue pagada" });
        if (dto.Detalles.Count == 0) return BadRequest(new { message = "La compra debe tener al menos un ítem" });

        var centroCostoValido = await _db.CentrosCosto.AnyAsync(c => c.Id == dto.CentroCostoId && c.EmpresaId == empresaId && c.Activo);
        if (!centroCostoValido) return BadRequest(new { message = "El centro de costo/área indicado no existe o está inactivo" });

        var almacenValido = await _db.Almacenes.AnyAsync(a => a.Id == dto.AlmacenId && a.EmpresaId == empresaId);
        if (!almacenValido) return BadRequest(new { message = "El almacén indicado no existe" });

        if (dto.ConfiguracionCuentaOperacionId.HasValue)
        {
            var codigoOperacionValido = await _db.ConfiguracionesCuentaOperacion.AnyAsync(c =>
                c.Id == dto.ConfiguracionCuentaOperacionId && c.EmpresaId == empresaId && c.Activo
                && c.Movimiento == MovimientoOperacionContable.RECIBIDO);
            if (!codigoOperacionValido) return BadRequest(new { message = "El código de operación indicado no existe, está inactivo o no es de compras (RECIBIDO)" });
        }

        var comprador = await _db.Empleados.FirstOrDefaultAsync(e => e.Id == dto.EmpleadoCompradorId && e.EmpresaId == empresaId);
        if (comprador is null) return BadRequest(new { message = "El personal comprador indicado no existe" });
        if (!comprador.EsComprador) return BadRequest(new { message = "El personal indicado no tiene el rol de Comprador" });
        if (!comprador.Activo) return BadRequest(new { message = "El personal comprador indicado está inactivo" });

        if (!Enum.TryParse<TipoDocumentoCompra>(dto.TipoDocumento, out var tipoDocumento))
            return BadRequest(new { message = "Tipo de documento SUNAT inválido" });
        if (!Enum.TryParse<TipoOperacionSunat>(dto.TipoOperacionSunat, out var tipoOperacion))
            return BadRequest(new { message = "Tipo de operación SUNAT inválido" });

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var documento = new ComprasService.DatosDocumentoCompra(
            tipoDocumento, dto.Serie, dto.Numero, dto.FechaDocumento, dto.FechaVencimiento,
            dto.NumeroGuiaRemision, dto.Referencia, dto.LocalProveedor, tipoOperacion, dto.GlosaComprobante);

        try
        {
            await _comprasService.ActualizarCompraAsync(
                compra, dto.AlmacenId, dto.CentroCostoId, dto.Moneda ?? "PEN",
                dto.Detalles.Select(d => new ComprasService.DetalleCompra(d.ProductoId, d.Cantidad, d.PrecioUnitario, d.PorcentajeDescuento ?? 0, d.CuentaContableId)).ToList(),
                documento, compra.Proveedor.DiasCredito, dto.TasaIgv, dto.FechaTipoCambio, dto.GlosaMovimiento, comprador.Id, dto.ConfiguracionCuentaOperacionId);

            await transaction.CommitAsync();
        }
        catch (CompraNoEncontradaException ex)
        {
            // La compra (cabecera/ítems, grabados vía stored procedure) ya no existe o fue
            // modificada/eliminada por otra solicitud entre la lectura y el guardado (p. ej.
            // doble clic en "Guardar" o edición concurrente). Se registra en el log para
            // poder auditar ediciones que no llegan a grabarse y se revierte la transacción
            // en vez de dejar la excepción sin controlar (500).
            await transaction.RollbackAsync();
            _logger.LogWarning(ex,
                "No se pudo grabar la edición de la compra {CompraId} (empresa {EmpresaId}, usuario {UsuarioId}): conflicto de concurrencia, los datos pudieron cambiar desde que se cargó el formulario.",
                id, empresaId, User.GetUsuarioId());
            return Conflict(new { message = "La compra fue modificada por otra operación mientras se editaba. Recargue la compra e intente nuevamente." });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Conflicto de concurrencia proveniente del ajuste de stock/Kardex (que sigue
            // sobre EF Core), distinto del de la cabecera/ítems de la compra (ver catch anterior).
            await transaction.RollbackAsync();
            _logger.LogWarning(ex,
                "No se pudo grabar la edición de la compra {CompraId} (empresa {EmpresaId}, usuario {UsuarioId}): conflicto de concurrencia, los datos pudieron cambiar desde que se cargó el formulario.",
                id, empresaId, User.GetUsuarioId());
            return Conflict(new { message = "La compra fue modificada por otra operación mientras se editaba. Recargue la compra e intente nuevamente." });
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex,
                "Error al grabar la edición de la compra {CompraId} (empresa {EmpresaId}, usuario {UsuarioId}).",
                id, empresaId, User.GetUsuarioId());
            throw;
        }

        _logger.LogInformation(
            "Compra {CompraId} (empresa {EmpresaId}) editada por el usuario {UsuarioId}.",
            id, empresaId, User.GetUsuarioId());

        var resultadoActualizado = await _db.Compras.Include(c => c.Detalles).ThenInclude(d => d.CuentaContable).Include(c => c.CentroCosto).Include(c => c.EmpleadoComprador)
            .Include(c => c.ConfiguracionCuentaOperacion).ThenInclude(o => o!.CuentaContable).FirstAsync(c => c.Id == compra.Id);
        return Ok(resultadoActualizado);
    }

    [HttpGet]
    public async Task<IActionResult> FindAll()
    {
        var empresaId = User.GetEmpresaId();
        var compras = await _db.Compras
            .Where(c => c.EmpresaId == empresaId)
            .Include(c => c.Proveedor)
            .Include(c => c.OrdenCompra)
            .Include(c => c.CentroCosto)
            .Include(c => c.EmpleadoComprador)
            .Include(c => c.ConfiguracionCuentaOperacion).ThenInclude(o => o!.CuentaContable)
            .Include(c => c.Detalles).ThenInclude(d => d.CuentaContable)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return Ok(compras);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var compra = await _db.Compras
            .Where(c => c.Id == id && c.EmpresaId == empresaId)
            .Include(c => c.Proveedor)
            .Include(c => c.EmpleadoComprador)
            .Include(c => c.ConfiguracionCuentaOperacion).ThenInclude(o => o!.CuentaContable)
            .Include(c => c.Detalles).ThenInclude(d => d.Producto)
            .Include(c => c.Detalles).ThenInclude(d => d.CuentaContable)
            .FirstOrDefaultAsync();
        if (compra is null) return NotFound(new { message = "Compra no encontrada" });
        return Ok(compra);
    }

    /// <summary>
    /// Sube/reemplaza la foto del comprobante físico recibido del proveedor, como respaldo
    /// adicional opcional al registro digital de la compra; se guarda en
    /// wwwroot/uploads/compras y se sirve como archivo estático.
    /// </summary>
    [HttpPost("{id:guid}/foto")]
    public async Task<IActionResult> SubirFotoComprobante(Guid id, IFormFile foto)
    {
        var empresaId = User.GetEmpresaId();
        var compra = await _db.Compras.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (compra is null) return NotFound(new { message = "Compra no encontrada" });
        if (foto is null || foto.Length == 0) return BadRequest(new { message = "Debe adjuntar un archivo de imagen" });

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(foto.FileName).ToLowerInvariant();
        if (!extensionesPermitidas.Contains(extension))
        {
            return BadRequest(new { message = "Formato de imagen no soportado (use jpg, png, webp o gif)" });
        }

        var carpeta = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "compras");
        Directory.CreateDirectory(carpeta);
        var nombreArchivo = $"{compra.Id}{extension}";
        var rutaFisica = Path.Combine(carpeta, nombreArchivo);
        await using (var stream = System.IO.File.Create(rutaFisica))
        {
            await foto.CopyToAsync(stream);
        }

        compra.ComprobanteFotoUrl = $"/uploads/compras/{nombreArchivo}";
        compra.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(compra);
    }

    /// <summary>Quita la foto del comprobante adjuntada a la compra (el adjunto es opcional).</summary>
    [HttpDelete("{id:guid}/foto")]
    public async Task<IActionResult> QuitarFotoComprobante(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var compra = await _db.Compras.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (compra is null) return NotFound(new { message = "Compra no encontrada" });
        if (compra.ComprobanteFotoUrl is not null)
        {
            var rutaFisica = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), compra.ComprobanteFotoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(rutaFisica)) System.IO.File.Delete(rutaFisica);
            compra.ComprobanteFotoUrl = null;
            compra.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return Ok(compra);
    }

    /// <summary>Registra el pago de la cuenta por pagar al proveedor.</summary>
    [HttpPost("{id:guid}/marcar-pagado")]
    public async Task<IActionResult> MarcarPagado(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var compra = await _db.Compras.FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (compra is null) return NotFound(new { message = "Compra no encontrada" });
        compra.Pagado = true;
        compra.FechaPago = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(compra);
    }

    /// <summary>
    /// Anula (baja lógica) una compra registrada: revierte el stock/Kardex ingresado y anula
    /// el asiento contable generado. No se elimina el registro, para conservar el histórico.
    /// Requiere, al estilo CONCAR, el código (credenciales) de un usuario Administrador de la
    /// empresa para autorizar la operación.
    /// </summary>
    [HttpPost("{id:guid}/anular")]
    public async Task<IActionResult> Anular(Guid id, AnularCompraDto dto)
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

        var compra = await _db.Compras.Include(c => c.Detalles).FirstOrDefaultAsync(c => c.Id == id && c.EmpresaId == empresaId);
        if (compra is null) return NotFound(new { message = "Compra no encontrada" });
        if (compra.Anulado) return BadRequest(new { message = "La compra ya está anulada" });
        if (compra.Pagado) return BadRequest(new { message = "No se puede anular una compra que ya fue pagada" });

        await using var transaction = await _db.Database.BeginTransactionAsync();
        await _comprasService.AnularCompraAsync(compra, dto.Motivo, User.GetUsuarioId());
        await transaction.CommitAsync();

        var resultado = await _db.Compras.FirstAsync(c => c.Id == id);
        return Ok(resultado);
    }
}
