public sealed class SapODataOptions
{
    public required string BaseUrl { get; set; }          // https://fiori-lb-uat.spar.net:44311
    public required string ServiceRoot { get; set; }      // /sap/opu/odata/sap/API_PRODUCT_SRV/
    public string? SapClient { get; set; }                // 650
}