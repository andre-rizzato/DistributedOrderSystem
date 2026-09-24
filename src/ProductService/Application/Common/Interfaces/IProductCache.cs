namespace ProductService.Application.Common.Interfaces;

using ProductService.Domain.Entities;

/// <summary>
/// Interface for product caching.
/// Defined in the Application layer (not Infrastructure) because the use cases require it.
/// The concrete implementation (Redis, InMemory) lives in the Infrastructure layer.
/// </summary>
public interface IProductCache
{
    Task<Product?> GetProductByIdAsync(Guid id, CancellationToken ct = default);
    Task SetProductAsync(Product product, CancellationToken ct = default);
    Task RemoveProductAsync(Guid id, CancellationToken ct = default);
}
