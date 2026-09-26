namespace OrderService.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Aggregates;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Outbox;

/// <summary>
/// Repository implementation for the Order aggregate.
/// Lives in the Infrastructure layer and implements the interface defined in the Domain layer.
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

    public async Task<Order> AddAsync(Order order, Func<Order, (string Type, string Payload)> buildOutboxMessage, CancellationToken ct = default)
    {
        // Two SaveChanges calls wrapped in one transaction: the first assigns the order's
        // DB-generated Id (needed to build the event payload), the second persists the
        // outbox row alongside it. Either both commit or neither does.
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(ct);

            var (type, payload) = buildOutboxMessage(order);
            _context.OutboxMessages.Add(new OutboxMessage { Type = type, Payload = payload });
            await _context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        _logger.LogInformation(
            "Order {OrderId} persisted with {ItemCount} items and a queued outbox message",
            order.Id, order.Items.Count);

        return order;
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(ct);
    }
}
