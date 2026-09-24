namespace OrderService.Domain.ValueObjects;

using OrderService.Domain.SeedWork;
using OrderService.Domain.Exceptions;

/// <summary>
/// Value Object representing a monetary amount.
/// Invariant: the amount cannot be negative.
/// </summary>
public class Money : ValueObject
{
    public decimal Amount { get; }

    public Money(decimal amount)
    {
        if (amount < 0)
            throw new OrderDomainException("The monetary amount cannot be negative.");
        Amount = amount;
    }

    public static Money Zero => new(0);

    public Money Add(Money other) => new(Amount + other.Amount);
    public Money Multiply(int quantity) => new(Amount * quantity);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
    }

    public override string ToString() => $"{Amount:F2}";
}
