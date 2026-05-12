using System;
using System.Collections.Generic;
using System.Text;

namespace Prodright.Objects
{
    public class SapConversion
    {
        public string Uom { get; set; }
        public double Eaches { get; set; }
    }


    public class StoreItem
    {
        public string? Uom { get; set; }
        public double Price { get; set; }
        public string? Prefix { get; set; }
        public string? TypeCode { get; set; }
        public string? Eans { get; set; }

        // NEW
        public string? ItemDescription { get; set; }
        public string? ScanDescription { get; set; }
        public string? ShelfDescription { get; set; }

        // Optional: SAP fallback/enrichment
        public string? SapUomDescription { get; set; }
    }

}
