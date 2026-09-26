namespace GatewayBff.Contracts;

public record InventoryDto(Guid ProductId, int AvailableQuantity, int ReservedQuantity);
