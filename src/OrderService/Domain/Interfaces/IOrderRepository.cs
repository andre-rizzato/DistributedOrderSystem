namespace OrderService.Domain.Interfaces;

using OrderService.Domain.Aggregates;

/// <summary>
/// Repository interface for the Order aggregate.
/// Defined in the Domain layer — the implementation lives in the Infrastructure layer.
/// Only Aggregate Roots have their own Repository.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Order>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Persists a new order and an outbox message describing its integration event in one
    /// atomic transaction (transactional outbox). <paramref name="buildOutboxMessage"/> is
    /// invoked after the order's Id has been generated but before the transaction commits,
    /// so it can embed the real, DB-assigned order id in the event payload it returns.
    /// </summary>
    Task<Order> AddAsync(Order order, Func<Order, (string Type, string Payload)> buildOutboxMessage, CancellationToken ct = default);

    Task UpdateAsync(Order order, CancellationToken ct = default);
}
