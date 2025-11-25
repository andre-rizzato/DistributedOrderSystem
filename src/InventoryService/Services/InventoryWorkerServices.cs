namespace InventoryService.Services;
using InventoryService.Services.Interfaces;
using InventoryService.Cache;
using InventoryService.Data;
using InventoryService.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Threading;
using InventoryService.Cache.Interfaces;

public class InventoryWorkerService : IInventoryWorkerService
{
    private readonly InventoryContext _db;
    private readonly IInventoryCache _cache;
    private readonly ILogger<InventoryWorkerService> _logger;

    public InventoryWorkerService(InventoryContext db, IInventoryCache cache, ILogger<InventoryWorkerService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> AdjustInventoryQuantityAsync(int productId, int delta, CancellationToken ct = default)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId, ct);
        if (item == null)
        {
            _logger.LogWarning("Inventory item for product {ProductId} not found.", productId);
            return false;
        }
        var newQuantity = item.AvailableQuantity + delta;
        if (newQuantity <= 0)
        {
            _logger.LogWarning("Insufficient inventory for product {ProductId}. Requested adjustment: {Delta}, Available: {AvailableQuantity}", productId, delta, item.AvailableQuantity);
            return false;
        }
        item.AvailableQuantity = newQuantity;
        item.LastUpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _cache.SetInventoryItemAsync(item, ct);
        return true;
    }

    public async Task<InventoryItem?> GetInventoryByProductIdAsync(int productId, CancellationToken ct = default)
    {
       var cachedItem =  await _cache.GetInventoryItemByProductIdAsync(productId, ct);
       if (cachedItem is not null)
       {
        _logger.LogInformation("Inventory for product {ProductId} served from cache", productId);
        return cachedItem;
       }

       var item = await _db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == productId, ct);
         if (item != null)
         {
          await _cache.SetInventoryItemAsync(item, ct);
         }
         return item;
    }

    public async Task<InventoryItem?> SetInventoryQuantityAsync(int productId, int quantity, CancellationToken ct = default)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId,ct);
        if (item == null)
        {
            item = new InventoryItem
            {
                ProductId = productId,
                AvailableQuantity = quantity,
                ReservedQuantity = 0,
                LastUpdatedUtc = DateTime.UtcNow
            };
            await _db.InventoryItems.AddAsync(item, ct);
        }
        else
        {
            item.AvailableQuantity = quantity;
            item.LastUpdatedUtc = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        await _cache.SetInventoryItemAsync(item, ct);
        return item;
    }
}
