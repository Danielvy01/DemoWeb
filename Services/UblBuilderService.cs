using System.Xml.Linq;
using Siserp.Api.Models;

namespace Siserp.Api.Services;

/// <summary>
/// Construye el XML UBL 2.1 de un comprobante (Factura/Boleta) siguiendo la
/// estructura general exigida por SUNAT para el envio de comprobantes de pago
/// electronicos (Facturacion Electronica - SEE).
///
/// NOTA: Esta es una implementacion base pensada para operar 100% local.
/// Antes de usarse contra el ambiente BETA/PRODUCCION de SUNAT se debe
/// validar el XML contra los XSD oficiales y completar campos opcionales.
/// </summary>
public class UblBuilderService
{
    private static readonly Dictionary<TipoComprobante, string> TipoDocSunat = new()
    {
        [TipoComprobante.FACTURA] = "01",
        [TipoComprobante.BOLETA] = "03",
        [TipoComprobante.NOTA_CREDITO] = "07",
        [TipoComprobante.NOTA_DEBITO] = "08",
    };

    private static readonly Dictionary<TipoDocumentoIdentidad, string> TipoDocIdentidadSunat = new()
    {
        [TipoDocumentoIdentidad.DNI] = "1",
        [TipoDocumentoIdentidad.RUC] = "6",
        [TipoDocumentoIdentidad.CE] = "4",
        [TipoDocumentoIdentidad.PASAPORTE] = "7",
        [TipoDocumentoIdentidad.SIN_DOCUMENTO] = "0",
    };

    private static readonly XNamespace Ns = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Ext = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";

    public string BuildInvoiceXml(Comprobante comprobante)
    {
        var tipoDocSunat = TipoDocSunat.GetValueOrDefault(comprobante.TipoComprobante, "01");
        var idComprobante = $"{comprobante.Serie}-{comprobante.Correlativo}";

        var invoiceLines = comprobante.Detalles.Select(detalle => new XElement(Cac + "InvoiceLine",
            new XElement(Cbc + "ID", detalle.Id),
            new XElement(Cbc + "InvoicedQuantity", new XAttribute("unitCode", detalle.Producto.UnidadMedida), detalle.Cantidad),
            new XElement(Cbc + "LineExtensionAmount", new XAttribute("currencyID", comprobante.Moneda), detalle.ValorVenta),
            new XElement(Cac + "Item", new XElement(Cbc + "Description", detalle.Producto.Descripcion)),
            new XElement(Cac + "Price", new XElement(Cbc + "PriceAmount", new XAttribute("currencyID", comprobante.Moneda), detalle.PrecioUnitario))
        ));

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(Ns + "Invoice",
                new XAttribute(XNamespace.Xmlns + "cac", Cac),
                new XAttribute(XNamespace.Xmlns + "cbc", Cbc),
                new XAttribute(XNamespace.Xmlns + "ext", Ext),
                new XElement(Ext + "UBLExtensions", new XElement(Ext + "UBLExtension", new XElement(Ext + "ExtensionContent"))),
                new XElement(Cbc + "UBLVersionID", "2.1"),
                new XElement(Cbc + "CustomizationID", "2.0"),
                new XElement(Cbc + "ID", idComprobante),
                new XElement(Cbc + "IssueDate", comprobante.FechaEmision.ToString("yyyy-MM-dd")),
                new XElement(Cbc + "InvoiceTypeCode", new XAttribute("listID", "0101"), tipoDocSunat),
                new XElement(Cbc + "DocumentCurrencyCode", comprobante.Moneda),
                new XElement(Cac + "AccountingSupplierParty",
                    new XElement(Cac + "Party",
                        new XElement(Cac + "PartyIdentification", new XElement(Cbc + "ID", new XAttribute("schemeID", "6"), comprobante.Empresa.Ruc)),
                        new XElement(Cac + "PartyLegalEntity", new XElement(Cbc + "RegistrationName", comprobante.Empresa.RazonSocial)))),
                new XElement(Cac + "AccountingCustomerParty",
                    new XElement(Cac + "Party",
                        new XElement(Cac + "PartyIdentification", new XElement(Cbc + "ID",
                            new XAttribute("schemeID", TipoDocIdentidadSunat.GetValueOrDefault(comprobante.Cliente.TipoDocumento, "1")),
                            comprobante.Cliente.NumeroDocumento)),
                        new XElement(Cac + "PartyLegalEntity", new XElement(Cbc + "RegistrationName", comprobante.Cliente.NombreORazonSocial)))),
                new XElement(Cac + "TaxTotal", new XElement(Cbc + "TaxAmount", new XAttribute("currencyID", comprobante.Moneda), comprobante.Igv)),
                new XElement(Cac + "LegalMonetaryTotal",
                    new XElement(Cbc + "LineExtensionAmount", new XAttribute("currencyID", comprobante.Moneda), comprobante.OpGravada),
                    new XElement(Cbc + "TaxInclusiveAmount", new XAttribute("currencyID", comprobante.Moneda), comprobante.Total),
                    new XElement(Cbc + "PayableAmount", new XAttribute("currencyID", comprobante.Moneda), comprobante.Total)),
                invoiceLines
            ));

        return doc.ToString();
    }
}
