namespace OrderService.Domain.SeedWork;

/// <summary>
/// Interfaccia marker per gli eventi di dominio.
/// Ogni evento cattura qualcosa di significativo accaduto nel dominio.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
