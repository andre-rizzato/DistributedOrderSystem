namespace OrderService.Domain.Aggregates;

using OrderService.Domain.SeedWork;
using OrderService.Domain.ValueObjects;
using OrderService.Domain.Events;
using OrderService.Domain.Exceptions;

/// <summary>
/// Order Aggregate Root.
/// Encapsulates all business rules related to orders.
/// Access to child entities (OrderItem) happens only through the aggregate.
/// </summary>
public class Order : AggregateRoot
{
    public DateTime CreatedAt { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public Money Total { get; private set; } = Money.Zero;

    private readonly List<OrderItem> _items = new();

    /// <summary>
    /// Collection of order lines (read-only access from the outside).
    /// </summary>
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    // Private constructor required by EF Core for materialization
    private Order() { }

    /// <summary>
    /// Factory method: creates a new order and raises OrderCreatedDomainEvent.
    /// Encapsulates the creation logic, guaranteeing the aggregate is always in a valid state.
    /// </summary>
    public static Order Create(IEnumerable<(Guid productId, int quantity, decimal unitPrice)> items)
    {
        var order = new Order
        {
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        foreach (var (productId, quantity, unitPrice) in items)
        {
            order.AddItem(productId, quantity, new Money(unitPrice));
        }

        if (order._items.Count == 0)
            throw new OrderDomainException("An order must contain at least one item.");

        order.RecalculateTotal();
        order.AddDomainEvent(new OrderCreatedDomainEvent(order.CreatedAt, order.Total.Amount));

        return order;
    }

    /// <summary>
    /// Adds an order line. Only allowed while the order is in the Pending state.
    /// </summary>
    private void AddItem(Guid productId, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new OrderDomainException("Cannot add items to an order that is not in the Pending state.");

        var item = new OrderItem(productId, quantity, unitPrice);
        _items.Add(item);
    }

    /// <summary>
    /// Changes the order's status, applying the transition rules.
    /// Raises OrderStatusChangedDomainEvent.
    /// </summary>
    public void ChangeStatus(string newStatus)
    {
        var oldStatus = Status.Value;
        Status = Status.TransitionTo(newStatus);
        AddDomainEvent(new OrderStatusChangedDomainEvent(Id, oldStatus, newStatus));
    }

    /// <summary>
    /// Recalculates the order total from the sum of the line subtotals.
    /// </summary>
    private void RecalculateTotal()
    {
        Total = _items.Aggregate(Money.Zero, (sum, item) => sum.Add(item.GetSubtotal()));
    }
}
