namespace OrderService.Infrastructure.Messaging;

using System.Text.Json;
using Confluent.Kafka;
using OrderService.Application.Services;
using OrderService.Domain.Exceptions;
using Shared.Messages;

/// <summary>
/// Saga participant: consumes InventoryService's reservation outcome and applies the
/// compensating/confirming action on the order - Confirmed on success, Cancelled
/// (compensating action) when stock couldn't be reserved.
/// </summary>
public class InventoryReservationResultConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventoryReservationResultConsumer> _logger;
    private readonly string _topic;
    private bool _disposed;

    public InventoryReservationResultConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<InventoryReservationResultConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _topic = configuration["Kafka:InventoryReservationResultTopic"] ?? "inventory-reservation-result";
        var groupId = configuration["Kafka:ConsumerGroupId"] ?? "order-service";

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
            await Task.Delay(2000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    if (consumeResult?.Message == null)
                        continue;

                    await ProcessMessageAsync(consumeResult.Message.Value, stoppingToken);

                    _consumer.Commit(consumeResult);
                    _consumer.StoreOffset(consumeResult);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message: {Error}", ex.Error.Reason);
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

    private async Task ProcessMessageAsync(string messageValue, CancellationToken ct)
    {
        var result = JsonSerializer.Deserialize<InventoryReservationResultEvent>(messageValue);
        if (result is null || !int.TryParse(result.OrderId, out var orderId))
        {
            _logger.LogWarning("Unable to parse InventoryReservationResultEvent: {Message}", messageValue);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var appService = scope.ServiceProvider.GetRequiredService<IOrderApplicationService>();

        try
        {
            if (result.Success)
            {
                await appService.UpdateOrderStatusAsync(orderId, "Confirmed", ct);
                _logger.LogInformation("Order {OrderId} confirmed after successful inventory reservation", orderId);
            }
            else
            {
                await appService.CancelOrderAsync(orderId, ct);
                _logger.LogWarning("Order {OrderId} canceled - inventory reservation failed: {Reason}", orderId, result.Reason);
            }
        }
        catch (OrderDomainException ex)
        {
            // The order already moved to a state that doesn't allow this transition (e.g. a
            // customer canceled it manually before this event arrived). Not a transient error -
            // log and move on instead of blocking the consumer by retrying forever.
            _logger.LogWarning(ex, "Could not apply inventory reservation result to Order {OrderId}: {Message}", orderId, ex.Message);
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
