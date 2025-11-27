namespace OrderService.Services;

using OrderService.Models;

public interface IOrderService
{
    Task<Order?> GetOrderByIdAsync(int id, CancellationToken ct = default);
    Task<List<Order>> GetAllOrdersAsync(CancellationToken ct = default);
    Task<Order> CreateOrderAsync(Order order, CancellationToken ct = default);
    Task<bool> UpdateOrderStatusAsync(int orderId, string status, CancellationToken ct = default);
}
