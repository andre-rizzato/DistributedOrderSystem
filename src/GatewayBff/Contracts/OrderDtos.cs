namespace GatewayBff.Contracts;

public record OrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice);

public record CreateOrderRequest(List<OrderItemDto> Items);

public record CreateOrderResponse(int OrderId, string Status, decimal Total);

public record OrderDto(
    int Id,
    DateTime CreatedAt,
    string Status,
    decimal Total,
    List<OrderItemDetailDto> Items
);

public record OrderItemDetailDto(
    int Id,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);
