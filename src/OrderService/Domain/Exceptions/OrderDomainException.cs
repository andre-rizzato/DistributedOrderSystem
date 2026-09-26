namespace OrderService.Domain.Exceptions;

/// <summary>
/// Thrown when a domain invariant is violated.
/// </summary>
public class OrderDomainException : Exception
{
    public OrderDomainException(string message) : base(message) { }
    public OrderDomainException(string message, Exception inner) : base(message, inner) { }
}
