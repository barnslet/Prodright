using Prodright.Objects;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Xml.Linq;

namespace Prodright.Processing
{
    public static class Extractions
    {

        public static string GenerateXmlSummary(string sapPath)
        {
            try
            {
                XDocument doc = XDocument.Load(sapPath);
                XNamespace n0 = "http://sap.com/xi/SAPGlobal20/Global";

                // Extract Bulk Header
                var bulkHeader = doc.Descendants("MessageHeader").FirstOrDefault();
                // Extract Internal Replication Message Header
                var msgHeader = doc.Descendants("MerchandiseERPReplicationRequestMessage")
                                   .Elements("MessageHeader").FirstOrDefault();

                StringBuilder sb = new StringBuilder();
                sb.Append("SAP INTEGRATION MESSAGE SUMMARY");


                if (bulkHeader != null)
                {
                    sb.AppendLine(" - BULK ENVELOPE DETAILS");
                    sb.AppendLine($"Bulk Message ID:   {bulkHeader.Element("ID")?.Value}");
                    sb.AppendLine($"Created At:        {SapHelpers.FormatSapDate(bulkHeader.Element("CreationDateTime")?.Value)}");
                    sb.AppendLine($"Source System:     {bulkHeader.Element("SenderBusinessSystemID")?.Value}");
                    sb.AppendLine($"Target System:     {bulkHeader.Element("RecipientBusinessSystemID")?.Value}");
                }


                if (msgHeader != null)
                {
                    sb.AppendLine($"REPLICATION Request ID:        {msgHeader.Element("ID")?.Value}");
                }

                sb.AppendLine($"Report Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine();


                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error generating summary: {ex.Message}";
            }
        }


        public static (List<SapConversion> conversions, List<StoreItem> storeItems) ParseXmlFiles(string sapPath, string storePath)
        {
            XDocument sapDoc = XDocument.Load(sapPath);
            XDocument storeDoc = XDocument.Load(storePath);

            XNamespace ns2 = "http://spar.co.za/Interface/MasterData/Global";

            // ----------------------------
            // SAP: Generic EN description (fallback)
            // ----------------------------
            // SAP contains a general <Description><Description languageCode="en">...</Description></Description> [2](https://sparza-my.sharepoint.com/personal/letourneurb_spar_net/Documents/Microsoft%20Copilot%20Chat%20Files/10283_sap.xml)
            string sapGenericDesc = sapDoc.Descendants()
                .Where(e => e.Name.LocalName == "Description" && (string?)e.Attribute("languageCode") == "en")
                .Select(e => e.Value.Trim())
                .FirstOrDefault() ?? "";

            // ----------------------------
            // SAP: Per-UOM descriptions (fallback/enrichment)
            // ----------------------------
            // SAP provides <MeasureUnitCodeSpecificDescription> per UOM with an EN description. [2](https://sparza-my.sharepoint.com/personal/letourneurb_spar_net/Documents/Microsoft%20Copilot%20Chat%20Files/10283_sap.xml)
            var sapUomDesc = sapDoc.Descendants()
                .Where(e => e.Name.LocalName == "MeasureUnitCodeSpecificDescription")
                .Select(x => new
                {
                    Uom = x.Elements().FirstOrDefault(e => e.Name.LocalName == "MeasureUnitCode")?.Value?.Trim(),
                    Desc = x.Elements().FirstOrDefault(e => e.Name.LocalName == "Description" &&
                                                           (string?)e.Attribute("languageCode") == "en")?.Value?.Trim()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Uom) && !string.IsNullOrWhiteSpace(x.Desc))
                .GroupBy(x => x.Uom!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Desc!, StringComparer.OrdinalIgnoreCase);

            // ----------------------------
            // STORE: EANs by UOM (faster than scanning the doc for every price row)
            // ----------------------------
            // Store message contains multiple <ns2:Barcode> entries with MeasureUnitCode and ProductStandardID. [1](https://sparza-my.sharepoint.com/personal/letourneurb_spar_net/Documents/Microsoft%20Copilot%20Chat%20Files/10283_store.xml)
            var eansByUom = storeDoc.Descendants(ns2 + "Barcode")
                .Select(b => new
                {
                    Uom = b.Elements().FirstOrDefault(e => e.Name.LocalName == "MeasureUnitCode")?.Value?.Trim(),
                    Ean = b.Elements().FirstOrDefault(e => e.Name.LocalName == "ProductStandardID")?.Value?.Trim()
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Uom) && !string.IsNullOrWhiteSpace(x.Ean))
                .GroupBy(x => x.Uom!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.Ean)), StringComparer.OrdinalIgnoreCase);

            // ----------------------------
            // Conversions (you already parse these from SAP)
            // ----------------------------
            var conversions = sapDoc.Descendants().Where(d => d.Name.LocalName == "QuantityConversion")
                .Select(c => new SapConversion
                {
                    Uom = c.Elements().FirstOrDefault(e => e.Name.LocalName == "Quantity")?.Attribute("unitCode")?.Value?.Trim(),
                    Eaches = double.Parse(c.Elements().FirstOrDefault(e => e.Name.LocalName == "CorrespondingQuantity")?.Value ?? "0")
                }).ToList();

            // ----------------------------
            // Store items (VKP0 only)
            // ----------------------------
            var storeItems = storeDoc.Descendants(ns2 + "RetailSellingPrice")
                .Select(p =>
                {
                    var priceSpec = p.Elements().FirstOrDefault(e => e.Name.LocalName == "PriceSpecification");
                    var uom = p.Elements().FirstOrDefault(e => e.Name.LocalName == "MeasureUnitCode")?.Value?.Trim() ?? "";

                    var typeCode = priceSpec?.Elements()?.FirstOrDefault(e => e.Name.LocalName == "PriceSpecificationElementTypeCode")?.Value;

                    // STORE descriptions are present per selling price row. 
                    var itemDesc = p.Elements().FirstOrDefault(e => e.Name.LocalName == "ItemDescription")?.Value?.Trim();
                    var scanDesc = p.Elements().FirstOrDefault(e => e.Name.LocalName == "ScanDescription")?.Value?.Trim();
                    var shelfDesc = p.Elements().FirstOrDefault(e => e.Name.LocalName == "ShelfDescription")?.Value?.Trim();

                    // SAP fallback for missing store descriptions. 
                    sapUomDesc.TryGetValue(uom, out var sapPerUom);

                    if (string.IsNullOrWhiteSpace(itemDesc))
                        itemDesc = sapGenericDesc;

                    if (string.IsNullOrWhiteSpace(scanDesc))
                        scanDesc = sapPerUom ?? sapGenericDesc;

                    if (string.IsNullOrWhiteSpace(shelfDesc))
                        shelfDesc = sapPerUom ?? sapGenericDesc;

                    eansByUom.TryGetValue(uom, out var eans);

                    return new StoreItem
                    {
                        Uom = uom,
                        Price = double.Parse(priceSpec?.Elements()?.FirstOrDefault(e => e.Name.LocalName == "Amount")?.Value ?? "0"),
                        Prefix = p.Elements().FirstOrDefault(e => e.Name.LocalName == "PrefixCode")?.Value,
                        TypeCode = typeCode,
                        Eans = eans ?? "",

                        // ✅ three separate columns
                        ItemDescription = itemDesc,
                        ScanDescription = scanDesc,
                        ShelfDescription = shelfDesc,

                        // Optional audit/debug
                        SapUomDescription = sapPerUom
                    };
                })
                .Where(i => i.TypeCode == "VKP0")
                .ToList();

            return (conversions, storeItems);
        }

        public static DataTable CreateMappingTable(List<SapConversion> conversions, List<StoreItem> storeItems, Parameters p)
        {
            DataTable dt = new DataTable();

            // Existing columns
            dt.Columns.Add("Item Description");            
            dt.Columns.Add("Pack Size (UoM)");
            dt.Columns.Add("Number of Eaches", typeof(double));
            dt.Columns.Add("EANs");
            dt.Columns.Add("Prefix Code");
            dt.Columns.Add("Retail Price (ZAR)", typeof(double));


            dt.Columns.Add("Reasonableness Check");            
            
            // ✅ New: Description columns (3 separate fields)
            dt.Columns.Add("Scan Description");
            dt.Columns.Add("Shelf Description");


            // Base price = EA VKP0 price (used as reference)
            double basePrice = storeItems.FirstOrDefault(i => string.Equals(i.Uom, "EA", StringComparison.OrdinalIgnoreCase))?.Price ?? 0;

            foreach (var item in storeItems.Where(i => !string.IsNullOrWhiteSpace(i.Uom)))
            {
                var conv = conversions.FirstOrDefault(c => string.Equals(c.Uom, item.Uom, StringComparison.OrdinalIgnoreCase));
                double eaches = conv?.Eaches ?? 0;

                string statusPrefix = "✅";
                string reason = "";

                if (string.Equals(item.Uom, "EA", StringComparison.OrdinalIgnoreCase))
                {
                    reason = "Base Unit: Single item reference price.";
                }
                else if (basePrice > 0 && eaches > 0)
                {
                    double expectedMax = basePrice * eaches;
                    double discountPercent = ((expectedMax - item.Price) / expectedMax) * 100;

                    if (item.Price > expectedMax)
                    {
                        statusPrefix = "❌ CRITICAL: Overpriced!";
                        reason = $"Pack cost ({item.Price:N2}) exceeds buying {eaches} singles ({expectedMax:N2}).";
                    }
                    else if (discountPercent > p.Threshold)
                    {
                        statusPrefix = "⚠️ WARNING: High Discount";
                        reason = $"{discountPercent:N1}% discount detected. Verify if this is intentional.";
                    }
                    else if (item.Price <= 0)
                    {
                        statusPrefix = "❌ ERROR: Missing Price";
                        reason = "Item has no retail price assigned.";
                    }
                    else
                    {
                        // NOTE: you had a $ here; changed to currency-neutral
                        reason = $"Reasonable: {basePrice:N2} x {eaches} = {expectedMax:N2}. ({discountPercent:N1}% bulk discount).";
                    }
                }

                // ✅ Populate the new columns from StoreItem
                // Preserve the order as per the creation of the columns
                dt.Rows.Add(
                    item.ItemDescription,
                    item.Uom,
                    eaches,
                    item.Eans,
                    item.Prefix,
                    item.Price,
                    $"{statusPrefix} {reason}",
                    item.ScanDescription,
                    item.ShelfDescription

                );
            }

            dt.DefaultView.Sort = "[Number of Eaches] ASC";
            return dt;
        }


    }
}