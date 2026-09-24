namespace OrderService.Domain.Events;

using OrderService.Domain.SeedWork;

/// <summary>
/// Domain event raised when an order's status changes.
/// </summary>
public record OrderStatusChangedDomainEvent(int OrderId, string OldStatus, string NewStatus) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
