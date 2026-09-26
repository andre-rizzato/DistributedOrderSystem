namespace OrderService.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed configuration for Kafka.
/// </summary>
public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string OrderCreatedTopic { get; set; } = "order-created";
    public string InventoryReservationResultTopic { get; set; } = "inventory-reservation-result";
    public string ConsumerGroupId { get; set; } = "order-service";
}
