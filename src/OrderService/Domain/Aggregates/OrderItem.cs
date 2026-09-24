namespace OrderService.Domain.Aggregates;

using OrderService.Domain.SeedWork;
using OrderService.Domain.ValueObjects;
using OrderService.Domain.Exceptions;

/// <summary>
/// Entity representing an order line within the Order aggregate.
/// Cannot exist independently of an Order (lifecycle managed by the aggregate).
/// </summary>
public class OrderItem : Entity
{
    public int OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero;

    // Private constructor required by EF Core for materialization
    private OrderItem() { }

    /// <summary>
    /// Creates a new order line, validating the invariants.
    /// </summary>
    internal OrderItem(Guid productId, int quantity, Money unitPrice)
    {
        if (productId == Guid.Empty)
            throw new OrderDomainException("ProductId cannot be empty.");
        if (quantity <= 0)
            throw new OrderDomainException("Quantity must be greater than zero.");

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice ?? throw new OrderDomainException("Unit price is required.");
    }

    /// <summary>
    /// Calculates the subtotal for this order line.
    /// </summary>
    public Money GetSubtotal() => UnitPrice.Multiply(Quantity);
}
