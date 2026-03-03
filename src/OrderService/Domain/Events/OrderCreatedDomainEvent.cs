namespace OrderService.Domain.Events;

using OrderService.Domain.SeedWork;

/// <summary>
/// Evento di dominio sollevato quando un nuovo ordine viene creato.
/// </summary>
public record OrderCreatedDomainEvent(DateTime CreatedAt, decimal Total) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
