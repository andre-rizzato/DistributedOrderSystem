namespace InventoryService.Messaging;

using Confluent.Kafka;
using Shared.Messages;
using System.Text.Json;

/// <summary>
/// Kafka producer for InventoryReservationResultEvent. Mirrors OrderService's
/// OrderEventProducer configuration (idempotent producer, Acks.All).
/// </summary>
public class InventoryEventProducer : IInventoryEventProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;
    private readonly ILogger<InventoryEventProducer> _logger;

    public InventoryEventProducer(IConfiguration configuration, ILogger<InventoryEventProducer> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _topic = configuration["Kafka:InventoryReservationResultTopic"] ?? "inventory-reservation-result";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MaxInFlight = 5,
            MessageSendMaxRetries = 3,
            LingerMs = 10
        };

        _producer = new ProducerBuilder<string, string>(config).Build();

        _logger.LogInformation(
            "Kafka producer initialized for topic {Topic} at {BootstrapServers}",
            _topic, bootstrapServers);
    }

    public async Task PublishReservationResultAsync(InventoryReservationResultEvent resultEvent, CancellationToken ct = default)
    {
        try
        {
            var message = new Message<string, string>
            {
                Key = $"order-{resultEvent.OrderId}",
                Value = JsonSerializer.Serialize(resultEvent),
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            var result = await _producer.ProduceAsync(_topic, message, ct);

            _logger.LogInformation(
                "Published InventoryReservationResult (Success={Success}) for Order {OrderId} partition {Partition} offset {Offset}",
                resultEvent.Success, resultEvent.OrderId, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Unable to publish InventoryReservationResult for Order {OrderId}: {Error}",
                resultEvent.OrderId, ex.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing the Kafka producer");
        }
    }
}
