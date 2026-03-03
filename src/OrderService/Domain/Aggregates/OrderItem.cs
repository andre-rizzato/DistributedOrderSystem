namespace OrderService.Domain.Aggregates;

using OrderService.Domain.SeedWork;
using OrderService.Domain.ValueObjects;
using OrderService.Domain.Exceptions;

/// <summary>
/// Entità che rappresenta una riga d'ordine all'interno dell'aggregato Order.
/// Non può esistere indipendentemente da un Order (cycle di vita gestito dall'aggregato).
/// </summary>
public class OrderItem : Entity
{
    public int OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero;

    // Costruttore privato richiesto da EF Core per la materializzazione
    private OrderItem() { }

    /// <summary>
    /// Crea una nuova riga d'ordine, validando gli invarianti.
    /// </summary>
    internal OrderItem(Guid productId, int quantity, Money unitPrice)
    {
        if (productId == Guid.Empty)
            throw new OrderDomainException("Il ProductId non può essere vuoto.");
        if (quantity <= 0)
            throw new OrderDomainException("La quantità deve essere maggiore di zero.");

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice ?? throw new OrderDomainException("Il prezzo unitario è obbligatorio.");
    }

    /// <summary>
    /// Calcola il subtotale per questa riga d'ordine.
    /// </summary>
    public Money GetSubtotal() => UnitPrice.Multiply(Quantity);
}
