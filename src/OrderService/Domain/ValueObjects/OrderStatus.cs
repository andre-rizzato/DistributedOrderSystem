namespace OrderService.Domain.ValueObjects;

using OrderService.Domain.SeedWork;
using OrderService.Domain.Exceptions;

/// <summary>
/// Value Object che rappresenta lo stato di un ordine con transizioni valide.
/// 
/// Transizioni consentite:
///   Pending   → Confirmed, Cancelled
///   Confirmed → Shipped, Cancelled
///   Shipped   → Delivered
///   Delivered → (stato finale)
///   Cancelled → (stato finale)
/// </summary>
public class OrderStatus : ValueObject
{
    public string Value { get; }

    // Stati predefiniti
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
    /// Crea un OrderStatus da una stringa, validandone il valore.
    /// </summary>
    public static OrderStatus From(string value)
    {
        if (!ValidTransitions.ContainsKey(value))
            throw new OrderDomainException($"Stato ordine non valido: '{value}'.");
        return new OrderStatus(value);
    }

    /// <summary>
    /// Transiziona verso un nuovo stato, applicando le regole di business.
    /// </summary>
    public OrderStatus TransitionTo(string newStatus)
    {
        if (!ValidTransitions.TryGetValue(Value, out var allowed) || !allowed.Contains(newStatus))
            throw new OrderDomainException(
                $"Transizione non consentita da '{Value}' a '{newStatus}'.");
        return From(newStatus);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
