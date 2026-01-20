namespace ProductService.Repositories;

    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using ProductService.Models;
    using ProductService.Data;
    using ProductService.Repositories.Interfaces;
public class ProductRepository : IProductRepository
{
    
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken ct = default)
    {
        return await _context.Products.ToListAsync(ct);
    }
    public async Task<Product> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Products.FindAsync(new object[] { id }, ct);
    }
    public async Task AddProductAsync(Product product, CancellationToken ct = default)
    {
        await _context.Products.AddAsync(product, ct);
        await _context.SaveChangesAsync(ct);
    }
    public async Task UpdateProductAsync(Product product, CancellationToken ct = default)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync(ct);
    }
    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken ct = default)
    {
        var product = await GetProductByIdAsync(id, ct);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync(ct);
            return true;
        }
        return false;
    }

}
