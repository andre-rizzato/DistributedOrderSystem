namespace ProductService.Infrastructure.Cache;

using System.Text.Json;
using Microsoft.Extensions.Options;
using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Configuration;
using StackExchange.Redis;

/// <summary>
/// Implementazione Redis della cache prodotti.
/// Implementa l'interfaccia definita nell'Application layer (Dependency Inversion).
/// </summary>
public class RedisProductCache : IProductCache
{
    private readonly IDatabase _db;
    private readonly RedisSettings _settings;

    public RedisProductCache(IConnectionMultiplexer multiplexer, IOptions<RedisSettings> options)
    {
        _db = multiplexer.GetDatabase();
        _settings = options.Value;
    }

    private string Key(Guid id) => $"{_settings.Prefix}{id}";

    public async Task<Product?> GetProductByIdAsync(Guid id, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(Key(id));
        if (!value.HasValue) return null;
        return JsonSerializer.Deserialize<Product>(value.ToString()!);
    }

    public async Task SetProductAsync(Product product, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(product);
        await _db.StringSetAsync(Key(product.Id), json, TimeSpan.FromMinutes(5));
    }

    public async Task RemoveProductAsync(Guid id, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync(Key(id));
    }
}
