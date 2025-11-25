namespace InventoryService.Services.Interfaces;
using InventoryService.Models;

public interface IInventoryWorkerService
{
    Task<InventoryItem?> GetInventoryByProductIdAsync(int productId, CancellationToken ct = default);
    Task<InventoryItem?> SetInventoryQuantityAsync(int productId, int quantity, CancellationToken ct = default);
    Task<bool> AdjustInventoryQuantityAsync(int productId, int delta, CancellationToken ct = default);

}