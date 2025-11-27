namespace OrderService.Services;

using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

public class OrderWorkerService : IOrderService
{
    private readonly OrderContext _context;
    private readonly ILogger<OrderWorkerService> _logger;

    public OrderWorkerService(OrderContext context, ILogger<OrderWorkerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Order?> GetOrderByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<List<Order>> GetAllOrdersAsync(CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Order> CreateOrderAsync(Order order, CancellationToken ct = default)
    {
        order.CreatedAt = DateTime.UtcNow;
        order.Status = "Pending";
        
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Order {OrderId} created with {ItemCount} items", order.Id, order.Items.Count);
        
        return order;
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string status, CancellationToken ct = default)
    {
        var order = await _context.Orders.FindAsync(new object[] { orderId }, ct);
        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for status update", orderId);
            return false;
        }

        order.Status = status;
        await _context.SaveChangesAsync(ct);
        
        _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
        
        return true;
    }
}
