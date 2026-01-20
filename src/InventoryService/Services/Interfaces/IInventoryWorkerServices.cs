namespace InventoryService.Services.Interfaces;
using InventoryService.Models;

public interface IInventoryWorkerService
{
    Task<InventoryItem?> GetInventoryByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<InventoryItem?> SetInventoryQuantityAsync(Guid productId, int quantity, CancellationToken ct = default);
    Task<bool> AdjustInventoryQuantityAsync(Guid productId, int delta, CancellationToken ct = default);

}