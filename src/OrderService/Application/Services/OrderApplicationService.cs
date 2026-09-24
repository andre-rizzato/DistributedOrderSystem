namespace OrderService.Application.Services;

using OrderService.Domain.Aggregates;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Messaging;
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
    private readonly IOrderEventProducer _eventProducer;
    private readonly ILogger<OrderApplicationService> _logger;

    public OrderApplicationService(
        IOrderRepository repository,
        IOrderEventProducer eventProducer,
        ILogger<OrderApplicationService> logger)
    {
        _repository = repository;
        _eventProducer = eventProducer;
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

        // Persistence via repository
        var created = await _repository.AddAsync(order, ct);

        // Publish integration event to Kafka (asynchronous, fire-and-forget-safe)
        try
        {
            var integrationEvent = new OrderCreatedEvent
            {
                OrderId = created.Id.ToString(),
                CreatedAt = created.CreatedAt,
                Items = created.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId.ToString(),
                    Quantity = i.Quantity
                }).ToList()
            };

            await _eventProducer.PublishOrderCreatedAsync(integrationEvent, ct);
            _logger.LogInformation("Published OrderCreated event for Order {OrderId}", created.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish OrderCreated event for Order {OrderId}", created.Id);
            // Do not fail the request if publishing the event fails
        }

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
}
