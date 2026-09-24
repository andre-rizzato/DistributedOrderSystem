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
    Task<Order> AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
