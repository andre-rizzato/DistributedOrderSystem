namespace GatewayBff.Contracts;
public record CatalogItemDto(int ProductId, string Name, string? Description, decimal Price, bool IsActive, int AvailableQuantity);
