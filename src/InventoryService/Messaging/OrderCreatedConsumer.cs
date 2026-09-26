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
        try
        {
            _logger.LogInformation("Starting Kafka consumer for topic: {Topic}", _topic);
            // Add a delay to let the host finish starting up completely
            await Task.Delay(2000, stoppingToken);
        
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
                        "Message processed and successfully committed at offset {Offset}",
                        consumeResult.Offset.Value);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message: {Error}", ex.Error.Reason);

                    // Don't commit on error - the message will be reprocessed
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");

                    // Don't commit on error - the message will be reprocessed
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
            _logger.LogWarning("Unable to deserialize OrderCreatedEvent");
            return;
        }

        _logger.LogInformation(
            "Processing OrderCreatedEvent for Order {OrderId} with {ItemCount} items",
            orderEvent.OrderId,
            orderEvent.Items.Count);

        using var scope = _serviceProvider.CreateScope();
        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryWorkerService>();
        var eventProducer = scope.ServiceProvider.GetRequiredService<IInventoryEventProducer>();

        // Saga participant: reserve stock for every line item, all-or-nothing for this order.
        // A technical failure (DB unreachable, etc.) still throws so the message is redelivered.
        // "Insufficient stock" is a business outcome, not a technical one - it stops the loop,
        // rolls back whatever was already reserved for this same order, and reports failure back
        // to OrderService instead of leaving a half-decremented order with no compensation.
        var reserved = new List<(Guid ProductId, int Quantity)>();
        string? failureReason = null;

        foreach (var item in orderEvent.Items)
        {
            var productId = Guid.Parse(item.ProductId);
            bool success;
            try
            {
                success = await inventoryService.AdjustInventoryQuantityAsync(productId, -item.Quantity, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating inventory for Product {ProductId} (Order {OrderId})",
                    item.ProductId,
                    orderEvent.OrderId);
                throw; // Rethrow to prevent commit - the message will be reprocessed
            }

            if (success)
            {
                reserved.Add((productId, item.Quantity));
                _logger.LogInformation(
                    "Reduced inventory for Product {ProductId} by {Quantity} units (Order {OrderId})",
                    item.ProductId, item.Quantity, orderEvent.OrderId);
            }
            else
            {
                failureReason = $"Insufficient inventory for product {item.ProductId}";
                _logger.LogWarning(
                    "Unable to reduce inventory for Product {ProductId} by {Quantity} units (Order {OrderId}) - insufficient inventory or product not found",
                    item.ProductId, item.Quantity, orderEvent.OrderId);
                break;
            }
        }

        if (failureReason is not null)
        {
            // Compensating action: undo every reservation already made for this order.
            foreach (var (productId, quantity) in reserved)
            {
                var rolledBack = await inventoryService.AdjustInventoryQuantityAsync(productId, quantity, ct);
                if (!rolledBack)
                {
                    _logger.LogError(
                        "Rollback failed for Product {ProductId} (Order {OrderId}) - inventory may now be inconsistent and needs manual reconciliation",
                        productId, orderEvent.OrderId);
                }
            }

            await eventProducer.PublishReservationResultAsync(new InventoryReservationResultEvent
            {
                OrderId = orderEvent.OrderId,
                Success = false,
                Reason = failureReason,
                ProcessedAt = DateTime.UtcNow
            }, ct);

            _logger.LogWarning(
                "Inventory reservation failed for Order {OrderId}: {Reason}. Rolled back {Count} already-reserved item(s).",
                orderEvent.OrderId, failureReason, reserved.Count);
            return; // Handled (a business outcome, not a fault) - commit the offset.
        }

        await eventProducer.PublishReservationResultAsync(new InventoryReservationResultEvent
        {
            OrderId = orderEvent.OrderId,
            Success = true,
            ProcessedAt = DateTime.UtcNow
        }, ct);

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
