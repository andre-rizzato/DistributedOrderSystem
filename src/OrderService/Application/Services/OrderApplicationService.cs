namespace OrderService.Application.Services;

using OrderService.Domain.Aggregates;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Messaging;
using Shared.Messages;

/// <summary>
/// Interfaccia del servizio applicativo per gli ordini.
/// Orchestrita le operazioni di dominio e i side-effect infrastrutturali.
/// </summary>
public interface IOrderApplicationService
{
    Task<Order?> GetOrderByIdAsync(int id, CancellationToken ct = default);
    Task<List<Order>> GetAllOrdersAsync(CancellationToken ct = default);
    Task<Order> CreateOrderAsync(IEnumerable<(Guid productId, int quantity, decimal unitPrice)> items, CancellationToken ct = default);
    Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, CancellationToken ct = default);
}

/// <summary>
/// Servizio applicativo per gli ordini.
/// Si colloca tra i Controller (Presentation) e il Domain layer.
/// Responsabilità: orchestrare il dominio, coordinare la persistenza e gli eventi di integrazione.
/// NON contiene logica di business — quella risiede nel Domain layer (aggregati, value object, domain events).
/// </summary>
public class OrderApplicationService : IOrderApplicationService
{
    private readonly IOrderRepository _repository;
    private readonly IOrderEventProducer _eventProducer;
    private readonly ILogger<OrderApplicationService> _logger;

    public OrderApplicationService(
        IOrderRepository repository,
        IOrderEventProducer eventProducer,
        ILogger<OrderApplicationService> logger)
    {
        _repository = repository;
        _eventProducer = eventProducer;
        _logger = logger;
    }

    public async Task<Order?> GetOrderByIdAsync(int id, CancellationToken ct)
        => await _repository.GetByIdAsync(id, ct);

    public async Task<List<Order>> GetAllOrdersAsync(CancellationToken ct)
        => await _repository.GetAllAsync(ct);

    public async Task<Order> CreateOrderAsync(
        IEnumerable<(Guid productId, int quantity, decimal unitPrice)> items,
        CancellationToken ct)
    {
        // Il Domain crea l'aggregato (con invarianti garantiti dal factory method)
        var order = Order.Create(items);

        // Persistenza tramite repository
        var created = await _repository.AddAsync(order, ct);

        // Pubblica evento di integrazione su Kafka (asincrono, fire-and-forget-safe)
        try
        {
            var integrationEvent = new OrderCreatedEvent
            {
                OrderId = created.Id.ToString(),
                CreatedAt = created.CreatedAt,
                Items = created.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId.ToString(),
                    Quantity = i.Quantity
                }).ToList()
            };

            await _eventProducer.PublishOrderCreatedAsync(integrationEvent, ct);
            _logger.LogInformation("Pubblicato evento OrderCreated per Ordine {OrderId}", created.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Impossibile pubblicare evento OrderCreated per Ordine {OrderId}", created.Id);
            // Non fallire la richiesta se la pubblicazione dell'evento fallisce
        }

        return created;
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(orderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Ordine {OrderId} non trovato per aggiornamento stato", orderId);
            return false;
        }

        // Il Domain applica le regole di transizione degli stati
        order.ChangeStatus(newStatus);

        await _repository.UpdateAsync(order, ct);
        _logger.LogInformation("Stato Ordine {OrderId} aggiornato a {Status}", orderId, newStatus);

        return true;
    }
}
