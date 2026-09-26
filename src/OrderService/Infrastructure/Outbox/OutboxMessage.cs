namespace OrderService.Infrastructure.Outbox;

/// <summary>
/// Transactional outbox row. Written in the same DB transaction as the order it
/// describes, so "order persisted" and "integration event durably queued" are
/// atomic - a Kafka outage at request time can no longer lose the event the way
/// a direct inline publish-and-swallow could.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Event type name (e.g. nameof(OrderCreatedEvent)) - lets the dispatcher pick the right topic/producer call.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>JSON-serialized event payload.</summary>
    public string Payload { get; set; } = string.Empty;

    public DateTime OccurredOnUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Null until the dispatcher successfully publishes this message to Kafka.</summary>
    public DateTime? ProcessedOnUtc { get; set; }

    public int Attempts { get; set; }

    public string? Error { get; set; }
}
