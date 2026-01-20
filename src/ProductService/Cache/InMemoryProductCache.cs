namespace ProductService.Cache;

using ProductService.Cache.Interfaces;
using ProductService.Models;

public class InMemoryProductCache : IProductCache
{
    private readonly Dictionary<Guid, Product> _cache = new();
    private readonly object _lock = new object();

    public Task<Product?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _cache.TryGetValue(id, out var product);
            return Task.FromResult(product);
        }
    }

    public Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_cache.Values.AsEnumerable());
        }
    }

    public Task SetProductAsync(Product product, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _cache[product.Id] = product;
            return Task.CompletedTask;
        }
    }

    public Task RemoveProductAsync(Guid id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _cache.Remove(id);
            return Task.CompletedTask;
        }
    }
}