using System;
using System.Collections.Generic;
using System.Text;

namespace Prodright.Objects
{
    public sealed class ProductStoreView
    {
        // Identity
        public string ProductId { get; init; }              // SAP Product (material) number
        public string Description { get; init; }            // Human readable name (EN)
        public string BrandCode { get; init; }              // Brand as maintained in SAP (often needs a lookup to show brand name)
        public string ProductType { get; init; }            // E.g., finished product type
        public string ProductGroup { get; init; }           // Category / grouping code
        public string Division { get; init; }               // Division code

        // Scan / retail
        public string EachGtin { get; init; }               // Store scan barcode
        public string EachGtinCategory { get; init; }       // Barcode category

        // Buying / ordering
        public string BaseUnit { get; init; }               // Usually EA
        public string OrderingUnit { get; init; }           // What we buy in (e.g., CS1)
        public string OrderingUnitStatus { get; init; }     // Status code from procurement expansion
        public bool IsVariableOrderingUnitActive { get; init; }

        // Physical (each-level defaults)
        public decimal? EachGrossWeightKg { get; init; }
        public decimal? EachNetWeightKg { get; init; }
        public decimal? EachVolume { get; init; }
        public string VolumeUnit { get; init; }

        // Handling / compliance flags
        public bool IsMarkedForDeletion { get; init; }
        public bool BatchManaged { get; init; }
        public bool QualityChecksActive { get; init; }
        public bool Pilferable { get; init; }
        public bool HazardousRelevant { get; init; }
        public string WarehouseStorageCondition { get; init; } // Code (needs a text lookup if you want “Chilled”, etc.)

        // Audit
        public DateTime? CreatedOn { get; init; }
        public string CreatedBy { get; init; }
        public DateTime? LastChangedOn { get; init; }
        public string LastChangedBy { get; init; }

        // Packaging levels (EA, inner pack, case, etc.)
        public List<PackagingLevel> Packaging { get; init; } = new();
    }

    public sealed class PackagingLevel
    {
        public string UnitCode { get; init; }              // EA / PK1 / PK2 / CS1
        public string FriendlyName { get; init; }          // “Each”, “Inner Pack”, “Case”, etc.

        public int? UnitsPerBase { get; init; }            // e.g., 24 EA per CS1 (from numerator/denominator)
        public string BaseUnit { get; init; }              // EA

        public string Gtin { get; init; }                  // Barcode for this packaging level (may be blank)
        public string GtinCategory { get; init; }

        public decimal? GrossWeight { get; init; }
        public string WeightUnit { get; init; }

        public decimal? Volume { get; init; }
        public string VolumeUnit { get; init; }

        public decimal? Length { get; init; }
        public decimal? Width { get; init; }
        public decimal? Height { get; init; }
        public string DimensionUnit { get; init; }

        public string ContainsLowerPackUnit { get; init; } // e.g., case contains PK1
    }
}

