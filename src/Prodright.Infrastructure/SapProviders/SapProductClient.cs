using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using Prodright.SapProviders.Dtos;

public sealed class SapProductClient
{
    private readonly HttpClient _http;
    private readonly SapODataOptions _opts;

    public SapProductClient(HttpClient http, IOptions<SapODataOptions> opts)
    {
        _http = http;
        _opts = opts.Value;
    }

    public async Task<Product?> GetProductAsync(string productId, CancellationToken ct = default)
    {
        var select = string.Join(',',
            "Product", "ProductType", "BaseUnit", "ProductGroup", "Division", "Brand",
            "GrossWeight", "NetWeight", "WeightUnit", "MaterialVolume", "VolumeUnit",
            "CreationDate", "LastChangeDate", "IsMarkedForDeletion",
            "ProductStandardID", "InternationalArticleNumberCat",
            "PurchaseOrderQuantityUnit"
        );

        var expand = "to_Description,to_ProductProcurement,to_ProductUnitsOfMeasure";

        var qs = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(_opts.SapClient))
            qs.Append($"sap-client={Uri.EscapeDataString(_opts.SapClient)}&");

        qs.Append($"$select={Uri.EscapeDataString(select)}&");
        qs.Append($"$expand={Uri.EscapeDataString(expand)}");

        var relative = $"A_Product('{Uri.EscapeDataString(productId)}')/?{qs}";

        using var resp = await _http.GetAsync(relative, ct);
        resp.EnsureSuccessStatusCode();

        var envelope = await resp.Content.ReadFromJsonAsync<ODataV2Envelope<ProductDto>>(cancellationToken: ct);
        var dto = envelope?.D;
        if (dto?.Product is null) return null;

        return MapToDomain(dto);
    }

    private static Product MapToDomain(ProductDto dto)
    {
        // Description: prefer EN if present, else first
        var description = dto.ToDescription?.Results
            ?.OrderByDescending(d => string.Equals(d.Language, "EN", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault()
            ?.ProductDescription;

        // Purchase order UoM: prefer the procurement expand if present, else main field
        var purchaseOrderUnit =
            dto.ToProductProcurement?.PurchaseOrderQuantityUnit
            ?? dto.PurchaseOrderQuantityUnit;

        var uoms = (dto.ToProductUnitsOfMeasure?.Results ?? [])
            .Select(u => new ProductUom(
                AlternativeUnit: u.AlternativeUnit ?? "",
                Numerator: u.QuantityNumerator,
                Denominator: u.QuantityDenominator,
                Gtin: u.GlobalTradeItemNumber,
                GtinCategory: u.GlobalTradeItemNumberCategory,
                GrossWeight: u.GrossWeight,
                WeightUnit: u.WeightUnit,
                Volume: u.MaterialVolume,
                VolumeUnit: u.VolumeUnit,
                Dimensions: new Dimensions(
                    Length: u.UnitSpecificProductLength,
                    Width: u.UnitSpecificProductWidth,
                    Height: u.UnitSpecificProductHeight,
                    Unit: u.ProductMeasurementUnit
                )
            ))
            .Where(u => !string.IsNullOrWhiteSpace(u.AlternativeUnit))
            .ToList();

        // Gtin: in your sample, the EA UoM carries the “retail” GTIN
        var eaGtin = uoms.FirstOrDefault(x => x.AlternativeUnit.Equals("EA", StringComparison.OrdinalIgnoreCase))?.Gtin;

        return new Product(
            Id: dto.Product!,
            Description: description,
            ProductType: dto.ProductType,
            BaseUnit: dto.BaseUnit,
            PurchaseOrderUnit: purchaseOrderUnit,
            ProductGroup: dto.ProductGroup,
            Division: dto.Division,
            Brand: dto.Brand,
            GrossWeight: dto.GrossWeight,
            NetWeight: dto.NetWeight,
            WeightUnit: dto.WeightUnit,
            Volume: dto.MaterialVolume,
            VolumeUnit: dto.VolumeUnit,
            IsMarkedForDeletion: dto.IsMarkedForDeletion ?? false,
            Gtin: eaGtin ?? dto.ProductStandardID,
            GtinCategory: dto.InternationalArticleNumberCat,
            UnitsOfMeasure: uoms
        );
    }
}