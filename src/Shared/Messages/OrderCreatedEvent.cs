namespace Shared.Messages;

/// <summary>
/// Event published to Kafka when an order is successfully created.
/// Used to trigger asynchronous inventory updates.
///
/// Flow:
/// 1. OrderService creates a new order and saves it to the database
/// 2. OrderService publishes this event to the Kafka topic "order-created"
/// 3. InventoryService (consumer) receives the event
/// 4. InventoryService automatically reduces stock for each product in the order
///
/// This event-driven pattern guarantees:
/// - Decoupling between OrderService and InventoryService
/// - Asynchronous processing (OrderService doesn't wait for InventoryService)
/// - Resilience: if InventoryService is down, the order is still saved
/// - Scalability: multiple InventoryService consumers can process events in parallel
/// </summary>
public record OrderCreatedEvent
{
    /// <summary>
    /// Unique ID of the created order.
    /// Used to track which order triggered the event.
    /// </summary>
    public string OrderId { get; init; } = string.Empty;

    /// <summary>
    /// UTC timestamp of when the order was created.
    /// Used for audit trail and event ordering.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// List of ordered products with their quantities.
    /// Each item contains the ProductId and Quantity to subtract from inventory.
    /// </summary>
    public List<OrderItemEvent> Items { get; init; } = new();
}

/// <summary>
/// Represents a single product within an order.
/// Contains the minimum information needed to update inventory.
/// </summary>
public record OrderItemEvent
{
    /// <summary>
    /// ID of the ordered product.
    /// Matches ProductId in ProductService and InventoryService.
    /// </summary>
    public string ProductId { get; init; } = string.Empty;

    /// <summary>
    /// Quantity of the product ordered.
    /// InventoryService subtracts this value from AvailableQuantity.
    /// Example: if Quantity=5, inventory will be reduced by 5 units.
    /// </summary>
    public int Quantity { get; init; }
}
