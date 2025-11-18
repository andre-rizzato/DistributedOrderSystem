namespace ProductService.Repositories.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ProductService.Models;

    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken ct = default);
        Task<Product> GetProductByIdAsync(int id, CancellationToken ct = default);
        Task AddProductAsync(Product product, CancellationToken ct = default);
        Task UpdateProductAsync(Product product, CancellationToken ct = default);
        Task<bool> DeleteProductAsync(int id, CancellationToken ct = default);
    }
}