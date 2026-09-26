namespace GatewayBff.Clients;

using GatewayBff.Contracts;

public interface IOrderServiceClient
{
    Task<OrderDto?> GetOrderAsync(int id, CancellationToken ct);
    Task<List<OrderDto>> GetOrdersAsync(CancellationToken ct);
    Task<CreateOrderResponse> CreateOrderAsync(List<OrderItemDto> items, CancellationToken ct);
    Task<bool> UpdateOrderStatusAsync(int id, string status, CancellationToken ct);
    Task<CancelOrderResponse?> CancelOrderAsync(int id, CancellationToken ct);
}
