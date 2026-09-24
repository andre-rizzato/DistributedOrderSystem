namespace OrderService.Infrastructure.Messaging;

using Shared.Messages;

/// <summary>
/// Interface for publishing integration events to Kafka.
/// Defined in the Application/Infrastructure layer (it's an integration concern, not a domain one).
/// </summary>
public interface IOrderEventProducer
{
    Task PublishOrderCreatedAsync(OrderCreatedEvent orderEvent, CancellationToken ct = default);
}
