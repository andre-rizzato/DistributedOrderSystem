namespace OrderService.Infrastructure.Messaging;

using Shared.Messages;

/// <summary>
/// Interfaccia per la pubblicazione di eventi di integrazione su Kafka.
/// Definita nell'Application/Infrastructure layer (è un concern di integrazione, non di dominio).
/// </summary>
public interface IOrderEventProducer
{
    Task PublishOrderCreatedAsync(OrderCreatedEvent orderEvent, CancellationToken ct = default);
}
