namespace Shared.Messages;

/// <summary>
/// Evento pubblicato su Kafka quando un ordine viene creato con successo.
/// Utilizzato per triggerare aggiornamenti asincroni dell'inventario.
/// 
/// Flusso:
/// 1. OrderService crea un nuovo ordine e lo salva nel database
/// 2. OrderService pubblica questo evento sul topic Kafka "order-created"
/// 3. InventoryService (consumer) riceve l'evento
/// 4. InventoryService riduce automaticamente lo stock per ogni prodotto nell'ordine
/// 
/// Questo pattern event-driven garantisce:
/// - Disaccoppiamento tra OrderService e InventoryService
/// - Elaborazione asincrona (OrderService non aspetta InventoryService)
/// - Resilienza: se InventoryService è down, l'ordine viene comunque salvato
/// - Scalabilità: più consumer InventoryService possono elaborare eventi in parallelo
/// </summary>
public record OrderCreatedEvent
{
    /// <summary>
    /// ID univoco dell'ordine creato.
    /// Utilizzato per tracciare quale ordine ha triggerato l'evento.
    /// </summary>
    public string OrderId { get; init; } = string.Empty;
    
    /// <summary>
    /// Timestamp UTC di quando l'ordine è stato creato.
    /// Utilizzato per audit trail e ordinamento eventi.
    /// </summary>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// Lista di prodotti ordinati con relative quantità.
    /// Ogni item contiene ProductId e Quantity da sottrarre dall'inventario.
    /// </summary>
    public List<OrderItemEvent> Items { get; init; } = new();
}

/// <summary>
/// Rappresenta un singolo prodotto all'interno di un ordine.
/// Contiene le informazioni minime necessarie per aggiornare l'inventario.
/// </summary>
public record OrderItemEvent
{
    /// <summary>
    /// ID del prodotto ordinato.
    /// Corrisponde al ProductId in ProductService e InventoryService.
    /// </summary>
    public string ProductId { get; init; } = string.Empty;
    
    /// <summary>
    /// Quantità ordinata del prodotto.
    /// InventoryService sottrae questo valore da AvailableQuantity.
    /// Esempio: Se Quantity=5, l'inventario verrà ridotto di 5 unità.
    /// </summary>
    public int Quantity { get; init; }
}
