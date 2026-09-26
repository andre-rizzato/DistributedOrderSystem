namespace GatewayBff.Clients;

using GatewayBff.Contracts;

public interface IInventoryServiceClient
{
    Task<InventoryDto?> GetInventoryAsync(Guid productId, CancellationToken ct);
    Task<bool> AdjustInventoryAsync(Guid productId, int delta, CancellationToken ct);
    Task<bool> SetInventoryAsync(Guid productId, int quantity, CancellationToken ct);
}
