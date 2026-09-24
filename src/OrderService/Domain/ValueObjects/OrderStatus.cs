namespace OrderService.Domain.ValueObjects;

using OrderService.Domain.SeedWork;
using OrderService.Domain.Exceptions;

/// <summary>
/// Value Object representing an order's status, with valid transitions.
///
/// Allowed transitions:
///   Pending   → Confirmed, Cancelled
///   Confirmed → Shipped, Cancelled
///   Shipped   → Delivered
///   Delivered → (final state)
///   Cancelled → (final state)
/// </summary>
public class OrderStatus : ValueObject
{
    public string Value { get; }

    // Predefined states
    public static readonly OrderStatus Pending   = new("Pending");
    public static readonly OrderStatus Confirmed = new("Confirmed");
    public static readonly OrderStatus Shipped   = new("Shipped");
    public static readonly OrderStatus Delivered = new("Delivered");
    public static readonly OrderStatus Cancelled = new("Cancelled");

    private static readonly Dictionary<string, string[]> ValidTransitions = new()
    {
        { "Pending",   new[] { "Confirmed", "Cancelled" } },
        { "Confirmed", new[] { "Shipped", "Cancelled" } },
        { "Shipped",   new[] { "Delivered" } },
        { "Delivered", Array.Empty<string>() },
        { "Cancelled", Array.Empty<string>() }
    };

    private OrderStatus(string value) => Value = value;

    /// <summary>
    /// Creates an OrderStatus from a string, validating its value.
    /// </summary>
    public static OrderStatus From(string value)
    {
        if (!ValidTransitions.ContainsKey(value))
            throw new OrderDomainException($"Invalid order status: '{value}'.");
        return new OrderStatus(value);
    }

    /// <summary>
    /// Transitions to a new status, enforcing the business rules.
    /// </summary>
    public OrderStatus TransitionTo(string newStatus)
    {
        if (!ValidTransitions.TryGetValue(Value, out var allowed) || !allowed.Contains(newStatus))
            throw new OrderDomainException(
                $"Transition not allowed from '{Value}' to '{newStatus}'.");
        return From(newStatus);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
