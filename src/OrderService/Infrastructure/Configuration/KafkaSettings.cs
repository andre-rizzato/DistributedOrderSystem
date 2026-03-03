namespace OrderService.Infrastructure.Configuration;

/// <summary>
/// Configurazione tipizzata per Kafka.
/// </summary>
public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string OrderCreatedTopic { get; set; } = "order-created";
}
