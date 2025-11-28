namespace InventoryService.Messaging;

using Confluent.Kafka;
using InventoryService.Services.Interfaces;
using Shared.Messages;
using System.Text.Json;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly string _topic;
    private bool _disposed = false;

    public OrderCreatedConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<OrderCreatedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _topic = configuration["Kafka:OrderCreatedTopic"] ?? "order-created";
        var groupId = configuration["Kafka:ConsumerGroupId"] ?? "inventory-service";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        
        _logger.LogInformation(
            "Kafka consumer initialized for topic {Topic} with group {GroupId} at {BootstrapServers}",
            _topic, groupId, bootstrapServers);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topic);
        _logger.LogInformation("Subscribed to Kafka topic: {Topic}", _topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult?.Message == null)
                        continue;

                    _logger.LogInformation(
                        "Received message from partition {Partition} at offset {Offset}",
                        consumeResult.Partition.Value,
                        consumeResult.Offset.Value);

                    await ProcessMessageAsync(consumeResult.Message.Value, stoppingToken);

                    // Commit offset after successful processing
                    _consumer.Commit(consumeResult);
                    _consumer.StoreOffset(consumeResult);

                    _logger.LogInformation(
                        "Successfully processed and committed message at offset {Offset}",
                        consumeResult.Offset.Value);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message: {Error}", ex.Error.Reason);
                    
                    // Don't commit on error - message will be reprocessed
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    
                    // Don't commit on error - message will be reprocessed
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer stopped");
        }
        finally
        {
            CloseConsumer();
        }
    }

    private void CloseConsumer()
    {
        if (!_disposed && _consumer != null)
        {
            try
            {
                _consumer.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing Kafka consumer");
            }
        }
    }

    private async Task ProcessMessageAsync(string messageValue, CancellationToken ct)
    {
        var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(messageValue);
        
        if (orderEvent == null)
        {
            _logger.LogWarning("Failed to deserialize OrderCreatedEvent");
            return;
        }

        _logger.LogInformation(
            "Processing OrderCreatedEvent for Order {OrderId} with {ItemCount} items",
            orderEvent.OrderId,
            orderEvent.Items.Count);

        using var scope = _serviceProvider.CreateScope();
        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryWorkerService>();

        // Update inventory for each item in the order
        foreach (var item in orderEvent.Items)
        {
            try
            {
                // Reduce inventory by the ordered quantity (negative delta)
                var success = await inventoryService.AdjustInventoryQuantityAsync(
                    item.ProductId,
                    -item.Quantity,
                    ct);

                if (success)
                {
                    _logger.LogInformation(
                        "Reduced inventory for Product {ProductId} by {Quantity} units (Order {OrderId})",
                        item.ProductId,
                        item.Quantity,
                        orderEvent.OrderId);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to reduce inventory for Product {ProductId} by {Quantity} units (Order {OrderId}) - insufficient inventory or product not found",
                        item.ProductId,
                        item.Quantity,
                        orderEvent.OrderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating inventory for Product {ProductId} (Order {OrderId})",
                    item.ProductId,
                    orderEvent.OrderId);
                throw; // Re-throw to prevent commit - message will be reprocessed
            }
        }

        _logger.LogInformation(
            "Completed inventory updates for Order {OrderId}",
            orderEvent.OrderId);
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            
            try
            {
                _consumer?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing Kafka consumer during dispose");
            }
            
            try
            {
                _consumer?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing Kafka consumer");
            }
            
            base.Dispose();
        }
    }
}
