namespace ProductService.Domain.Interfaces;

using ProductService.Domain.Entities;

/// <summary>
/// Contratto del Repository definito nel Domain layer.
/// L'Infrastructure layer fornisce l'implementazione concreta (Dependency Inversion Principle).
/// Questo è un pilastro della Clean Architecture: le dipendenze puntano verso l'interno.
/// </summary>
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
