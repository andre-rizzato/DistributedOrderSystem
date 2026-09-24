namespace OrderService.Domain.SeedWork;

/// <summary>
/// Base class for Aggregate Roots.
/// Handles collecting and dispatching domain events.
/// Only Aggregate Roots can be persisted directly through Repositories.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Accumulated domain events, to be dispatched after persistence.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
