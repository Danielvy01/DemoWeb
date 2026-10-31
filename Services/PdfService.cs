using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

/// <summary>
/// Genera la representación impresa (PDF) de los documentos comerciales:
/// cotizaciones, guías de remisión, comprobantes (factura/boleta), órdenes
/// de compra y órdenes de trabajo. Usa QuestPDF (licencia Community).
/// </summary>
public class PdfService
{
    private readonly IWebHostEnvironment _env;

    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public PdfService(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>Lee el archivo de logo de la empresa desde wwwroot, si existe.</summary>
    private byte[]? LeerLogo(Empresa empresa)
    {
        if (string.IsNullOrWhiteSpace(empresa.LogoUrl)) return null;
        var raiz = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var rutaFisica = Path.Combine(raiz, empresa.LogoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        return System.IO.File.Exists(rutaFisica) ? System.IO.File.ReadAllBytes(rutaFisica) : null;
    }

    public byte[] GenerarCotizacion(Cotizacion c, Empresa empresa)
    {
        return Construir(empresa, "COTIZACIÓN", c.Numero, c.FechaEmision, c.Moneda, c.Total,
            $"Cliente: {c.Cliente.NombreORazonSocial} - {c.Cliente.NumeroDocumento}",
            c.Detalles.Select(d => (d.Producto.Descripcion, d.Cantidad, (decimal?)d.PrecioUnitario, (decimal?)d.Total)),
            new[] { ("Validez", c.FechaValidez?.ToString("dd/MM/yyyy") ?? "-"), ("Estado", c.Estado.ToString()) },
            c.Observaciones);
    }

    public byte[] GenerarGuiaRemision(GuiaRemision g, Empresa empresa)
    {
        var destinatario = g.Cliente is not null
            ? $"Destinatario: {g.Cliente.NombreORazonSocial} - {g.Cliente.NumeroDocumento}"
            : "Traslado entre almacenes propios";
        return Construir(empresa, "GUÍA DE REMISIÓN", g.Numero, g.FechaEmision, "PEN", null,
            destinatario,
            g.Detalles.Select(d => (d.Producto.Descripcion, d.Cantidad, (decimal?)null, (decimal?)null)),
            new[]
            {
                ("Almacén origen", g.AlmacenOrigen.Nombre),
                ("Almacén destino", g.AlmacenDestino?.Nombre ?? "-"),
                ("Motivo de traslado", g.MotivoTraslado),
                ("Punto de partida", g.DireccionPartida ?? "-"),
                ("Punto de llegada", g.DireccionLlegada ?? "-"),
                ("Transportista", g.Transportista ?? "-"),
                ("Placa", g.PlacaVehiculo ?? "-"),
                ("Estado", g.Estado.ToString()),
            },
            g.Observaciones);
    }

    public byte[] GenerarComprobante(Comprobante c, Empresa empresa)
    {
        return Construir(empresa, c.TipoComprobante.ToString(), $"{c.Serie}-{c.Correlativo:D8}", c.FechaEmision, c.Moneda, c.Total,
            $"Cliente: {c.Cliente.NombreORazonSocial} - {c.Cliente.NumeroDocumento}",
            c.Detalles.Select(d => (d.Producto.Descripcion, d.Cantidad, (decimal?)d.PrecioUnitario, (decimal?)d.Total)),
            new[]
            {
                ("Op. gravada", c.OpGravada.ToString("0.00")),
                ("IGV", c.Igv.ToString("0.00")),
                ("Estado", c.Estado.ToString()),
            },
            c.Observaciones);
    }

    /// <summary>Genera el comprobante en formato compacto para ticketera térmica de 80mm.</summary>
    public byte[] GenerarComprobanteTicket80mm(Comprobante c, Empresa empresa)
    {
        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(80, 200, Unit.Millimetre);
                page.MarginVertical(8);
                page.MarginHorizontal(4);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Text(empresa.RazonSocial).Bold().FontSize(10);
                    col.Item().AlignCenter().Text($"RUC: {empresa.Ruc}");
                    if (!string.IsNullOrWhiteSpace(empresa.Direccion)) col.Item().AlignCenter().Text(empresa.Direccion!);
                    col.Item().PaddingTop(4).AlignCenter().Text(c.TipoComprobante.ToString().Replace('_', ' ')).Bold();
                    col.Item().AlignCenter().Text($"{c.Serie}-{c.Correlativo:D8}").Bold();
                    col.Item().PaddingTop(2).LineHorizontal(0.5f);

                    col.Item().Text($"Fecha: {c.FechaEmision:dd/MM/yyyy HH:mm}");
                    col.Item().Text($"Cliente: {c.Cliente.NombreORazonSocial}");
                    col.Item().Text($"Doc: {c.Cliente.NumeroDocumento}");
                    if (c.Vendedor is not null) col.Item().Text($"Vendedor: {c.Vendedor.Nombre}");
                    col.Item().PaddingTop(2).LineHorizontal(0.5f);

                    foreach (var d in c.Detalles)
                    {
                        col.Item().Text(d.Producto.Descripcion).FontSize(8);
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"{d.Cantidad:0.##} x {d.PrecioUnitario:0.00}");
                            row.ConstantItem(50).Text(t => { t.AlignRight(); t.Span(d.Total.ToString("0.00")); });
                        });
                    }
                    col.Item().PaddingTop(2).LineHorizontal(0.5f);

                    col.Item().Row(row => { row.RelativeItem().Text("Op. gravada"); row.ConstantItem(50).Text(t => { t.AlignRight(); t.Span(c.OpGravada.ToString("0.00")); }); });
                    if (c.OpInafecta > 0)
                        col.Item().Row(row => { row.RelativeItem().Text("Op. inafecta"); row.ConstantItem(50).Text(t => { t.AlignRight(); t.Span(c.OpInafecta.ToString("0.00")); }); });
                    if (c.OpExonerada > 0)
                        col.Item().Row(row => { row.RelativeItem().Text("Op. exonerada"); row.ConstantItem(50).Text(t => { t.AlignRight(); t.Span(c.OpExonerada.ToString("0.00")); }); });
                    col.Item().Row(row => { row.RelativeItem().Text("IGV (18%)"); row.ConstantItem(50).Text(t => { t.AlignRight(); t.Span(c.Igv.ToString("0.00")); }); });
                    col.Item().PaddingTop(2).Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL").Bold();
                        row.ConstantItem(50).Text(t => { t.AlignRight(); t.Span($"{c.Moneda} {c.Total:0.00}"); t.DefaultTextStyle(s => s.Bold()); });
                    });

                    if (c.Pagos.Count > 0)
                    {
                        col.Item().PaddingTop(2).LineHorizontal(0.5f);
                        col.Item().Text("Pagos:").SemiBold();
                        foreach (var p in c.Pagos)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text(p.MetodoPago.ToString());
                                row.ConstantItem(50).Text(t => { t.AlignRight(); t.Span(p.Monto.ToString("0.00")); });
                            });
                        }
                    }
                    else if (c.FechaVencimiento is not null)
                    {
                        col.Item().PaddingTop(2).Text($"Vence: {c.FechaVencimiento:dd/MM/yyyy}").Bold();
                    }

                    if (!string.IsNullOrWhiteSpace(c.Observaciones))
                    {
                        col.Item().PaddingTop(4).Text(c.Observaciones!).FontSize(7);
                    }

                    col.Item().PaddingTop(6).AlignCenter().Text("Gracias por su compra").FontSize(7);
                });
            });
        });

        return documento.GeneratePdf();
    }

    public byte[] GenerarOrdenCompra(OrdenCompra o, Empresa empresa)
    {
        var entrega = o.TipoEntrega == TipoEntregaOrdenCompra.INMEDIATA
            ? "Inmediata"
            : $"A {o.PlazoEntregaDias} día(s)";
        return Construir(empresa, "ORDEN DE COMPRA", o.Numero, o.CreatedAt, o.Moneda, o.Total,
            $"Proveedor: {o.Proveedor.RazonSocial} - {o.Proveedor.Ruc}",
            o.Detalles.Select(d => (d.Producto.Descripcion, d.Cantidad, (decimal?)d.PrecioUnitario, (decimal?)d.Total)),
            new[]
            {
                ("Comprador", o.EmpleadoComprador is not null ? $"{o.EmpleadoComprador.Nombres} {o.EmpleadoComprador.Apellidos}" : "-"),
                ("Tipo de entrega", entrega),
                ("Fecha entrega esperada", o.FechaEntregaEsperada?.ToString("dd/MM/yyyy") ?? "-"),
                ("Estado", o.Estado.ToString()),
            },
            o.Observaciones);
    }

    public byte[] GenerarOrdenTrabajo(OrdenTrabajo o, Empresa empresa)
    {
        return Construir(empresa, "ORDEN DE TRABAJO", o.Numero, o.FechaInicio, "-", null,
            $"Producto a fabricar: {o.Producto.Descripcion} (cantidad planificada: {o.CantidadPlanificada})",
            o.Insumos.Select(i => (i.Producto.Descripcion, i.CantidadRequerida, (decimal?)null, (decimal?)null)),
            new[]
            {
                ("Almacén", o.Almacen.Nombre),
                ("Cantidad producida", o.CantidadProducida.ToString("0.00")),
                ("Estado", o.Estado.ToString()),
            },
            o.Observaciones);
    }

    private byte[] Construir(
        Empresa empresa, string titulo, string numero, DateTime fecha, string? moneda, decimal? total,
        string referencia, IEnumerable<(string Descripcion, decimal Cantidad, decimal? PrecioUnitario, decimal? Total)> items,
        (string Label, string Valor)[] datosAdicionales, string? observaciones)
    {
        var lista = items.ToList();
        var conMontos = lista.Any(i => i.PrecioUnitario is not null);
        var logo = LeerLogo(empresa);

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (logo is not null)
                        {
                            row.ConstantItem(60).Height(60).PaddingRight(8).Image(logo).FitArea();
                        }
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(empresa.RazonSocial).Bold().FontSize(14);
                            c.Item().Text($"RUC: {empresa.Ruc}");
                            if (!string.IsNullOrWhiteSpace(empresa.Direccion)) c.Item().Text(empresa.Direccion!);
                        });
                        row.ConstantItem(180).Column(c =>
                        {
                            c.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(inner =>
                            {
                                inner.Item().Text(titulo).Bold().FontSize(12).AlignCenter();
                                inner.Item().Text(numero).Bold().AlignCenter();
                            });
                            c.Item().PaddingTop(4).Text($"Fecha: {fecha:dd/MM/yyyy}");
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingTop(10).Column(col =>
                {
                    col.Item().Text(referencia).SemiBold();

                    if (datosAdicionales.Length > 0)
                    {
                        col.Item().PaddingTop(4).Row(row =>
                        {
                            foreach (var (label, valor) in datosAdicionales)
                            {
                                row.RelativeItem().Text(t =>
                                {
                                    t.Span($"{label}: ").SemiBold();
                                    t.Span(valor);
                                });
                            }
                        });
                    }

                    col.Item().PaddingTop(12).Table(table =>
                    {
                        // Las columnas de cantidad/montos usan ancho fijo (suficiente para sus
                        // encabezados y valores numéricos) y la descripción absorbe el espacio
                        // restante; así se evita que la tabla se desborde del margen de la página.
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.ConstantColumn(70);
                            if (conMontos)
                            {
                                columns.ConstantColumn(80);
                                columns.ConstantColumn(80);
                            }
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CeldaHeader).Text("Descripción");
                            header.Cell().Element(CeldaHeader).Text(t => { t.AlignRight(); t.Span("Cantidad").FontSize(9); });
                            if (conMontos)
                            {
                                header.Cell().Element(CeldaHeader).Text(t => { t.AlignRight(); t.Span("P. Unit.").FontSize(9); });
                                header.Cell().Element(CeldaHeader).Text(t => { t.AlignRight(); t.Span("Total").FontSize(9); });
                            }
                        });

                        foreach (var item in lista)
                        {
                            table.Cell().Element(Celda).Text(item.Descripcion);
                            table.Cell().Element(Celda).Text(t => { t.AlignRight(); t.Span(item.Cantidad.ToString("0.##")); });
                            if (conMontos)
                            {
                                table.Cell().Element(Celda).Text(t => { t.AlignRight(); t.Span(item.PrecioUnitario?.ToString("0.00") ?? "-"); });
                                table.Cell().Element(Celda).Text(t => { t.AlignRight(); t.Span(item.Total?.ToString("0.00") ?? "-"); });
                            }
                        }
                    });

                    if (total is not null)
                    {
                        col.Item().PaddingTop(8).Text(t => { t.AlignRight(); t.Span($"TOTAL ({moneda}): {total:0.00}").Bold().FontSize(12); });
                    }

                    if (!string.IsNullOrWhiteSpace(observaciones))
                    {
                        col.Item().PaddingTop(12).Text(t =>
                        {
                            t.Span("Observaciones: ").SemiBold();
                            t.Span(observaciones);
                        });
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generado por SISERP - ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        return documento.GeneratePdf();
    }

    private static IContainer CeldaHeader(IContainer container) =>
        container.Background(Colors.Grey.Lighten2).Padding(5).DefaultTextStyle(x => x.SemiBold());

    private static IContainer Celda(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
}
