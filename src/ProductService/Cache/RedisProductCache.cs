namespace ProductService.Cache;

using System.Text.Json;
using Microsoft.Extensions.Options;
using ProductService.Cache.Interfaces;
using ProductService.Configuration;
using ProductService.Models;
using StackExchange.Redis;

public class RedisProductCache : IProductCache
{
     private readonly IDatabase _db;
    private readonly RedisSettings _settings;

    public RedisProductCache(IConnectionMultiplexer multiplexer, IOptions<RedisSettings> options)
    {
        _db = multiplexer.GetDatabase();
        _settings = options.Value;
    }

    private string Key(int id) => $"{_settings.Prefix}{id}";

    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(Key(id));
        if (!value.HasValue) return null;

        return JsonSerializer.Deserialize<Product>(value.ToString()!);
    }
    public async Task<IEnumerable<Product>> GetAllProductsAsync(CancellationToken ct = default)
    {
      throw new NotImplementedException("GetAllProductsAsync is not implemented for RedisProductCache.");
    }

    public async Task SetProductAsync(Product product, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(product);
        // TTL di esempio, 5 minuti
        await _db.StringSetAsync(Key(product.Id), json, TimeSpan.FromMinutes(5));
    }

    public async Task RemoveProductAsync(int id, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync(Key(id));
    }
}