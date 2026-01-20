namespace CustomerService.Storage;

using System.Text.Json;
using CustomerService.Models;
using StackExchange.Redis;

public class RedisCartStorage : ICartStorage
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCartStorage> _logger;
    private const string KeyPrefix = "cart:";
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromDays(7);

    public RedisCartStorage(IConnectionMultiplexer multiplexer, ILogger<RedisCartStorage> logger)
    {
        _db = multiplexer.GetDatabase();
        _logger = logger;
    }

    private string GetKey(string sessionId) => $"{KeyPrefix}{sessionId}";

    public async Task<Cart> GetCartAsync(string sessionId, CancellationToken ct = default)
    {
        var key = GetKey(sessionId);
        var value = await _db.StringGetAsync(key);
        
        if (value.IsNullOrEmpty)
        {
            return new Cart { SessionId = sessionId };
        }
        
        var cart = JsonSerializer.Deserialize<Cart>(value.ToString());
        return cart ?? new Cart { SessionId = sessionId };
    }

    public async Task SaveCartAsync(Cart cart, CancellationToken ct = default)
    {
        var key = GetKey(cart.SessionId);
        var json = JsonSerializer.Serialize(cart);
        await _db.StringSetAsync(key, json, DefaultExpiry);
    }

    public async Task DeleteCartAsync(string sessionId, CancellationToken ct = default)
    {
        var key = GetKey(sessionId);
        await _db.KeyDeleteAsync(key);
    }
}
