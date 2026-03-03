namespace OrderService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Aggregates;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Data;

/// <summary>
/// Implementazione del Repository per l'aggregato Order.
/// Risiede nell'Infrastructure layer e implementa l'interfaccia definita nel Domain layer.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly OrderContext _context;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(OrderContext context, ILogger<OrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Ordine {OrderId} persistito con {ItemCount} articoli",
            order.Id, order.Items.Count);

        return order;
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(ct);
    }
}
