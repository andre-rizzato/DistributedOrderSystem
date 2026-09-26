namespace InventoryService.Messaging;

using Shared.Messages;

/// <summary>
/// Publishes the outcome of an inventory reservation attempt back to OrderService -
/// the compensating/confirming half of the order/inventory saga.
/// </summary>
public interface IInventoryEventProducer
{
    Task PublishReservationResultAsync(InventoryReservationResultEvent resultEvent, CancellationToken ct = default);
}
