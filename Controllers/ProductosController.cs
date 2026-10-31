using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Siserp.Api.Common;
using Siserp.Api.Data;
using Siserp.Api.Domain.Ports;
using Siserp.Api.Dtos;
using Siserp.Api.Infrastructure.Authorization;
using Siserp.Api.Models;

namespace Siserp.Api.Controllers;

[Authorize]
[MenuPermiso("productos")]
[ApiController]
[Route("productos")]
public class ProductosController : ControllerBase
{
    private readonly SiserpDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IProductoRepository _productoRepository;

    public ProductosController(SiserpDbContext db, IWebHostEnvironment env, IProductoRepository productoRepository)
    {
        _db = db;
        _env = env;
        _productoRepository = productoRepository;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductoDto dto)
    {
        var empresaId = User.GetEmpresaId();
        var producto = new Producto
        {
            EmpresaId = empresaId,
            Codigo = dto.Codigo,
            CodigoAlternativo = dto.CodigoAlternativo,
            Descripcion = dto.Descripcion,
            DescripcionAlternativa = dto.DescripcionAlternativa,
            UnidadMedida = dto.UnidadMedida ?? "NIU",
            PrecioVenta = dto.PrecioVenta,
            PrecioLista = dto.PrecioLista,
            PrecioCosto = dto.PrecioCosto ?? 0,
            MonedaCompra = dto.MonedaCompra ?? "PEN",
            TipoExistencia = dto.TipoExistencia ?? "01",
            EsServicio = dto.EsServicio ?? false,
            AfectoIGV = dto.AfectoIGV ?? true,
            StockMinimo = dto.StockMinimo ?? 0,
            StockMaximo = dto.StockMaximo ?? 0,
            Observaciones = dto.Observaciones,
            CategoriaId = dto.CategoriaId,
            DescuentoMayoristaPct = dto.DescuentoMayoristaPct ?? 0,
            DescuentoSuperMayoristaPct = dto.DescuentoSuperMayoristaPct ?? 0,
            CuentaVentaId = dto.CuentaVentaId,
            CuentaCostoVentaId = dto.CuentaCostoVentaId,
            CuentaExistenciasId = dto.CuentaExistenciasId,
            CodigoBienServicio = dto.CodigoBienServicio,
        };

        if (dto.Componentes is { Count: > 0 })
        {
            var validacion = await ValidarComponentesAsync(dto.Componentes, empresaId, null);
            if (validacion is not null) return validacion;
        }

        // Alta del producto a través del stored procedure sp_Producto_Registrar.
        var creado = await _productoRepository.RegistrarAsync(producto);
        if (creado is null) return BadRequest(new { message = "No se pudo registrar el producto" });

        if (dto.Componentes is { Count: > 0 })
        {
            // La composición (BOM) vive en una tabla aparte (ProductoComponente); se gestiona con
            // EF Core ya que no forma parte del alta simple del producto.
            foreach (var c in dto.Componentes)
            {
                _db.Set<ProductoComponente>().Add(new ProductoComponente { ProductoTerminadoId = creado.Id, InsumoId = c.InsumoId, Cantidad = c.Cantidad });
            }
            await _db.SaveChangesAsync();
        }

        return Ok(creado);
    }

    /// <summary>
    /// Valida que los insumos de la composición (BOM) existan en la empresa y que un producto
    /// terminado no se incluya a sí mismo como su propio insumo.
    /// </summary>
    private async Task<IActionResult?> ValidarComponentesAsync(List<ComponenteProductoDto> componentes, Guid empresaId, Guid? productoTerminadoId)
    {
        if (componentes.Any(c => c.Cantidad <= 0))
            return BadRequest(new { message = "La cantidad de cada insumo de la composición debe ser mayor a cero" });
        if (productoTerminadoId.HasValue && componentes.Any(c => c.InsumoId == productoTerminadoId.Value))
            return BadRequest(new { message = "Un producto no puede formar parte de su propia composición" });

        var insumoIds = componentes.Select(c => c.InsumoId).Distinct().ToList();
        var existentes = await _db.Productos.CountAsync(p => insumoIds.Contains(p.Id) && p.EmpresaId == empresaId);
        if (existentes != insumoIds.Count)
            return BadRequest(new { message = "Uno o más insumos de la composición no existen para esta empresa" });

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> FindAll([FromQuery] bool incluirInactivos = false)
    {
        var empresaId = User.GetEmpresaId();
        var productos = await _db.Productos
            .Where(p => p.EmpresaId == empresaId && (incluirInactivos || p.Activo))
            .Include(p => p.Stocks)
            .Include(p => p.Categoria)
            .Include(p => p.Componentes).ThenInclude(c => c.Insumo)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return Ok(productos);
    }

    /// <summary>Busca un producto por su código exacto (código de barras / código alternativo), para registro rápido por escáner.</summary>
    [HttpGet("por-codigo/{codigo}")]
    public async Task<IActionResult> FindPorCodigo(string codigo)
    {
        var empresaId = User.GetEmpresaId();
        var producto = await _db.Productos
            .Include(p => p.Stocks)
            .FirstOrDefaultAsync(p => p.EmpresaId == empresaId && p.Activo &&
                (p.Codigo == codigo || p.CodigoAlternativo == codigo));
        if (producto is null) return NotFound(new { message = "No se encontró un producto con ese código" });
        return Ok(producto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> FindOne(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var producto = await _db.Productos
            .Where(p => p.Id == id && p.EmpresaId == empresaId)
            .Include(p => p.Stocks).ThenInclude(s => s.Almacen)
            .Include(p => p.Componentes).ThenInclude(c => c.Insumo)
            .FirstOrDefaultAsync();
        if (producto is null) return NotFound(new { message = "Producto no encontrado" });
        return Ok(producto);
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateProductoDto dto)
    {
        var empresaId = User.GetEmpresaId();

        // Base para el merge de campos (no rastreada por EF: la escritura real del renglón
        // principal ocurre vía stored procedure, no vía SaveChanges de este contexto).
        var productoBase = await _db.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (productoBase is null) return NotFound(new { message = "Producto no encontrado" });

        if (dto.Codigo is not null) productoBase.Codigo = dto.Codigo;
        if (dto.CodigoAlternativo is not null) productoBase.CodigoAlternativo = dto.CodigoAlternativo;
        if (dto.Descripcion is not null) productoBase.Descripcion = dto.Descripcion;
        if (dto.DescripcionAlternativa is not null) productoBase.DescripcionAlternativa = dto.DescripcionAlternativa;
        if (dto.UnidadMedida is not null) productoBase.UnidadMedida = dto.UnidadMedida;
        if (dto.PrecioVenta is not null) productoBase.PrecioVenta = dto.PrecioVenta.Value;
        if (dto.PrecioLista is not null) productoBase.PrecioLista = dto.PrecioLista.Value;
        if (dto.PrecioCosto is not null) productoBase.PrecioCosto = dto.PrecioCosto.Value;
        if (dto.MonedaCompra is not null) productoBase.MonedaCompra = dto.MonedaCompra;
        if (dto.TipoExistencia is not null) productoBase.TipoExistencia = dto.TipoExistencia;
        if (dto.EsServicio is not null) productoBase.EsServicio = dto.EsServicio.Value;
        if (dto.AfectoIGV is not null) productoBase.AfectoIGV = dto.AfectoIGV.Value;
        if (dto.StockMinimo is not null) productoBase.StockMinimo = dto.StockMinimo.Value;
        if (dto.StockMaximo is not null) productoBase.StockMaximo = dto.StockMaximo.Value;
        if (dto.Observaciones is not null) productoBase.Observaciones = dto.Observaciones;
        if (dto.CategoriaId is not null) productoBase.CategoriaId = dto.CategoriaId;
        if (dto.DescuentoMayoristaPct is not null) productoBase.DescuentoMayoristaPct = dto.DescuentoMayoristaPct.Value;
        if (dto.DescuentoSuperMayoristaPct is not null) productoBase.DescuentoSuperMayoristaPct = dto.DescuentoSuperMayoristaPct.Value;
        if (dto.CuentaVentaId is not null) productoBase.CuentaVentaId = dto.CuentaVentaId;
        if (dto.CuentaCostoVentaId is not null) productoBase.CuentaCostoVentaId = dto.CuentaCostoVentaId;
        if (dto.CuentaExistenciasId is not null) productoBase.CuentaExistenciasId = dto.CuentaExistenciasId;
        if (dto.CodigoBienServicio is not null) productoBase.CodigoBienServicio = dto.CodigoBienServicio;

        if (dto.Componentes is { Count: > 0 })
        {
            var validacion = await ValidarComponentesAsync(dto.Componentes, empresaId, productoBase.Id);
            if (validacion is not null) return validacion;
        }

        // Modificación del renglón principal a través del stored procedure sp_Producto_Modificar.
        var actualizado = await _productoRepository.ModificarAsync(
            productoBase, categoriaIdSet: dto.CategoriaId is not null,
            cuentaVentaIdSet: dto.CuentaVentaId is not null,
            cuentaCostoVentaIdSet: dto.CuentaCostoVentaId is not null,
            cuentaExistenciasIdSet: dto.CuentaExistenciasId is not null,
            codigoBienServicioSet: dto.CodigoBienServicio is not null);
        if (actualizado is null) return NotFound(new { message = "Producto no encontrado" });

        if (dto.Componentes is not null)
        {
            // La composición (BOM) vive en una tabla aparte (ProductoComponente) y se gestiona con
            // EF Core; se reintenta si dos guardados casi simultáneos (p. ej. doble clic o dos
            // pestañas) provocan que la composición ya haya sido reemplazada por la otra solicitud.
            const int maxIntentos = 3;
            for (var intento = 0; intento < maxIntentos; intento++)
            {
                var componentesActuales = await _db.Set<ProductoComponente>()
                    .Where(c => c.ProductoTerminadoId == id).ToListAsync();

                // En vez de vaciar y recrear todas las filas (lo que fuerza un DELETE+INSERT de cada
                // componente aunque no haya cambiado, ampliando la ventana para un choque de concurrencia),
                // se actualiza/agrega/elimina solo lo que realmente cambió.
                var nuevos = dto.Componentes.ToDictionary(c => c.InsumoId, c => c.Cantidad);
                foreach (var existente in componentesActuales.ToList())
                {
                    if (nuevos.TryGetValue(existente.InsumoId, out var cantidad))
                    {
                        existente.Cantidad = cantidad;
                        nuevos.Remove(existente.InsumoId);
                    }
                    else
                    {
                        _db.Set<ProductoComponente>().Remove(existente);
                    }
                }
                foreach (var (insumoId, cantidad) in nuevos)
                {
                    _db.Set<ProductoComponente>().Add(new ProductoComponente { ProductoTerminadoId = id, InsumoId = insumoId, Cantidad = cantidad });
                }

                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    _db.ChangeTracker.Clear();
                    if (intento < maxIntentos - 1)
                    {
                        await Task.Delay(50 * (intento + 1));
                        continue;
                    }
                    return Conflict(new { message = "El producto fue modificado por otra solicitud mientras se guardaba. Vuelve a intentarlo." });
                }
                break;
            }
        }

        return Ok(actualizado);
    }

    /// <summary>Sube/reemplaza la foto del producto; se guarda en wwwroot/uploads/productos y se sirve como archivo estático.</summary>
    [HttpPost("{id:guid}/foto")]
    public async Task<IActionResult> SubirFoto(Guid id, IFormFile foto)
    {
        var empresaId = User.GetEmpresaId();
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (producto is null) return NotFound(new { message = "Producto no encontrado" });
        if (foto is null || foto.Length == 0) return BadRequest(new { message = "Debe adjuntar un archivo de imagen" });

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(foto.FileName).ToLowerInvariant();
        if (!extensionesPermitidas.Contains(extension))
        {
            return BadRequest(new { message = "Formato de imagen no soportado (use jpg, png, webp o gif)" });
        }

        var carpeta = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "productos");
        Directory.CreateDirectory(carpeta);
        var nombreArchivo = $"{producto.Id}{extension}";
        var rutaFisica = Path.Combine(carpeta, nombreArchivo);
        await using (var stream = System.IO.File.Create(rutaFisica))
        {
            await foto.CopyToAsync(stream);
        }

        producto.FotoUrl = $"/uploads/productos/{nombreArchivo}";
        producto.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(producto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        // Baja lógica a través de sp_Producto_EliminarLogico: se conserva el histórico de
        // ventas/compras asociado.
        var producto = await _productoRepository.EliminarLogicoAsync(id, empresaId);
        if (producto is null) return NotFound(new { message = "Producto no encontrado" });
        return Ok(producto);
    }

    /// <summary>Reactiva un producto dado de baja lógicamente.</summary>
    [HttpPut("{id:guid}/reactivar")]
    public async Task<IActionResult> Reactivar(Guid id)
    {
        var empresaId = User.GetEmpresaId();
        var producto = await _db.Productos.FirstOrDefaultAsync(p => p.Id == id && p.EmpresaId == empresaId);
        if (producto is null) return NotFound(new { message = "Producto no encontrado" });
        producto.Activo = true;
        producto.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(producto);
    }
}
