namespace ProductService.Infrastructure.Cache;

using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Entities;

/// <summary>
/// Implementazione in-memory della cache prodotti (per sviluppo/test senza Redis).
/// </summary>
public class InMemoryProductCache : IProductCache
{
    private readonly Dictionary<Guid, Product> _cache = new();
    private readonly object _lock = new();

    public Task<Product?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _cache.TryGetValue(id, out var product);
            return Task.FromResult(product);
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
