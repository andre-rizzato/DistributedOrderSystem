namespace GatewayBff.Contracts;

public record CartDto(
    string SessionId,
    List<CartItemDto> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    decimal Total,
    int TotalItems
);

public record CartItemDto(
    Guid Id,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);

public record AddToCartRequest(Guid ProductId, int Quantity, decimal UnitPrice);

public record UpdateCartItemRequest(int Quantity);
