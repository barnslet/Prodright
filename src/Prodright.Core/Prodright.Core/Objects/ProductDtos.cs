using System.Text.Json.Serialization;

public sealed class ODataV2Envelope<T>
{
    [JsonPropertyName("d")]
    public T? D { get; set; }
}

public sealed class ODataV2Results<T>
{
    [JsonPropertyName("results")]
    public List<T> Results { get; set; } = new();
}

public sealed class ProductDto
{
    public string? Product { get; set; }
    public string? ProductType { get; set; }
    public string? BaseUnit { get; set; }
    public string? PurchaseOrderQuantityUnit { get; set; }

    public string? ProductGroup { get; set; }
    public string? Division { get; set; }
    public string? Brand { get; set; }

    public decimal? GrossWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public string? WeightUnit { get; set; }

    public decimal? MaterialVolume { get; set; }
    public string? VolumeUnit { get; set; }

    public bool? IsMarkedForDeletion { get; set; }

    public string? ProductStandardID { get; set; }
    public string? InternationalArticleNumberCat { get; set; }

    // Expands
    [JsonPropertyName("to_Description")]
    public ODataV2Results<ProductDescriptionDto>? ToDescription { get; set; }

    [JsonPropertyName("to_ProductProcurement")]
    public ProductProcurementDto? ToProductProcurement { get; set; }

    [JsonPropertyName("to_ProductUnitsOfMeasure")]
    public ODataV2Results<ProductUomDto>? ToProductUnitsOfMeasure { get; set; }
}

public sealed class ProductDescriptionDto
{
    public string? Product { get; set; }
    public string? Language { get; set; }
    public string? ProductDescription { get; set; }
}

public sealed class ProductProcurementDto
{
    public string? Product { get; set; }
    public string? PurchaseOrderQuantityUnit { get; set; }
}

public sealed class ProductUomDto
{
    public string? Product { get; set; }
    public string? AlternativeUnit { get; set; }

    public decimal? QuantityNumerator { get; set; }
    public decimal? QuantityDenominator { get; set; }

    public decimal? MaterialVolume { get; set; }
    public string? VolumeUnit { get; set; }

    public decimal? GrossWeight { get; set; }
    public string? WeightUnit { get; set; }

    public string? GlobalTradeItemNumber { get; set; }
    public string? GlobalTradeItemNumberCategory { get; set; }

    public decimal? UnitSpecificProductLength { get; set; }
    public decimal? UnitSpecificProductWidth { get; set; }
    public decimal? UnitSpecificProductHeight { get; set; }
    public string? ProductMeasurementUnit { get; set; }
}