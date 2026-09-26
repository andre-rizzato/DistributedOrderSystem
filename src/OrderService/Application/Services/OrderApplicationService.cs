namespace OrderService.Application.Services;

using System.Text.Json;
using OrderService.Domain.Aggregates;
using OrderService.Domain.Interfaces;
using OrderService.Domain.ValueObjects;
using Shared.Messages;

/// <summary>
/// Application service interface for orders.
/// Orchestrates domain operations and infrastructure side effects.
/// </summary>
public interface IOrderApplicationService
{
    Task<Order?> GetOrderByIdAsync(int id, CancellationToken ct = default);
    Task<List<Order>> GetAllOrdersAsync(CancellationToken ct = default);
    Task<Order> CreateOrderAsync(IEnumerable<(Guid productId, int quantity, decimal unitPrice)> items, CancellationToken ct = default);
    Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, CancellationToken ct = default);
    Task<bool?> CancelOrderAsync(int orderId, CancellationToken ct = default);
}

/// <summary>
/// Application service for orders.
/// Sits between the Controllers (Presentation) and the Domain layer.
/// Responsibility: orchestrate the domain, coordinate persistence and integration events.
/// Does NOT contain business logic — that lives in the Domain layer (aggregates, value objects, domain events).
/// </summary>
public class OrderApplicationService : IOrderApplicationService
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<OrderApplicationService> _logger;

    public OrderApplicationService(
        IOrderRepository repository,
        ILogger<OrderApplicationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Order?> GetOrderByIdAsync(int id, CancellationToken ct)
        => await _repository.GetByIdAsync(id, ct);

    public async Task<List<Order>> GetAllOrdersAsync(CancellationToken ct)
        => await _repository.GetAllAsync(ct);

    public async Task<Order> CreateOrderAsync(
        IEnumerable<(Guid productId, int quantity, decimal unitPrice)> items,
        CancellationToken ct)
    {
        // The Domain creates the aggregate (with invariants guaranteed by the factory method)
        var order = Order.Create(items);

        // Persistence + outbox message in one atomic transaction (transactional outbox) -
        // the event is durably queued in the same commit as the order, so it can no longer
        // be silently lost the way an inline "publish and swallow on failure" could lose it
        // if Kafka happened to be unreachable at this exact moment. OutboxDispatcherService
        // is what actually publishes it to Kafka, with retries, on its own schedule.
        var created = await _repository.AddAsync(order, o => (
            nameof(OrderCreatedEvent),
            JsonSerializer.Serialize(new OrderCreatedEvent
            {
                OrderId = o.Id.ToString(),
                CreatedAt = o.CreatedAt,
                Items = o.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId.ToString(),
                    Quantity = i.Quantity
                }).ToList()
            })
        ), ct);

        return created;
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(orderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for status update", orderId);
            return false;
        }

        // The Domain applies the state transition rules
        order.ChangeStatus(newStatus);

        await _repository.UpdateAsync(order, ct);
        _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, newStatus);

        return true;
    }

    public async Task<bool?> CancelOrderAsync(int orderId, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(orderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for cancellation", orderId);
            return null;
        }

        // The Domain enforces which states can transition to Cancelled
        order.ChangeStatus(OrderStatus.Cancelled.Value);

        await _repository.UpdateAsync(order, ct);
        _logger.LogInformation("Order {OrderId} canceled", orderId);

        return true;
    }
}
