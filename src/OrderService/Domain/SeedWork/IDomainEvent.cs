namespace OrderService.Domain.SeedWork;

/// <summary>
/// Marker interface for domain events.
/// Each event captures something significant that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
