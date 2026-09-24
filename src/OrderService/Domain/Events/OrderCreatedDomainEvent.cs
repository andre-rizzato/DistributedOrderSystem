namespace OrderService.Domain.Events;

using OrderService.Domain.SeedWork;

/// <summary>
/// Domain event raised when a new order is created.
/// </summary>
public record OrderCreatedDomainEvent(DateTime CreatedAt, decimal Total) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
