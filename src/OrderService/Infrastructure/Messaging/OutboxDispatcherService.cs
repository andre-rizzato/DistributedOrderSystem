namespace OrderService.Infrastructure.Messaging;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.Data;
using Shared.Messages;

/// <summary>
/// Polls the outbox table and publishes unprocessed messages to Kafka. Decouples "the order
/// was persisted" from "Kafka happened to be reachable at that exact moment" - if a publish
/// fails, the row just stays unprocessed and is retried on the next poll (up to MaxAttempts),
/// instead of the event being lost the way an inline publish-and-swallow would lose it.
/// </summary>
public class OutboxDispatcherService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private const int MaxAttempts = 5;
    private const int BatchSize = 20;

    private readonly IServiceProvider _serviceProvider;
    private readonly IOrderEventProducer _producer;
    private readonly ILogger<OutboxDispatcherService> _logger;

    public OutboxDispatcherService(IServiceProvider serviceProvider, IOrderEventProducer producer, ILogger<OutboxDispatcherService> logger)
    {
        _serviceProvider = serviceProvider;
        _producer = producer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching outbox messages");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task DispatchPendingMessagesAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderContext>();

        var pending = await context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null && m.Attempts < MaxAttempts)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0)
            return;

        foreach (var message in pending)
        {
            try
            {
                if (message.Type == nameof(OrderCreatedEvent))
                {
                    var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Payload)
                        ?? throw new InvalidOperationException("Unable to deserialize OrderCreatedEvent from outbox payload");

                    await _producer.PublishOrderCreatedAsync(evt, ct);
                }
                else
                {
                    _logger.LogWarning(
                        "Unknown outbox message type {Type} (Id {MessageId}) - marking as processed instead of retrying forever",
                        message.Type, message.Id);
                }

                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.Error = ex.Message;
                _logger.LogWarning(ex,
                    "Failed to dispatch outbox message {MessageId} (attempt {Attempt}/{MaxAttempts})",
                    message.Id, message.Attempts, MaxAttempts);
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
