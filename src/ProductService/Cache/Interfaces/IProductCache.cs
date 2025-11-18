namespace ProductService.Cache.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using ProductService.Models;

    public interface IProductCache
    {
        Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken ct = default);
        Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default);
        Task SetProductAsync(Product product, CancellationToken ct = default);
        Task RemoveProductAsync(int id, CancellationToken ct = default);
    }
}