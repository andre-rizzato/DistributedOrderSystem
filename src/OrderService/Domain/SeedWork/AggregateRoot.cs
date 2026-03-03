namespace OrderService.Domain.SeedWork;

/// <summary>
/// Classe base per gli Aggregate Root.
/// Gestisce la raccolta e il dispatching degli eventi di dominio.
/// Solo gli Aggregate Root possono essere persistiti direttamente tramite i Repository.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Eventi di dominio accumulati, da dispatchare dopo la persistenza.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
