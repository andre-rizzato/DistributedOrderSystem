namespace CustomerService.Storage;

using System.Text.Json;
using CustomerService.Models;
using StackExchange.Redis;

public class RedisWishlistStorage : IWishlistStorage
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisWishlistStorage> _logger;
    private const string KeyPrefix = "wishlist:";
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromDays(90);

    public RedisWishlistStorage(IConnectionMultiplexer multiplexer, ILogger<RedisWishlistStorage> logger)
    {
        _db = multiplexer.GetDatabase();
        _logger = logger;
    }

    private string GetKey(Guid userId) => $"{KeyPrefix}{userId}";

    public async Task<Wishlist> GetWishlistAsync(Guid userId, CancellationToken ct = default)
    {
        var key = GetKey(userId);
        var value = await _db.StringGetAsync(key);
        
        if (value.IsNullOrEmpty)
        {
            return new Wishlist { UserId = userId };
        }
        
        var wishlist = JsonSerializer.Deserialize<Wishlist>(value.ToString());
        return wishlist ?? new Wishlist { UserId = userId };
    }

    public async Task SaveWishlistAsync(Wishlist wishlist, CancellationToken ct = default)
    {
        var key = GetKey(wishlist.UserId);
        var json = JsonSerializer.Serialize(wishlist);
        await _db.StringSetAsync(key, json, DefaultExpiry);
    }

    public async Task DeleteWishlistAsync(Guid userId, CancellationToken ct = default)
    {
        var key = GetKey(userId);
        await _db.KeyDeleteAsync(key);
    }
}
