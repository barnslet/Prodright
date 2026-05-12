using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public sealed class ProductLookupService : IProductLookupService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ProductLookupService> _logger;

    public ProductLookupService(IServiceProvider services, ILogger<ProductLookupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<Product?> GetProductAsync(string productId, CancellationToken ct = default)
    {
        try
        {
            // Create a scope so transient handlers/options are clean per call
            using var scope = _services.CreateScope();
            var sap = scope.ServiceProvider.GetRequiredService<SapProductClient>();

            return await sap.GetProductAsync(productId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SAP product lookup failed.");
            // Re-throw a friendly exception for UI
            throw new InvalidOperationException(
                "SAP integration is not configured correctly (or sign-in is required). " +
                "Please check MSAL/SAP settings and try again.", ex);
        }
    }
}