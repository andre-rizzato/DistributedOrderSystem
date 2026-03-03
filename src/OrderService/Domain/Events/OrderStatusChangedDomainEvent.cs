namespace OrderService.Domain.Events;

using OrderService.Domain.SeedWork;

/// <summary>
/// Evento di dominio sollevato quando lo stato di un ordine cambia.
/// </summary>
public record OrderStatusChangedDomainEvent(int OrderId, string OldStatus, string NewStatus) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
