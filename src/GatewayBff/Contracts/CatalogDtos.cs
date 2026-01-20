namespace GatewayBff.Contracts;

public record CatalogItemDto(Guid ProductId, string Name, string? Description, decimal Price, bool IsActive, int AvailableQuantity);

public record ProductDto(Guid Id, string Name, string? Description, decimal Price, bool IsActive);
