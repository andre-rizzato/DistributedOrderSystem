namespace OrderService.Messaging;

using Confluent.Kafka;
using Shared.Messages;
using System.Text.Json;

public interface IOrderEventProducer
{
    Task PublishOrderCreatedAsync(OrderCreatedEvent orderEvent, CancellationToken ct = default);
}

public class OrderEventProducer : IOrderEventProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly ILogger<OrderEventProducer> _logger;

    public OrderEventProducer(
        IConfiguration configuration,
        ILogger<OrderEventProducer> logger)
    {
        _logger = logger;
        
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _topic = configuration["Kafka:OrderCreatedTopic"] ?? "order-created";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.Leader,
            EnableIdempotence = true,
            MaxInFlight = 5,
            MessageSendMaxRetries = 3,
            LingerMs = 10
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        
        _logger.LogInformation("Kafka producer initialized for topic {Topic} at {BootstrapServers}", 
            _topic, bootstrapServers);
    }

    public async Task PublishOrderCreatedAsync(OrderCreatedEvent orderEvent, CancellationToken ct = default)
    {
        try
        {
            var key = $"order-{orderEvent.OrderId}";
            var value = JsonSerializer.Serialize(orderEvent);

            var message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            var result = await _producer.ProduceAsync(_topic, message, ct);

            _logger.LogInformation(
                "Published OrderCreated event for Order {OrderId} to partition {Partition} at offset {Offset}",
                orderEvent.OrderId,
                result.Partition.Value,
                result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, 
                "Failed to publish OrderCreated event for Order {OrderId}: {Error}",
                orderEvent.OrderId,
                ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Unexpected error publishing OrderCreated event for Order {OrderId}",
                orderEvent.OrderId);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
            _logger.LogInformation("Kafka producer disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Kafka producer");
        }
    }
}
