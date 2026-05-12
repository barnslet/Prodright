public sealed record Product(
    string Id,
    string? Description,
    string? ProductType,
    string? BaseUnit,
    string? PurchaseOrderUnit,
    string? ProductGroup,
    string? Division,
    string? Brand,
    decimal? GrossWeight,
    decimal? NetWeight,
    string? WeightUnit,
    decimal? Volume,
    string? VolumeUnit,
    bool IsMarkedForDeletion,
    string? Gtin,
    string? GtinCategory,
    IReadOnlyList<ProductUom> UnitsOfMeasure
);

public sealed record ProductUom(
    string AlternativeUnit,
    decimal? Numerator,
    decimal? Denominator,
    string? Gtin,
    string? GtinCategory,
    decimal? GrossWeight,
    string? WeightUnit,
    decimal? Volume,
    string? VolumeUnit,
    Dimensions? Dimensions
);

public sealed record Dimensions(
    decimal? Length,
    decimal? Width,
    decimal? Height,
    string? Unit
);