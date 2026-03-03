namespace OrderService.Domain.Aggregates;

using OrderService.Domain.SeedWork;
using OrderService.Domain.ValueObjects;
using OrderService.Domain.Events;
using OrderService.Domain.Exceptions;

/// <summary>
/// Order Aggregate Root.
/// Incapsula tutte le regole di business relative agli ordini.
/// L'accesso alle entità figlie (OrderItem) avviene solo attraverso l'aggregato.
/// </summary>
public class Order : AggregateRoot
{
    public DateTime CreatedAt { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public Money Total { get; private set; } = Money.Zero;

    private readonly List<OrderItem> _items = new();

    /// <summary>
    /// Collezione di righe d'ordine (accesso in sola lettura dall'esterno).
    /// </summary>
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    // Costruttore privato richiesto da EF Core per la materializzazione
    private Order() { }

    /// <summary>
    /// Factory method: crea un nuovo ordine e solleva OrderCreatedDomainEvent.
    /// Incapsula la logica di creazione garantendo che l'aggregato sia sempre in uno stato valido.
    /// </summary>
    public static Order Create(IEnumerable<(Guid productId, int quantity, decimal unitPrice)> items)
    {
        var order = new Order
        {
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        foreach (var (productId, quantity, unitPrice) in items)
        {
            order.AddItem(productId, quantity, new Money(unitPrice));
        }

        if (order._items.Count == 0)
            throw new OrderDomainException("Un ordine deve contenere almeno un articolo.");

        order.RecalculateTotal();
        order.AddDomainEvent(new OrderCreatedDomainEvent(order.CreatedAt, order.Total.Amount));

        return order;
    }

    /// <summary>
    /// Aggiunge una riga d'ordine. Consentito solo quando l'ordine è in stato Pending.
    /// </summary>
    private void AddItem(Guid productId, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new OrderDomainException("Non è possibile aggiungere articoli a un ordine non in stato Pending.");

        var item = new OrderItem(productId, quantity, unitPrice);
        _items.Add(item);
    }

    /// <summary>
    /// Cambia lo stato dell'ordine, applicando le regole di transizione.
    /// Solleva OrderStatusChangedDomainEvent.
    /// </summary>
    public void ChangeStatus(string newStatus)
    {
        var oldStatus = Status.Value;
        Status = Status.TransitionTo(newStatus);
        AddDomainEvent(new OrderStatusChangedDomainEvent(Id, oldStatus, newStatus));
    }

    /// <summary>
    /// Ricalcola il totale dell'ordine dalla somma dei subtotali delle righe.
    /// </summary>
    private void RecalculateTotal()
    {
        Total = _items.Aggregate(Money.Zero, (sum, item) => sum.Add(item.GetSubtotal()));
    }
}
