namespace InventoryService.Cache.Interfaces;
    
using InventoryService.Models;

public interface IInventoryCache
{
    Task<InventoryItem?> GetInventoryItemByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task SetInventoryItemAsync(InventoryItem item, CancellationToken ct = default);
    Task RemoveInventoryItemAsync(Guid productId, CancellationToken ct = default);
}