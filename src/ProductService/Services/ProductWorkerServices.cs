namespace ProductService.Services;
using ProductService.Cache.Interfaces;
using ProductService.Models;
using ProductService.Repositories.Interfaces;
using ProductService.Services.Interfaces;

public class ProductWorkerServices : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IProductCache _cache;

    public ProductWorkerServices(IProductRepository repository, IProductCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken ct = default)
    {
        return await _repository.GetAllProductsAsync(ct);
    }

    public async Task<Product> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        var cachedProduct = await _cache.GetProductByIdAsync(id, ct);
        if (cachedProduct != null)
        {
            return cachedProduct;
        }

        var product = await _repository.GetProductByIdAsync(id, ct);
        if (product != null)
        {
            await _cache.SetProductAsync(product, ct);
        }
        return product!;
    }
    public async Task AddProductAsync(Product product, CancellationToken ct = default)
    {
        await _repository.AddProductAsync(product, ct);
        await _cache.SetProductAsync(product, ct);
    }
    public async Task UpdateProductAsync(Product product, CancellationToken ct = default)
    {
        await _repository.UpdateProductAsync(product, ct);
        await _cache.SetProductAsync(product, ct);
    }

    public async Task<bool> DeleteProductAsync(int id, CancellationToken ct = default)
    {
        var deleted = await _repository.DeleteProductAsync(id, ct);
        if (deleted)
        {
            await _cache.RemoveProductAsync(id, ct);
        }
        return deleted;
    }
}