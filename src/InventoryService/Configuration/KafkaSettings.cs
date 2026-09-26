namespace InventoryService.Configuration;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string OrderCreatedTopic { get; set; } = "order-created";
    public string InventoryReservationResultTopic { get; set; } = "inventory-reservation-result";
    public string ConsumerGroupId { get; set; } = "inventory-service";
}
