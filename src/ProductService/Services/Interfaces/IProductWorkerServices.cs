
using ProductService.Cache;
using ProductService.Models;
using ProductService.Repositories;

namespace ProductService.Services.Interfaces;

public interface IProductService
{
        Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken ct = default);
        Task<Product> GetProductByIdAsync(int id, CancellationToken ct = default);
        Task AddProductAsync(Product product, CancellationToken ct = default);
        Task UpdateProductAsync(Product product, CancellationToken ct = default);
        Task<bool> DeleteProductAsync(int id, CancellationToken ct = default);
}