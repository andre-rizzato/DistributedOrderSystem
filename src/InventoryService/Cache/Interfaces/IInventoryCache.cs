namespace InventoryService.Cache.Interfaces;
    
using InventoryService.Models;

public interface IInventoryCache
{
    Task<InventoryItem?> GetInventoryItemByProductIdAsync(int productId, CancellationToken ct = default);
    Task SetInventoryItemAsync(InventoryItem item, CancellationToken ct = default);
    Task RemoveInventoryItemAsync(int productId, CancellationToken ct = default);
}