namespace Prodright
{
    public sealed class SapODataOptions
    {
        public required string BaseUrl { get; set; }
        public required string ServiceRoot { get; set; }
        public string? SapClient { get; set; }
    }


    public sealed class MsalOptions
    {
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string? Authority { get; set; }
        public string[] Scopes { get; set; } = Array.Empty<string>();
    }

}
