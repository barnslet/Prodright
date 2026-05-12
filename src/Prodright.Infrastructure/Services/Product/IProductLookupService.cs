public interface IProductLookupService
{
    Task<Product?> GetProductAsync(string productId, CancellationToken ct = default);
}