namespace ProductService.Domain.Interfaces;

using ProductService.Domain.Entities;

/// <summary>
/// Repository contract defined in the Domain layer.
/// The Infrastructure layer provides the concrete implementation (Dependency Inversion Principle).
/// This is a pillar of Clean Architecture: dependencies point inward.
/// </summary>
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync(CancellationToken ct = default);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
