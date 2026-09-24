namespace OrderService.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed configuration for Kafka.
/// </summary>
public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string OrderCreatedTopic { get; set; } = "order-created";
}
