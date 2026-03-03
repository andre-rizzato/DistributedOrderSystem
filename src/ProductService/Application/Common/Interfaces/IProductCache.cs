namespace ProductService.Application.Common.Interfaces;

using ProductService.Domain.Entities;

/// <summary>
/// Interfaccia per il caching dei prodotti.
/// Definita nell'Application layer (non nell'Infrastructure) perché i casi d'uso la richiedono.
/// L'implementazione concreta (Redis, InMemory) risiede nell'Infrastructure layer.
/// </summary>
public interface IProductCache
{
    Task<Product?> GetProductByIdAsync(Guid id, CancellationToken ct = default);
    Task SetProductAsync(Product product, CancellationToken ct = default);
    Task RemoveProductAsync(Guid id, CancellationToken ct = default);
}
