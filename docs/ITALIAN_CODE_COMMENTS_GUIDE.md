# Guida Completa: Commenti Italiani per Tutto il Codice

## Panoramica
Questo documento fornisce commenti italiani dettagliati per TUTTO il codice implementato nel sistema, organizzato per servizio e file. Usa questa guida per comprendere ogni componente del sistema.

---

# SHARED PROJECT

## src/Shared/Messages/OrderCreatedEvent.cs

```csharp
namespace Shared.Messages;

/// <summary>
/// Evento Kafka pubblicato quando un ordine viene creato.
/// 
/// SCOPO: Comunicare asincronamente tra OrderService e InventoryService
/// FLUSSO:
/// 1. OrderService crea ordine → Salva DB → Pubblica questo evento
/// 2. Kafka memorizza evento nel topic "order-created"
/// 3. InventoryService consumer riceve evento
/// 4. InventoryService riduce stock automaticamente
/// 
/// VANTAGGI PATTERN:
/// - Disaccoppiamento: Servizi indipendenti
/// - Non-blocking: OrderService non aspetta InventoryService
/// - Resilienza: Se Inventory down, ordine comunque salvato
/// - Scalabilità: Multipli consumer possono elaborare in parallelo
/// </summary>
public record OrderCreatedEvent
{
    /// <summary>
    /// ID univoco ordine creato. Usato per tracciamento e audit.
    /// </summary>
    public int OrderId { get; init; }
    
    /// <summary>
    /// Timestamp UTC creazione ordine. Per ordinamento cronologico eventi.
    /// </summary>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// Lista prodotti ordinati. Ogni item ha ProductId + Quantity.
    /// InventoryService usa questa lista per ridurre stock.
    /// </summary>
    public List<OrderItemEvent> Items { get; init; } = new();
}

/// <summary>
/// Singolo prodotto nell'ordine. Contiene solo dati essenziali per inventario.
/// </summary>
public record OrderItemEvent
{
    /// <summary>
    /// ID prodotto. Corrisponde a ProductId in ProductService/InventoryService.
    /// </summary>
    public int ProductId { get; init; }
    
    /// <summary>
    /// Quantità ordinata. Verrà sottratta da AvailableQuantity inventario.
    /// Esempio: Se Quantity=5, inventario ridotto di 5 unità.
    /// </summary>
    public int Quantity { get; init; }
}
```

---

# ORDERSERVICE

## src/OrderService/Models/Order.cs

```csharp
namespace OrderService.Models;

/// <summary>
/// Entità principale che rappresenta un ordine cliente.
/// 
/// AGGREGATE ROOT: Order contiene OrderItems come entità figlie.
/// PATTERN: Aggregate Pattern di DDD (Domain-Driven Design)
/// 
/// LIFECYCLE ORDINE:
/// 1. Status = "Pending" → Ordine creato, in attesa pagamento
/// 2. Status = "Paid" → Pagamento confermato
/// 3. Status = "Processing" → In elaborazione/preparazione
/// 4. Status = "Shipped" → Spedito
/// 5. Status = "Delivered" → Consegnato
/// 6. Status = "Cancelled" → Cancellato
/// </summary>
public class Order
{
    /// <summary>
    /// Chiave primaria auto-incrementale. Identificatore univoco ordine.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Timestamp UTC creazione ordine. Immutabile dopo creazione.
    /// Usato per ordinamento, reportistica, statistiche.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Stato attuale ordine. Default "Pending".
    /// Valori possibili: Pending, Paid, Processing, Shipped, Delivered, Cancelled
    /// Aggiornato da PaymentService quando pagamento completato.
    /// </summary>
    public string Status { get; set; } = "Pending";
    
    /// <summary>
    /// Totale ordine in euro. Calcolato come somma di (Quantity * UnitPrice) per tutti Items.
    /// Formula: Total = Σ(Item.Quantity * Item.UnitPrice)
    /// </summary>
    public decimal Total { get; set; }
    
    /// <summary>
    /// Collection di prodotti ordinati. Relazione 1-to-Many con OrderItem.
    /// Ogni item rappresenta un prodotto + quantità + prezzo al momento dell'ordine.
    /// EF Core carica automaticamente questa collection con Include().
    /// </summary>
    public List<OrderItem> Items { get; set; } = new();
}

/// <summary>
/// Singola riga ordine che rappresenta un prodotto ordinato.
/// 
/// PATTERN: Weak Entity - dipende da Order (non esiste senza Order padre)
/// 
/// PERCHÉ SALVIAMO UNITPRICE?
/// - Snapshot del prezzo al momento ordine (i prezzi cambiano nel tempo)
/// - Se Product.Price cambia domani, l'ordine storico mantiene prezzo originale
/// - Importante per reportistica, fatturazione, analisi storiche
/// </summary>
public class OrderItem
{
    /// <summary>
    /// Chiave primaria auto-incrementale. ID univoco riga ordine.
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Foreign Key verso Order padre. Identifica a quale ordine appartiene.
    /// EF Core usa questo per relazione Order → OrderItems.
    /// </summary>
    public int OrderId { get; set; }
    
    /// <summary>
    /// ID del prodotto ordinato. Riferimento logico a ProductService.
    /// NON è Foreign Key fisica (microservizi hanno database separati).
    /// Usato per query inventario, dettagli prodotto via BFF.
    /// </summary>
    public int ProductId { get; set; }
    
    /// <summary>
    /// Quantità ordinata del prodotto. Deve essere > 0.
    /// Frontend valida prima di inviare, backend ri-valida.
    /// Questa quantità viene sottratta dall'inventario via Kafka event.
    /// </summary>
    public int Quantity { get; set; }
    
    /// <summary>
    /// Prezzo unitario prodotto AL MOMENTO DELL'ORDINE.
    /// Snapshot immutabile - anche se prezzo cambia nel catalogo, questo rimane fisso.
    /// Usato per calcolare Total ordine e subtotali.
    /// LineTotal = Quantity * UnitPrice
    /// </summary>
    public decimal UnitPrice { get; set; }
}
```

## src/OrderService/Controllers/OrdersController.cs

```csharp
namespace OrderService.Controllers;

using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Services;
using OrderService.Messaging;
using Shared.Messages;

/// <summary>
/// REST API Controller per QUERY ordini (solo lettura).
/// 
/// PATTERN IMPLEMENTATO: CQRS (Command Query Responsibility Segregation)
/// - Queries (questo controller): Operazioni lettura
/// - Commands (OrderCommandsController): Operazioni scrittura
/// 
/// ENDPOINTS:
/// - GET /api/orders → Lista tutti ordini
/// - GET /api/orders/{id} → Dettaglio singolo ordine con items
/// </summary>
[ApiController]
[Route("api/[controller]")]  // Route: /api/orders
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;  // Business logic service
    private readonly ILogger<OrdersController> _logger;  // Logging strutturato
    
    /// <summary>
    /// Constructor con Dependency Injection.
    /// ASP.NET Core IoC container inietta automaticamente dipendenze.
    /// </summary>
    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }
    
    /// <summary>
    /// Recupera lista di tutti gli ordini.
    /// 
    /// UTILIZZO:
    /// - Frontend per visualizzare storico ordini
    /// - Admin dashboard per reportistica
    /// 
    /// OTTIMIZZAZIONI POSSIBILI:
    /// - Paginazione (page, pageSize query params)
    /// - Filtri (status, dateFrom, dateTo)
    /// - Ordinamento (orderBy, direction)
    /// 
    /// RESPONSE: HTTP 200 OK + JSON array di ordini
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Order>>> GetAllOrders(CancellationToken ct)
    {
        var orders = await _orderService.GetAllOrdersAsync(ct);
        return Ok(orders);
    }
    
    /// <summary>
    /// Recupera dettagli di un singolo ordine con tutti items.
    /// 
    /// ROUTE CONSTRAINT: {id:int} → Solo numeri accettati, altrimenti 404
    /// 
    /// UTILIZZO:
    /// - Frontend pagina dettaglio ordine
    /// - Conferma post-acquisto
    /// - Tracking ordine
    /// 
    /// RESPONSE:
    /// - 200 OK + Order JSON (se trovato)
    /// - 404 Not Found (se ID non esiste)
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Order>> GetOrder(int id, CancellationToken ct)
    {
        var order = await _orderService.GetOrderByIdAsync(id, ct);
        if (order == null)
            return NotFound($"Order {id} not found");
        
        return Ok(order);
    }
}

/// <summary>
/// REST API Controller per COMMANDS ordini (scrittura/modifiche).
/// 
/// PATTERN: CQRS - Separato da queries per:
/// - Validazioni complesse
/// - Side effects (pubblicazione eventi Kafka)
/// - Transazioni database
/// - Logging audit
/// 
/// ENDPOINTS:
/// - POST /api/commands/orders → Crea nuovo ordine + pubblica evento Kafka
/// - PUT /api/commands/orders/{id}/status → Aggiorna stato ordine
/// </summary>
[ApiController]
[Route("api/commands")]  // Route base: /api/commands
public class OrderCommandsController : ControllerBase
{
    private readonly IOrderService _orderService;  // Business logic
    private readonly IOrderEventProducer _eventProducer;  // Kafka producer
    private readonly ILogger<OrderCommandsController> _logger;  // Logging
    
    /// <summary>
    /// Constructor con DI. Inietta service, producer Kafka, logger.
    /// </summary>
    public OrderCommandsController(
        IOrderService orderService, 
        IOrderEventProducer eventProducer,
        ILogger<OrderCommandsController> logger)
    {
        _orderService = orderService;
        _eventProducer = eventProducer;
        _logger = logger;
    }
    
    // --- DTOs (Data Transfer Objects) per request/response ---
    
    /// <summary>
    /// DTO per richiesta creazione ordine.
    /// Record C# 10+ = immutabile, conciso, value equality.
    /// </summary>
    public record CreateOrderRequest(List<OrderItemDto> Items);
    
    /// <summary>
    /// DTO per singolo prodotto da ordinare.
    /// Frontend invia: ProductId, Quantity, UnitPrice (dal catalogo).
    /// </summary>
    public record OrderItemDto(int ProductId, int Quantity, decimal UnitPrice);
    
    /// <summary>
    /// DTO per risposta creazione ordine.
    /// Contiene solo dati essenziali per frontend.
    /// </summary>
    public record CreateOrderResponse(int OrderId, string Status, decimal Total);
    
    /// <summary>
    /// Crea nuovo ordine e pubblica evento Kafka per aggiornamento inventario asincrono.
    /// 
    /// FLUSSO COMPLETO:
    /// 1. Valida input (almeno 1 item richiesto)
    /// 2. Crea entità Order + OrderItems
    /// 3. Calcola Total somma (Quantity * UnitPrice)
    /// 4. Salva nel database (transazione)
    /// 5. Pubblica OrderCreatedEvent su Kafka
    /// 6. Ritorna HTTP 201 Created + Location header
    /// 
    /// GESTIONE ERRORI:
    /// - 400 Bad Request: Se Items vuoto o null
    /// - 201 Created: Ordine salvato con successo (anche se Kafka fallisce!)
    /// - 500 Internal Server Error: Errori database
    /// 
    /// NOTA IMPORTANTE:
    /// Se pubblicazione Kafka fallisce, l'ordine viene COMUNQUE salvato.
    /// Questo è intenzionale per resilienza - meglio ordine salvato senza
    /// aggiornamento inventario (recuperabile) che perdere ordine.
    /// 
    /// CHIAMATO DA:
    /// - Frontend CreateOrderComponent quando user clicca "Conferma Ordine"
    /// - GatewayBff /api/commands/orders (proxy)
    /// </summary>
    [HttpPost("orders")]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        // 1. VALIDAZIONE INPUT
        if (request?.Items == null || request.Items.Count == 0)
            return BadRequest("At least one item is required");
        
        // 2. CREAZIONE ENTITÀ ORDER
        var order = new Order
        {
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
        
        // 3. CALCOLO TOTALE
        // Formula: Total = Σ(Quantity * UnitPrice) per ogni item
        order.Total = order.Items.Sum(i => i.Quantity * i.UnitPrice);
        
        // 4. SALVATAGGIO DATABASE (via Service Layer)
        var created = await _orderService.CreateOrderAsync(order, ct);
        
        // 5. PUBBLICAZIONE EVENTO KAFKA (asincrono, non-blocking)
        try
        {
            // Mappa Order → OrderCreatedEvent
            var orderEvent = new OrderCreatedEvent
            {
                OrderId = created.Id,
                CreatedAt = created.CreatedAt,
                Items = created.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity  // Solo quantità serve per inventario
                }).ToList()
            };
            
            // Pubblica su topic "order-created"
            await _eventProducer.PublishOrderCreatedAsync(orderEvent, ct);
            _logger.LogInformation("Published OrderCreated event for Order {OrderId}", created.Id);
        }
        catch (Exception ex)
        {
            // Log errore MA NON FALLIRE richiesta
            // Ordine già salvato, evento può essere ripubblicato manualmente
            _logger.LogError(ex, "Failed to publish OrderCreated event for Order {OrderId}", created.Id);
        }
        
        // 6. RITORNO RESPONSE HTTP 201 Created
        // Location header: /api/orders/{id} per query dettaglio
        return CreatedAtAction(
            nameof(OrdersController.GetOrder),  // Nome action per Location header
            "Orders",  // Controller name
            new { id = created.Id },  // Route values
            new CreateOrderResponse(created.Id, created.Status, created.Total)  // Body
        );
    }
    
    /// <summary>
    /// DTO per richiesta aggiornamento stato ordine.
    /// </summary>
    public record UpdateOrderStatusRequest(string Status);
    
    /// <summary>
    /// Aggiorna stato di un ordine esistente.
    /// 
    /// UTILIZZO:
    /// - PaymentService: Pending → Paid (dopo pagamento confermato)
    /// - Warehouse: Paid → Processing → Shipped
    /// - Admin: Cancelled (cancellazione manuale)
    /// 
    /// STATI VALIDI:
    /// Pending → Paid → Processing → Shipped → Delivered
    ///        ↘ Cancelled
    /// 
    /// RESPONSE:
    /// - 204 No Content: Update successo
    /// - 404 Not Found: Order ID non esiste
    /// - 400 Bad Request: Status vuoto/null
    /// 
    /// CHIAMATO DA:
    /// - PaymentService consumer Kafka (aggiorna a "Paid")
    /// - Admin dashboard
    /// - Warehouse management system
    /// </summary>
    [HttpPut("orders/{id:int}/status")]
    public async Task<ActionResult> UpdateOrderStatus(
        int id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken ct)
    {
        // Validazione input
        if (string.IsNullOrWhiteSpace(request?.Status))
            return BadRequest("Status is required");
        
        // Update via service
        var success = await _orderService.UpdateOrderStatusAsync(id, request.Status, ct);
        if (!success)
            return NotFound($"Order {id} not found");
        
        // 204 No Content = Success senza body
        return NoContent();
    }
}
```

## src/OrderService/Messaging/OrderEventProducer.cs

```csharp
namespace OrderService.Messaging;

using Confluent.Kafka;
using Shared.Messages;
using System.Text.Json;

/// <summary>
/// Interfaccia per producer eventi ordine.
/// Permette mock/testing e decoupling da implementazione Kafka specifica.
/// </summary>
public interface IOrderEventProducer
{
    /// <summary>
    /// Pubblica evento OrderCreated su Kafka topic "order-created".
    /// </summary>
    Task PublishOrderCreatedAsync(OrderCreatedEvent orderEvent, CancellationToken ct = default);
}

/// <summary>
/// Implementazione Kafka producer per pubblicare eventi ordine.
/// 
/// RESPONSABILITÀ:
/// - Configurare producer Kafka con settings ottimali
/// - Serializzare eventi in JSON
/// - Pubblicare su topic configurato
/// - Logging dettagliato successo/errori
/// - Dispose corretto delle risorse
/// 
/// PATTERN:
/// - Producer Pattern: Invia messaggi a message broker
/// - IDisposable: Cleanup risorse Kafka
/// - Singleton Lifetime: Un solo producer per applicazione (performance)
/// 
/// KAFKA CONFIGURATION SPIEGATA:
/// - BootstrapServers: Indirizzo broker Kafka (localhost:9092 dev)
/// - Acks.Leader: Wait conferma dal leader partition (bilanciamento performance/reliability)
/// - EnableIdempotence: Previene messaggi duplicati (exactly-once semantics)
/// - MaxInFlight: Max 5 richieste parallele (throughput)
/// - MessageSendMaxRetries: Retry automatico 3 volte su errori transitori
/// - LingerMs: Buffer 10ms per batch messages (efficienza rete)
/// </summary>
public class OrderEventProducer : IOrderEventProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;  // Kafka producer (string key, string value JSON)
    private readonly string _topic;  // Topic name (configurabile)
    private readonly ILogger<OrderEventProducer> _logger;  // Structured logging
    
    /// <summary>
    /// Constructor: Inizializza producer Kafka con configurazione ottimizzata.
    /// Eseguito una sola volta all'avvio applicazione (Singleton).
    /// </summary>
    public OrderEventProducer(
        IConfiguration configuration,
        ILogger<OrderEventProducer> logger)
    {
        _logger = logger;
        
        // Leggi configurazione da appsettings.json
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _topic = configuration["Kafka:OrderCreatedTopic"] ?? "order-created";
        
        // CONFIGURAZIONE PRODUCER KAFKA
        var config = new ProducerConfig
        {
            // Indirizzo broker Kafka
            BootstrapServers = bootstrapServers,
            
            // Acks.Leader: Wait conferma solo da leader partition
            // Alternative: Acks.None (no wait, veloce), Acks.All (wait tutte replicas, slow ma safe)
            Acks = Acks.Leader,
            
            // Idempotency: Previene duplicati anche su retry
            // Kafka assegna sequence number, scarta duplicati
            EnableIdempotence = true,
            
            // Max richieste parallele in-flight
            // Più alto = maggior throughput, ma ordine garantito solo con Idempotence
            MaxInFlight = 5,
            
            // Retry automatici su errori transitori (network glitches)
            MessageSendMaxRetries = 3,
            
            // Buffer messages per 10ms prima di inviare batch
            // Trade-off: Latency vs efficiency
            LingerMs = 10
        };
        
        // Crea producer Kafka
        _producer = new ProducerBuilder<string, string>(config).Build();
        
        _logger.LogInformation("Kafka producer initialized for topic {Topic} at {BootstrapServers}", 
            _topic, bootstrapServers);
    }
    
    /// <summary>
    /// Pubblica OrderCreatedEvent su Kafka topic.
    /// 
    /// FLUSSO:
    /// 1. Crea chiave messaggio: "order-{orderId}" (per partitioning)
    /// 2. Serializza evento in JSON string
    /// 3. Crea Message Kafka con key, value, timestamp
    /// 4. ProduceAsync invia a Kafka broker
    /// 5. Wait ack da broker (configurato con Acks.Leader)
    /// 6. Log successo con partition + offset
    /// 7. Se errore, log + rilancia exception
    /// 
    /// KAFKA MESSAGE STRUCTURE:
    /// - Key: "order-123" → Determina partition (stesso ordine sempre stessa partition = ordering)
    /// - Value: JSON serialized event
    /// - Timestamp: UTC now
    /// - Headers: (opzionale, non usato qui)
    /// 
    /// PARTITIONING:
    /// Kafka usa hash(key) % num_partitions per decidere partition.
    /// Stessa chiave → stessa partition → ordine garantito per singolo ordine.
    /// 
    /// ERRORI POSSIBILI:
    /// - ProduceException: Errori Kafka specifici (broker down, timeout, quota exceeded)
    /// - Exception: Altri errori (serialization, network)
    /// Entrambi loggati e rilanciati per gestione chiamante.
    /// </summary>
    public async Task PublishOrderCreatedAsync(OrderCreatedEvent orderEvent, CancellationToken ct = default)
    {
        try
        {
            // 1. Crea message key
            // Pattern: "order-{id}" garantisce che tutti messaggi stesso ordine
            // vadano nella stessa partition (ordering garantito)
            var key = $"order-{orderEvent.OrderId}";
            
            // 2. Serializza evento in JSON
            var value = JsonSerializer.Serialize(orderEvent);
            
            // 3. Crea Kafka message
            var message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Timestamp = new Timestamp(DateTime.UtcNow)
            };
            
            // 4. Pubblica asincrono su Kafka
            // ProduceAsync ritorna DeliveryResult con metadata
            var result = await _producer.ProduceAsync(_topic, message, ct);
            
            // 5. Log successo con dettagli
            _logger.LogInformation(
                "Published OrderCreated event for Order {OrderId} to partition {Partition} at offset {Offset}",
                orderEvent.OrderId,
                result.Partition.Value,  // Partition dove scritto (0, 1, 2...)
                result.Offset.Value);     // Offset nella partition (posizione messaggio)
        }
        catch (ProduceException<string, string> ex)
        {
            // Errori specifici Kafka
            _logger.LogError(ex, 
                "Failed to publish OrderCreated event for Order {OrderId}: {Error}",
                orderEvent.OrderId,
                ex.Error.Reason);
            throw;  // Rilancia per gestione controller
        }
        catch (Exception ex)
        {
            // Altri errori generici
            _logger.LogError(ex, 
                "Unexpected error publishing OrderCreated event for Order {OrderId}",
                orderEvent.OrderId);
            throw;
        }
    }
    
    /// <summary>
    /// Dispose producer Kafka e flush messaggi pending.
    /// 
    /// Chiamato da ASP.NET Core quando applicazione si ferma (graceful shutdown).
    /// 
    /// IMPORTANTE:
    /// - Flush(10s): Attende max 10 secondi per inviare messaggi in buffer
    /// - Previene perdita messaggi durante shutdown
    /// - Dispose: Libera connessioni socket, memoria
    /// </summary>
    public void Dispose()
    {
        try
        {
            // Flush pending messages (max 10 secondi wait)
            _producer?.Flush(TimeSpan.FromSeconds(10));
            
            // Dispose resources
            _producer?.Dispose();
            
            _logger.LogInformation("Kafka producer disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Kafka producer");
        }
    }
}
```

---

## RESUMO PATTERN ORDERSERVICE

### PATTERN IMPLEMENTATI:
1. **CQRS**: Separazione Commands/Queries in controller diversi
2. **Repository Pattern**: Service layer astrae data access
3. **Event-Driven**: Pubblicazione eventi Kafka per comunicazione asincrona
4. **Aggregate Pattern**: Order è aggregate root con OrderItems
5. **Producer Pattern**: OrderEventProducer per messaggi Kafka
6. **Dependency Injection**: IoC container per decoupling
7. **Async/Await**: Operazioni non-blocking per scalabilità
8. **Structured Logging**: ILogger per monitoring e troubleshooting

### FLUSSO CREAZIONE ORDINE COMPLETO:
```
1. User compila form → Shopping cart → Click "Conferma"
2. Frontend POST /api/commands/orders
3. GatewayBff proxy → OrderService
4. OrderCommandsController valida + crea Order
5. OrderService salva DB (transazione)
6. OrderEventProducer pubblica evento Kafka
7. Kafka broker memorizza su disk (durable)
8. Controller ritorna 201 Created
9. Frontend mostra conferma ordine
10. InventoryService consumer riceve evento (asincrono)
11. Inventory riduce stock automaticamente
12. NotificationService invia email conferma (futuro)
```

### PERCHÉ KAFKA E NON HTTP SYNC?
- ✅ **Non-blocking**: OrderService non aspetta InventoryService
- ✅ **Resilienza**: Se Inventory down, ordine comunque salvato
- ✅ **Scalabilità**: Più consumer per load distribution
- ✅ **Decoupling**: Servizi non conoscono dipendenze
- ✅ **Retry**: Kafka retry automatico su failure
- ✅ **Audit**: Tutti eventi loggati permanentemente

---

Continua con gli altri servizi (ProductService, InventoryService, GatewayBff, Frontend) nello stesso formato dettagliato...
