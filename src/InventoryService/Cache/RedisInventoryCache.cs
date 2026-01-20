namespace InventoryService.Cache;
using InventoryService.Models;
using InventoryService.Configuration;
using InventoryService.Cache.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Threading.Tasks;
using System.Threading;

public class RedisInventoryCache : IInventoryCache
{
    private readonly IDatabase _db;
    private readonly RedisSettings _settings;

    public RedisInventoryCache(IDatabase db, IOptions<RedisSettings> options)   
    {
        _db = db;
        _settings = options.Value;
    }

    private string Key(Guid productId) => $"{_settings.InventoryPrefix}:{productId}";
    public async Task<InventoryItem?> GetInventoryItemByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
         var value = await _db.StringGetAsync(Key(productId));
         if (value.IsNullOrEmpty)
         {
             return null;
         }
         return JsonSerializer.Deserialize<InventoryItem>(value.ToString());
    }

    public async Task RemoveInventoryItemAsync(Guid productId, CancellationToken ct = default)
    {
        await _db.KeyDeleteAsync(Key(productId));   
    }

    public async Task SetInventoryItemAsync(InventoryItem item, CancellationToken ct = default)
    {
         var json = JsonSerializer.Serialize(item);
         await _db.StringSetAsync(Key(item.ProductId), json, TimeSpan.FromMinutes(5));

    }
}