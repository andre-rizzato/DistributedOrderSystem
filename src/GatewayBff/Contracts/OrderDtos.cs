
namespace GatewayBff.Contracts;
public record OrderItemDto(int ProductId, int Quantity);
public record CreateOrderRequest(List<OrderItemDto> Items);
public record CreateOrderResponse(int OrderId, string Status, decimal Total);
