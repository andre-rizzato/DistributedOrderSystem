namespace OrderService.Infrastructure.Messaging;

using Shared.Messages;

/// <summary>
/// Mock del produttore Kafka per lo sviluppo locale senza Kafka.
/// </summary>
public class MockOrderEventProducer : IOrderEventProducer
{
    private readonly ILogger<MockOrderEventProducer> _logger;

    public MockOrderEventProducer(ILogger<MockOrderEventProducer> logger)
    {
        _logger = logger;
    }

    public Task PublishOrderCreatedAsync(OrderCreatedEvent orderEvent, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[MOCK] Evento OrderCreated per Ordine {OrderId} verrebbe pubblicato su Kafka",
            orderEvent.OrderId);
        return Task.CompletedTask;
    }
}
