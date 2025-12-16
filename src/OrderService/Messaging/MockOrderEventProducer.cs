namespace OrderService.Messaging;

using Shared.Messages;

/// <summary>
/// Mock implementation of IOrderEventProducer for development when Kafka is not available
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
            "Mock Producer Kafka: Evento creazione Ordine {OrderId} verrebbe pubblicato su Kafka", 
            orderEvent.OrderId);
        
        return Task.CompletedTask;
    }
}