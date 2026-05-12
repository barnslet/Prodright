
using Prodright.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
namespace Prodright.Processing;

public static class SapProductStoreViewParser
{
    public static ProductStoreView FromSapAtomXml(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        var doc = XDocument.Load(fs);



        // Namespaces from the payload:
        // Atom default ns: http://www.w3.org/2005/Atom
        // OData metadata: http://schemas.microsoft.com/ado/2007/08/dataservices/metadata
        // OData data: http://schemas.microsoft.com/ado/2007/08/dataservices
        XNamespace atom = "http://www.w3.org/2005/Atom";
        XNamespace m = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        XNamespace d = "http://schemas.microsoft.com/ado/2007/08/dataservices";

        // Helper to find the FIRST properties node inside the root entry content
        XElement rootProps = doc
            .Descendants(atom + "entry")
            .FirstOrDefault()?
            .Element(atom + "content")?
            .Element(m + "properties");

        // Description: inside link title="to_Description" -> m:inline -> feed -> entry -> content -> m:properties
        string description = doc
            .Descendants(atom + "link")
            .Where(l => (string)l.Attribute("title") == "to_Description")
            .Select(l => l.Descendants(m + "properties").FirstOrDefault())
            .Select(p => p?.Element(d + "ProductDescription")?.Value)
            .FirstOrDefault();

        // Procurement: link title="to_ProductProcurement" -> m:inline -> entry -> content -> m:properties
        XElement procurementProps = doc
            .Descendants(atom + "link")
            .Where(l => (string)l.Attribute("title") == "to_ProductProcurement")
            .Select(l => l.Descendants(m + "properties").FirstOrDefault())
            .FirstOrDefault();

        // Units of measure feed: link title="to_ProductUnitsOfMeasure" -> m:inline -> feed -> entry -> content -> m:properties
        var uomPropsList = doc
            .Descendants(atom + "link")
            .Where(l => (string)l.Attribute("title") == "to_ProductUnitsOfMeasure")
            .SelectMany(l => l.Descendants(atom + "entry")
                              .Select(e => e.Element(atom + "content")?.Element(m + "properties"))
                              .Where(p => p != null))
            .ToList();

        var view = new ProductStoreView
        {
            ProductId = GetString(rootProps, d + "Product"),
            ProductType = GetString(rootProps, d + "ProductType"),
            ProductGroup = GetString(rootProps, d + "ProductGroup"),
            Division = GetString(rootProps, d + "Division"),
            BrandCode = GetString(rootProps, d + "Brand"),
            Description = description,

            EachGtin = GetString(rootProps, d + "ProductStandardID"),
            EachGtinCategory = GetString(rootProps, d + "InternationalArticleNumberCat"),

            BaseUnit = GetString(rootProps, d + "BaseUnit"),
            OrderingUnit = GetString(rootProps, d + "PurchaseOrderQuantityUnit")
                           ?? GetString(procurementProps, d + "PurchaseOrderQuantityUnit"),
            OrderingUnitStatus = GetString(procurementProps, d + "VarblPurOrdUnitStatus"),
            IsVariableOrderingUnitActive = GetBool01(rootProps, d + "VarblPurOrdUnitIsActive"),

            EachGrossWeightKg = GetDecimal(rootProps, d + "GrossWeight"),
            EachNetWeightKg = GetDecimal(rootProps, d + "NetWeight"),
            EachVolume = GetDecimal(rootProps, d + "MaterialVolume"),
            VolumeUnit = GetString(rootProps, d + "VolumeUnit"),

            IsMarkedForDeletion = GetBool(rootProps, d + "IsMarkedForDeletion"),
            BatchManaged = GetBool(rootProps, d + "IsBatchManagementRequired"),
            QualityChecksActive = GetBool(rootProps, d + "QltyMgmtInProcmtIsActive"),
            Pilferable = GetBool(rootProps, d + "IsPilferable"),
            HazardousRelevant = GetBool(rootProps, d + "IsRelevantForHzdsSubstances"),
            WarehouseStorageCondition = GetString(rootProps, d + "WarehouseStorageCondition"),

            CreatedOn = GetDate(rootProps, d + "CreationDate"),
            CreatedBy = GetString(rootProps, d + "CreatedByUser"),
            LastChangedOn = GetDate(rootProps, d + "LastChangeDate"),
            LastChangedBy = GetString(rootProps, d + "LastChangedByUser"),

            Packaging = MapPackaging(uomPropsList, d)
        };

        return view;
    }

    private static List<PackagingLevel> MapPackaging(List<XElement> propsList, XNamespace d)
    {
        var levels = new List<PackagingLevel>();

        foreach (var p in propsList)
        {
            string unit = GetString(p, d + "AlternativeUnit");
            if (string.IsNullOrWhiteSpace(unit))
                continue;

            int? numerator = GetInt(p, d + "QuantityNumerator");
            int? denominator = GetInt(p, d + "QuantityDenominator");
            int? unitsPerBase = (numerator.HasValue && denominator.HasValue && denominator.Value != 0)
                ? numerator.Value / denominator.Value
                : (int?)null;

            levels.Add(new PackagingLevel
            {
                UnitCode = unit,
                FriendlyName = ToFriendlyPackName(unit),

                UnitsPerBase = unitsPerBase,
                BaseUnit = GetString(p, d + "BaseUnit"),

                Gtin = NullIfEmpty(GetString(p, d + "GlobalTradeItemNumber")),
                GtinCategory = NullIfEmpty(GetString(p, d + "GlobalTradeItemNumberCategory")),

                GrossWeight = GetDecimal(p, d + "GrossWeight"),
                WeightUnit = GetString(p, d + "WeightUnit"),

                Volume = GetDecimal(p, d + "MaterialVolume"),
                VolumeUnit = GetString(p, d + "VolumeUnit"),

                Length = GetDecimal(p, d + "UnitSpecificProductLength"),
                Width = GetDecimal(p, d + "UnitSpecificProductWidth"),
                Height = GetDecimal(p, d + "UnitSpecificProductHeight"),
                DimensionUnit = GetString(p, d + "ProductMeasurementUnit"),

                ContainsLowerPackUnit = NullIfEmpty(GetString(p, d + "LowerLevelPackagingUnit"))
            });
        }

        // Optional: stable order (each -> inner packs -> case)
        return levels
            .OrderBy(l => PackSortKey(l.UnitCode))
            .ToList();
    }

    private static int PackSortKey(string unitCode)
    {
        if (string.Equals(unitCode, "EA", StringComparison.OrdinalIgnoreCase)) return 0;
        if (unitCode != null && unitCode.StartsWith("PK", StringComparison.OrdinalIgnoreCase)) return 1;
        if (unitCode != null && unitCode.StartsWith("CS", StringComparison.OrdinalIgnoreCase)) return 2;
        return 9;
    }

    private static string ToFriendlyPackName(string unitCode)
    {
        if (string.IsNullOrWhiteSpace(unitCode)) return null;

        if (unitCode.Equals("EA", StringComparison.OrdinalIgnoreCase)) return "Each (single unit)";
        if (unitCode.StartsWith("PK", StringComparison.OrdinalIgnoreCase)) return "Inner Pack";
        if (unitCode.StartsWith("CS", StringComparison.OrdinalIgnoreCase)) return "Case";
        return unitCode;
    }

    // --- Safe extraction helpers ---

    private static string GetString(XElement props, XName name)
        => props?.Element(name)?.Value?.Trim();

    private static string NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    private static bool GetBool(XElement props, XName name)
    {
        var s = GetString(props, name);
        if (string.IsNullOrWhiteSpace(s)) return false;

        // SAP OData often uses "true/false"
        if (bool.TryParse(s, out bool b)) return b;

        // sometimes "X" / "" or 1/0 patterns exist in other services
        if (s.Equals("X", StringComparison.OrdinalIgnoreCase)) return true;
        if (s.Equals("1")) return true;
        if (s.Equals("0")) return false;

        return false;
    }

    private static bool GetBool01(XElement props, XName name)
    {
        var s = GetString(props, name);
        if (string.IsNullOrWhiteSpace(s)) return false;
        return s == "1" || s.Equals("X", StringComparison.OrdinalIgnoreCase) || s.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private static int? GetInt(XElement props, XName name)
    {
        var s = GetString(props, name);
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            return i;
        return null;
    }

    private static decimal? GetDecimal(XElement props, XName name)
    {
        var s = GetString(props, name);
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;
        return null;
    }

    private static DateTime? GetDate(XElement props, XName name)
    {
        var s = GetString(props, name);
        if (string.IsNullOrWhiteSpace(s)) return null;

        // Your payload contains forms like 2022-09-18T00:00:00
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt))
            return dt;

        return null;
    }
}

