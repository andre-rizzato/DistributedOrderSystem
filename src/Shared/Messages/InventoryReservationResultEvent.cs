namespace Shared.Messages;

/// <summary>
/// Published by InventoryService after it finishes processing an OrderCreatedEvent -
/// the compensating half of the order/inventory saga. OrderService consumes this to
/// move the order from Pending to either Confirmed (Success) or Cancelled (compensating
/// action, when stock couldn't be reserved for one or more items).
/// </summary>
public record InventoryReservationResultEvent
{
    /// <summary>
    /// Same OrderId carried by the OrderCreatedEvent that triggered this reservation attempt.
    /// </summary>
    public string OrderId { get; init; } = string.Empty;

    /// <summary>
    /// True if every line item's stock was successfully reserved (decremented).
    /// False if any line item couldn't be reserved - in that case InventoryService has
    /// already rolled back whatever it decremented for the other lines of the same order,
    /// so a False result means "no net inventory change happened for this order".
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Human-readable reason when Success is false (e.g. which product ran out of stock).
    /// </summary>
    public string? Reason { get; init; }

    public DateTime ProcessedAt { get; init; }
}
