using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text;
using System.Xml.Linq;

namespace Prodright.Processing;
public static class SapMerchStoryBuilder
{
    public static string BuildStoryFromFile(string filePath, DateTime? asOf = null)
    {
        // Stream load handles BOM/encoding properly
        using var fs = File.OpenRead(filePath);
        var doc = XDocument.Load(fs);

        return BuildStory(doc, asOf);
    }

    // Optional: if you still want to accept XML text (e.g. from textbox)
    public static string BuildStory(string xmlText, DateTime? asOf = null)
    {
        xmlText = DecodeIfHtmlEncoded(xmlText);
        var doc = XDocument.Parse(xmlText);
        return BuildStory(doc, asOf);
    }

    private static string DecodeIfHtmlEncoded(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // If it's HTML-escaped like &lt;Merchandise&gt; then decode
        if (text.Contains("&lt;") || text.TrimStart().StartsWith("&lt;"))
            text = WebUtility.HtmlDecode(text);

        // Strip BOM + leading junk
        text = text.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        int lt = text.IndexOf('<');
        if (lt > 0) text = text.Substring(lt);

        return text;
    }

    // Call this with the XML payload string and (optionally) an "as of" date.
    // If asOf is null, it will use the message CreationDateTime date (if present), else DateTime.UtcNow.Date.


    private static XElement? FirstDesc(XContainer node, string localName) => node.Descendants().FirstOrDefault(e => e.Name.LocalName == localName); 
    private static IEnumerable<XElement> Desc(XContainer node, string localName) => node.Descendants().Where(e => e.Name.LocalName == localName); 
    private static XElement? Child(XElement node, string localName) => node.Elements().FirstOrDefault(e => e.Name.LocalName == localName); 
    private static IEnumerable<XElement> Children(XElement node, string localName) => node.Elements().Where(e => e.Name.LocalName == localName); 
    private static string Val(XElement? node) => node?.Value?.Trim() ?? "";


    public static string BuildStory(XDocument doc, DateTime? asOf = null)
    {
        // --- Find the Merchandise node regardless of namespace prefix
        var merch = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Merchandise");
        if (merch == null)
            return "No <Merchandise> node found in the payload.";

        // --- Core identifiers (these nodes are unprefixed in your payload) [1](https://sparza-my.sharepoint.com/personal/letourneurb_spar_net/Documents/Microsoft%20Copilot%20Chat%20Files/10283_sap.xml)
        string internalId = Val(Child(merch, "InternalID"));
        string matType = Val(Child(merch, "MaterialTypeCode"));
        string merchType = Val(Child(merch, "MaterialMerchandiseTypeCode"));

        // Description block contains nested <Description> elements (unprefixed) [1](https://sparza-my.sharepoint.com/personal/letourneurb_spar_net/Documents/Microsoft%20Copilot%20Chat%20Files/10283_sap.xml)
        string description = Desc(merch, "Description")
            .Where(x => (string?)x.Attribute("languageCode") == "en")
            .Select(x => x.Value.Trim())
            .FirstOrDefault() ?? "";

        // --- Message header date for default "as of"
        DateTime defaultAsOf = DateTime.UtcNow.Date;

        // There are two MessageHeader blocks in your payload; we take the first CreationDateTime found. [1](https://sparza-my.sharepoint.com/personal/letourneurb_spar_net/Documents/Microsoft%20Copilot%20Chat%20Files/10283_sap.xml)
        var headerCreation = doc.Descendants()
            .Where(e => e.Name.LocalName == "CreationDateTime")
            .Select(e => e.Value.Trim())
            .FirstOrDefault();

        if (DateTime.TryParse(headerCreation, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal, out var hdrDt))
        {
            defaultAsOf = hdrDt.Date;
        }

        var asOfDate = (asOf ?? defaultAsOf).Date;

        // --- Store / Org
        string receivingStore = Desc(merch, "ReceivingStore")
            .Select(rs => Val(FirstDesc(rs, "StoreInternalID")))
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "";

        // --- Product category
        var cat = Children(merch, "ProductCategory").FirstOrDefault();
        string prodCatId = Val(Child(cat!, "InternalID"));
        string catHierarchyId = Val(Child(cat!, "ProductCategoryHierarchyID"));
        string catHierarchyType = Val(Child(cat!, "ProductCategoryHierarchyTypeCode"));

        // --- SPAR extensions: note this is prefixed as n1:SPAR_MerchExtensions in your payload [1](https://sparza-my.sharepoint.com/personal/letourneurb_spar_net/Documents/Microsoft%20Copilot%20Chat%20Files/10283_sap.xml)
        var sparExt = merch.Descendants().FirstOrDefault(e => e.Name.LocalName == "SPAR_MerchExtensions");

        string brandName = sparExt?.Descendants().FirstOrDefault(e => e.Name.LocalName == "BrandName")?.Value.Trim() ?? "";
        string netCont = sparExt?.Descendants().FirstOrDefault(e => e.Name.LocalName == "NetCont")?.Value.Trim() ?? "";
        string contUnit = sparExt?.Descendants().FirstOrDefault(e => e.Name.LocalName == "ContUnit")?.Value.Trim() ?? "";

        string mchShort = sparExt?
            .Descendants()
            .Where(e => e.Name.LocalName == "MCHShortDes")
            .Descendants()
            .Where(e => e.Name.LocalName == "Description")
            .Select(e => e.Value.Trim())
            .FirstOrDefault() ?? "";

        string logisticsMethod = sparExt?.Descendants().FirstOrDefault(e => e.Name.LocalName == "SPAR_LogisticsMethod")?.Value.Trim() ?? "";
        string vendorArticleId = sparExt?.Descendants().FirstOrDefault(e => e.Name.LocalName == "VendorArticleID")?.Value.Trim() ?? "";

        // --- Tax & POS qualifiers
        var tax = Children(merch, "TaxClassification").FirstOrDefault();
        string taxCountry = Val(Child(tax!, "CountryCode"));
        string taxRegion = Val(Child(tax!, "RegionCode"));
        decimal taxPercent = TryDec(Val(Child(tax!, "Percent")));

        var pos = Children(merch, "PointOfSaleProcessingCondition").FirstOrDefault();
        bool priceReq = TryBool(Val(Child(pos!, "PriceRequiredIndicator")));
        bool discountAllowed = TryBool(Val(Child(pos!, "DiscountAllowedIndicator")));
        bool taxIncluded = TryBool(Val(Child(pos!, "TaxIncludedIndicator")));
        bool textVisible = TryBool(Val(Child(pos!, "TextOnPointOfSaleRegisterVisibleIndicator")));

        // --- Quantity conversions (these elements are unprefixed) [1](https://sparza-my.sharepoint.com/personal/letourneurb_spar_net/Documents/Microsoft%20Copilot%20Chat%20Files/10283_sap.xml)
        var conversionToEa = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var qc in Children(merch, "QuantityConversion"))
        {
            var qtyNode = Child(qc, "Quantity");
            var corNode = Child(qc, "CorrespondingQuantity");

            string fromUnit = qtyNode?.Attribute("unitCode")?.Value?.Trim() ?? "";
            decimal fromQty = TryDec(Val(qtyNode));

            string toUnit = corNode?.Attribute("unitCode")?.Value?.Trim() ?? "";
            decimal toQty = TryDec(Val(corNode));

            if (string.Equals(toUnit, "EA", StringComparison.OrdinalIgnoreCase) && fromQty != 0m)
            {
                conversionToEa[fromUnit] = toQty / fromQty;
            }
        }

        // --- Procurement prices (PB00) [1](https://sparza-my.sharepoint.com/personal/letourneurb_spar_net/Documents/Microsoft%20Copilot%20Chat%20Files/10283_sap.xml)
        var procurementItems = Children(merch, "ProcurementPriceInformation")
            .Select(ppi =>
            {
                var spec = Children(ppi, "PriceSpecification").FirstOrDefault();
                return new
                {
                    SupplierId = Val(Child(ppi, "SupplierInternalID")),
                    PurchasingOrg = Val(Child(ppi, "StrategicPurchasingFunctionalUnitID")),
                    OrderUom = Val(Child(ppi, "OrderMeasureUnitCode")),
                    Currency = Val(Child(ppi, "OrderTransactionCurrencyCode")),
                    Spec = spec
                };
            })
            .Where(x => x.Spec != null && Val(Child(x.Spec!, "PriceSpecificationElementTypeCode")) == "PB00")
            .Select(x => new ProcurementPrice
            {
                SupplierId = x.SupplierId,
                PurchasingUnit = x.PurchasingOrg,
                OrderUom = x.OrderUom,
                Currency = x.Currency,
                Amount = TryDec(Val(Child(x.Spec!, "Amount"))),
                SpecStart = TryDate(
                    Desc(x.Spec!, "StartTimePoint").Select(d => Val(Child(d, "Date"))).FirstOrDefault()
                ),
                SpecEnd = TryDate(
                    Desc(x.Spec!, "EndTimePoint").Select(d => Val(Child(d, "Date"))).FirstOrDefault()
                )
            })
            .OrderBy(p => p.SpecStart)
            .ToList();

        var activeProc = procurementItems
            .Where(p => p.SpecStart != null && p.SpecEnd != null &&
                        p.SpecStart.Value.Date <= asOfDate && asOfDate <= p.SpecEnd.Value.Date)
            .OrderByDescending(p => p.SpecStart)
            .FirstOrDefault()
            ?? procurementItems.OrderByDescending(p => p.SpecStart).FirstOrDefault();

        // --- Sales prices (VKP0) [1](https://sparza-my.sharepoint.com/personal/letourneurb_spar_net/Documents/Microsoft%20Copilot%20Chat%20Files/10283_sap.xml)
        var salesPrices = Children(merch, "SalesPriceInformation")
            .Select(spi =>
            {
                var spec = Children(spi, "PriceSpecification").FirstOrDefault();
                return new
                {
                    Uom = Val(Child(spi, "MeasureUnitCode")),
                    SalesOrg = Val(Child(spi, "SalesOrganisationID")),
                    Channel = Val(Child(spi, "DistributionChannelCode")),
                    Spec = spec
                };
            })
            .Where(x => x.Spec != null && Val(Child(x.Spec!, "PriceSpecificationElementTypeCode")) == "VKP0")
            .Select(x => new SalesPrice
            {
                Uom = x.Uom,
                SalesOrg = x.SalesOrg,
                Channel = x.Channel,
                Currency = Child(x.Spec!, "Amount")?.Attribute("currencyCode")?.Value?.Trim() ?? "",
                Amount = TryDec(Val(Child(x.Spec!, "Amount")))
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Uom))
            .ToList();

        // Other sales conditions (ZLO1) exist in payload; keep un-interpreted [1](https://sparza-my.sharepoint.com/personal/letourneurb_spar_net/Documents/Microsoft%20Copilot%20Chat%20Files/10283_sap.xml)
        var otherSalesConditions = Children(merch, "SalesPriceInformation")
            .Select(spi => Children(spi, "PriceSpecification").FirstOrDefault())
            .Where(spec => spec != null && Val(Child(spec!, "PriceSpecificationElementTypeCode")) == "ZLO1")
            .Select(spec => new
            {
                Currency = Child(spec!, "Amount")?.Attribute("currencyCode")?.Value?.Trim() ?? "",
                Amount = Val(Child(spec!, "Amount"))
            })
            .ToList();

        decimal? sellEA = salesPrices.FirstOrDefault(p => p.Uom.Equals("EA", StringComparison.OrdinalIgnoreCase))?.Amount;
        decimal? sellPK1 = salesPrices.FirstOrDefault(p => p.Uom.Equals("PK1", StringComparison.OrdinalIgnoreCase))?.Amount;
        decimal? sellCS1 = salesPrices.FirstOrDefault(p => p.Uom.Equals("CS1", StringComparison.OrdinalIgnoreCase))?.Amount;

        decimal? costPerCS1 = activeProc?.Amount;
        string procCurrency = activeProc?.Currency ?? "ZAR";

        decimal? eaPerCS1 = conversionToEa.TryGetValue("CS1", out var cs1Ea) ? cs1Ea : (decimal?)null;
        decimal? eaPerPK1 = conversionToEa.TryGetValue("PK1", out var pk1Ea) ? pk1Ea : (decimal?)null;

        decimal? costPerEA = (costPerCS1 != null && eaPerCS1 is > 0m)
            ? Decimal.Round(costPerCS1.Value / eaPerCS1.Value, 2)
            : (decimal?)null;

        decimal? costPerPK1 = (costPerEA != null && eaPerPK1 != null)
            ? Decimal.Round(costPerEA.Value * eaPerPK1.Value, 2)
            : (decimal?)null;

        var gpEA = CalcGp(sellEA, costPerEA);
        var gpPK1 = CalcGp(sellPK1, costPerPK1);
        var gpCS1 = CalcGp(sellCS1, costPerCS1);

        // --- Compose story (same as you had—kept concise here)
        var sb = new StringBuilder();
        sb.AppendLine($"PRODUCT STORY (as of {asOfDate:yyyy-MM-dd})");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine($"• Internal ID: {internalId}");
        sb.AppendLine($"• Material type: {matType} | Merchandise type: {merchType}");
        sb.AppendLine($"• Description: {description}");
        if (!string.IsNullOrWhiteSpace(brandName)) sb.AppendLine($"• Brand: {brandName}");
        if (!string.IsNullOrWhiteSpace(netCont) || !string.IsNullOrWhiteSpace(contUnit))
            sb.AppendLine($"• Net content: {netCont} {contUnit}".Trim());
        if (!string.IsNullOrWhiteSpace(mchShort)) sb.AppendLine($"• MCH short description: {mchShort}");
        sb.AppendLine();

        sb.AppendLine("PACK STRUCTURE / QUANTITIES");
        sb.AppendLine(new string('-', 60));
        if (eaPerCS1 != null) sb.AppendLine($"• 1 CS1 = {eaPerCS1:0.##} EA");
        if (eaPerPK1 != null) sb.AppendLine($"• 1 PK1 = {eaPerPK1:0.##} EA");
        sb.AppendLine("• Base unit: EA");
        sb.AppendLine();

        sb.AppendLine("SUPPLY (PROCUREMENT)");
        sb.AppendLine(new string('-', 60));
        if (activeProc != null)
        {
            sb.AppendLine($"• SupplierInternalID: {activeProc.SupplierId}");
            sb.AppendLine($"• StrategicPurchasingFunctionalUnitID: {activeProc.PurchasingUnit}");
            sb.AppendLine($"• Active PB00: {activeProc.Amount:0.00} {activeProc.Currency} per {activeProc.OrderUom} " +
                          $"(valid {activeProc.SpecStart:yyyy-MM-dd} to {activeProc.SpecEnd:yyyy-MM-dd})");
        }
        if (costPerEA != null) sb.AppendLine($"• Derived cost per EA: {costPerEA:0.00} {procCurrency}");
        if (costPerPK1 != null) sb.AppendLine($"• Derived cost per PK1: {costPerPK1:0.00} {procCurrency}");
        sb.AppendLine();

        sb.AppendLine("SELLING (SALES)");
        sb.AppendLine(new string('-', 60));
        var anySales = salesPrices.FirstOrDefault();
        if (anySales != null)
            sb.AppendLine($"• SalesOrg: {anySales.SalesOrg} | Channel: {anySales.Channel}");
        if (sellEA != null) sb.AppendLine($"• VKP0 EA:  {sellEA:0.00} ZAR");
        if (sellPK1 != null) sb.AppendLine($"• VKP0 PK1: {sellPK1:0.00} ZAR");
        if (sellCS1 != null) sb.AppendLine($"• VKP0 CS1: {sellCS1:0.00} ZAR");

        if (otherSalesConditions.Count > 0)
        {
            sb.AppendLine("• Other sales conditions present (not interpreted):");
            foreach (var c in otherSalesConditions)
                sb.AppendLine($"  - ZLO1: Amount={c.Amount} CurrencyCode='{c.Currency}'");
        }
        sb.AppendLine();

        sb.AppendLine("GROSS PROFIT (derived)");
        sb.AppendLine(new string('-', 60));
        AppendGp(sb, "EA", gpEA);
        AppendGp(sb, "PK1", gpPK1);
        AppendGp(sb, "CS1", gpCS1);
        sb.AppendLine();

        sb.AppendLine("WHERE IT EXISTS / KEY QUALIFIERS");
        sb.AppendLine(new string('-', 60));
        if (!string.IsNullOrWhiteSpace(receivingStore)) sb.AppendLine($"• Receiving store: {receivingStore}");
        if (!string.IsNullOrWhiteSpace(prodCatId))
            sb.AppendLine($"• Product category: InternalID={prodCatId}, HierarchyID={catHierarchyId}, HierarchyTypeCode={catHierarchyType}");
        if (!string.IsNullOrWhiteSpace(logisticsMethod)) sb.AppendLine($"• Logistics method: {logisticsMethod}");
        if (!string.IsNullOrWhiteSpace(vendorArticleId)) sb.AppendLine($"• Vendor article ID: {vendorArticleId}");
        sb.AppendLine($"• Tax: {taxCountry}-{taxRegion}, {taxPercent:0.##}%");
        sb.AppendLine($"• POS flags: PriceRequired={priceReq}, DiscountAllowed={discountAllowed}, TaxIncluded={taxIncluded}, TextVisible={textVisible}");

        return sb.ToString();
    }


    private static (decimal? gpValue, decimal? gpPercent) CalcGp(decimal? sell, decimal? cost)
    {
        if (sell == null || cost == null) return (null, null);
        if (sell.Value == 0m) return (Decimal.Round(sell.Value - cost.Value, 2), null);

        var gpVal = Decimal.Round(sell.Value - cost.Value, 2);
        var gpPct = Decimal.Round(gpVal / sell.Value * 100m, 2);
        return (gpVal, gpPct);
    }

    private static void AppendGp(StringBuilder sb, string uom, (decimal? gpValue, decimal? gpPercent) gp)
    {
        if (gp.gpValue == null || gp.gpPercent == null)
        {
            sb.AppendLine($"• {uom}: GP not available (missing sell/cost).");
            return;
        }
        sb.AppendLine($"• {uom}: GP = {gp.gpValue:0.00} | GP% = {gp.gpPercent:0.00}%");
    }

    private static decimal TryDec(string? s)
        => decimal.TryParse((s ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    private static bool TryBool(string? s)
        => bool.TryParse((s ?? "").Trim(), out var b) && b;

    private static DateTime? TryDate(string? s)
        => DateTime.TryParse((s ?? "").Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt) ? dt.Date : (DateTime?)null;

    private sealed class ProcurementPrice
    {
        public string SupplierId { get; set; } = "";
        public string PurchasingUnit { get; set; } = "";
        public string OrderUom { get; set; } = "";
        public string Currency { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime? SpecStart { get; set; }
        public DateTime? SpecEnd { get; set; }
    }

    private sealed class SalesPrice
    {
        public string Uom { get; set; } = "";
        public string SalesOrg { get; set; } = "";
        public string Channel { get; set; } = "";
        public string Currency { get; set; } = "";
        public decimal Amount { get; set; }
    }
}
