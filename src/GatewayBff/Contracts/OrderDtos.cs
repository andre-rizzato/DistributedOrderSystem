namespace GatewayBff.Contracts;

public record OrderItemDto(int ProductId, int Quantity, decimal UnitPrice);

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
    int ProductId,
    int Quantity,
    decimal UnitPrice
);
