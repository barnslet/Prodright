using Prodright.Objects;
using System;
using System.Linq;
using System.Text;
namespace Prodright.Processing;

public static class ProductStoreViewTextFormatter
{
    /// <summary>
    /// Creates a buyer/store-friendly, multi-line story suitable for display in a multiline TextBox.
    /// </summary>
    /// <param name="view">The ProductStoreView object to display.</param>
    /// <param name="detailed">If true, includes more technical/audit flags; if false, keeps it buyer-friendly.</param>
    public static string ToMultilineText(ProductStoreView view, bool detailed = false)
    {
        if (view == null) return "No product data to display.";

        var sb = new StringBuilder(1024);

        // --- Header ---
        sb.AppendLine("PRODUCT SUMMARY");
        sb.AppendLine(new string('=', 60));

        AppendIf(sb, "Product:", Combine(view.Description, $"(SAP #{view.ProductId})"));
        AppendIf(sb, "Brand (code):", view.BrandCode);
        AppendIf(sb, "Category (group):", view.ProductGroup);
        AppendIf(sb, "Division:", view.Division);
        AppendIf(sb, "Product type:", view.ProductType);
        sb.AppendLine();

        // --- Store scanning ---
        sb.AppendLine("STORE SCANNING");
        sb.AppendLine(new string('-', 60));
        AppendIf(sb, "Each barcode (GTIN/EAN):", view.EachGtin);
        AppendIf(sb, "Barcode category:", view.EachGtinCategory);
        sb.AppendLine();

        // --- Buying / Ordering ---
        sb.AppendLine("BUYING / ORDERING");
        sb.AppendLine(new string('-', 60));
        AppendIf(sb, "Order unit:", view.OrderingUnit);
        AppendIf(sb, "Base unit:", view.BaseUnit);

        if (detailed)
        {
            AppendIf(sb, "Variable ordering units active:", view.IsVariableOrderingUnitActive ? "Yes" : "No");
            AppendIf(sb, "Ordering unit status:", view.OrderingUnitStatus);
        }
        sb.AppendLine();

        // --- Physical information ---
        sb.AppendLine("PHYSICAL INFO (EACH)");
        sb.AppendLine(new string('-', 60));
        AppendIf(sb, "Gross weight:", FormatWeight(view.EachGrossWeightKg, "KG"));
        AppendIf(sb, "Net weight:", FormatWeight(view.EachNetWeightKg, "KG"));
        AppendIf(sb, "Volume:", FormatVolume(view.EachVolume, view.VolumeUnit));
        AppendIf(sb, "Storage condition (code):", view.WarehouseStorageCondition);
        sb.AppendLine();

        // --- Packaging breakdown ---
        sb.AppendLine("PACKAGING LEVELS");
        sb.AppendLine(new string('-', 60));

        if (view.Packaging == null || view.Packaging.Count == 0)
        {
            sb.AppendLine("No packaging levels were returned by SAP.");
        }
        else
        {
            foreach (var p in view.Packaging.OrderBy(x => PackSortKey(x.UnitCode)))
            {
                // Example line: "Case (CS1): 24 x EA | GTIN: ... | 390 x 250 x 170 MM | 8.5 KG"
                var line = new StringBuilder();

                line.Append($"{(p.FriendlyName ?? p.UnitCode)} ({p.UnitCode})");

                if (p.UnitsPerBase.HasValue && !string.IsNullOrWhiteSpace(p.BaseUnit))
                    line.Append($": {p.UnitsPerBase.Value} x {p.BaseUnit}");

                if (!string.IsNullOrWhiteSpace(p.Gtin))
                    line.Append($" | GTIN: {p.Gtin}");

                var dims = FormatDimensions(p.Length, p.Width, p.Height, p.DimensionUnit);
                if (!string.IsNullOrWhiteSpace(dims))
                    line.Append($" | {dims}");

                var weight = FormatWeight(p.GrossWeight, p.WeightUnit);
                if (!string.IsNullOrWhiteSpace(weight))
                    line.Append($" | {weight}");

                if (!string.IsNullOrWhiteSpace(p.ContainsLowerPackUnit))
                    line.Append($" | Contains: {p.ContainsLowerPackUnit}");

                sb.AppendLine(line.ToString());
            }
        }

        sb.AppendLine();

        // --- Handling / Compliance (optional) ---
        if (detailed)
        {
            sb.AppendLine("HANDLING / COMPLIANCE");
            sb.AppendLine(new string('-', 60));
            AppendIf(sb, "Batch managed:", view.BatchManaged ? "Yes" : "No");
            AppendIf(sb, "Quality checks active:", view.QualityChecksActive ? "Yes" : "No");
            AppendIf(sb, "Pilferable:", view.Pilferable ? "Yes" : "No");
            AppendIf(sb, "Hazardous relevant:", view.HazardousRelevant ? "Yes" : "No");
            AppendIf(sb, "Marked for deletion:", view.IsMarkedForDeletion ? "Yes" : "No");
            sb.AppendLine();

            sb.AppendLine("AUDIT");
            sb.AppendLine(new string('-', 60));
            AppendIf(sb, "Created on:", FormatDate(view.CreatedOn));
            AppendIf(sb, "Created by:", view.CreatedBy);
            AppendIf(sb, "Last changed on:", FormatDate(view.LastChangedOn));
            AppendIf(sb, "Last changed by:", view.LastChangedBy);
            sb.AppendLine();
        }

        // --- Footer note ---
        sb.AppendLine("TIP");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine("If the buyer needs pricing, supplier, or stock availability, those are retrieved from separate SAP services.");

        return sb.ToString().TrimEnd();
    }

    // ----------------- Helpers -----------------

    private static void AppendIf(StringBuilder sb, string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.AppendLine($"{label} {value}");
    }

    private static string Combine(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a)) return b;
        if (string.IsNullOrWhiteSpace(b)) return a;
        return $"{a} {b}";
    }

    private static string FormatWeight(decimal? value, string unit)
    {
        if (!value.HasValue) return null;
        unit = string.IsNullOrWhiteSpace(unit) ? "" : $" {unit.Trim()}";
        return $"{value.Value:0.###}{unit}";
    }

    private static string FormatVolume(decimal? value, string unit)
    {
        if (!value.HasValue) return null;
        unit = string.IsNullOrWhiteSpace(unit) ? "" : $" {unit.Trim()}";
        return $"{value.Value:0.###}{unit}";
    }

    private static string FormatDimensions(decimal? l, decimal? w, decimal? h, string unit)
    {
        if (!l.HasValue && !w.HasValue && !h.HasValue) return null;
        unit = string.IsNullOrWhiteSpace(unit) ? "" : $" {unit.Trim()}";
        return $"{(l ?? 0):0.###} x {(w ?? 0):0.###} x {(h ?? 0):0.###}{unit}";
    }

    private static string FormatDate(DateTime? dt)
        => dt.HasValue ? dt.Value.ToString("yyyy-MM-dd") : null;

    private static int PackSortKey(string unitCode)
    {
        if (string.IsNullOrWhiteSpace(unitCode)) return 9;
        if (unitCode.Equals("EA", StringComparison.OrdinalIgnoreCase)) return 0;
        if (unitCode.StartsWith("PK", StringComparison.OrdinalIgnoreCase)) return 1;
        if (unitCode.StartsWith("CS", StringComparison.OrdinalIgnoreCase)) return 2;
        return 9;
    }
}
