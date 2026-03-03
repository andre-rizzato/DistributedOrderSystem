namespace OrderService.Domain.Exceptions;

/// <summary>
/// Eccezione lanciata quando un invariante di dominio viene violato.
/// </summary>
public class OrderDomainException : Exception
{
    public OrderDomainException(string message) : base(message) { }
    public OrderDomainException(string message, Exception inner) : base(message, inner) { }
}
