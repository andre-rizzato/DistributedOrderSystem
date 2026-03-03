namespace OrderService.Domain.Interfaces;

using OrderService.Domain.Aggregates;

/// <summary>
/// Interfaccia del Repository per l'aggregato Order.
/// Definita nel Domain layer — l'implementazione risiede nell'Infrastructure layer.
/// Solo gli Aggregate Root hanno il proprio Repository.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Order>> GetAllAsync(CancellationToken ct = default);
    Task<Order> AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
