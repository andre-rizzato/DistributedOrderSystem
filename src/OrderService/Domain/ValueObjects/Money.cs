namespace OrderService.Domain.ValueObjects;

using OrderService.Domain.SeedWork;
using OrderService.Domain.Exceptions;

/// <summary>
/// Value Object che rappresenta un importo monetario.
/// Invariante: l'importo non può essere negativo.
/// </summary>
public class Money : ValueObject
{
    public decimal Amount { get; }

    public Money(decimal amount)
    {
        if (amount < 0)
            throw new OrderDomainException("L'importo monetario non può essere negativo.");
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
