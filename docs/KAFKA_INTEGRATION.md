# Kafka Integration for Asynchronous Inventory Updates

> Updated to match the real code. The previous version of this document had `OrderCreatedEvent.OrderId`/`OrderItemEvent.ProductId` typed as `int`, `Acks.Leader` on the producer, a single-listener Kafka config, and log lines that don't exist anywhere in the codebase (they're actually logged in Italian — see the [Observability](#observability) section). Every discrepancy below was checked against the source directly.

## Overview
Kafka-based event-driven architecture that asynchronously updates inventory when orders are created. This keeps OrderService and InventoryService loosely coupled while still reaching eventual consistency.

## Architecture

### Event Flow
```
1. User creates order via Frontend / GatewayBff / any HTTP client
2. Frontend → GatewayBff → OrderService (POST /api/commands/orders)
3. OrderService saves the order to OrderDb (PostgreSQL)
4. OrderService publishes OrderCreatedEvent to Kafka topic "order-created"
   — if this publish fails, the order is still returned successfully to the caller
     (see OrderApplicationService.CreateOrderAsync — the Kafka call is wrapped in
     try/catch and only logged on failure, never rethrown to the controller)
5. InventoryService's OrderCreatedConsumer (a BackgroundService) consumes the event
6. InventoryService reduces AvailableQuantity for each item — but see the
   ⚠️ note under "InventoryService - Consumer" below: an insufficient-stock
   result does NOT block the Kafka commit, only a thrown exception does
7. InventoryService updates InventoryDb (PostgreSQL) and the Redis cache
```

## Components Implemented

### 1. Shared Messages (`src/Shared/Messages/OrderCreatedEvent.cs`)

Real current shape — **both ids are `string`, not `int`**:
```csharp
public record OrderCreatedEvent
{
    public string OrderId { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public List<OrderItemEvent> Items { get; init; } = new();
}

public record OrderItemEvent
{
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
}
```
- **Why strings**: `Order.Id` in OrderService is an `int` (EF identity) but `OrderItem.ProductId` is a `Guid` (matching ProductService/InventoryService). Rather than carry two different native types across the wire, `OrderApplicationService.CreateOrderAsync` converts both to `string` (`created.Id.ToString()`, `i.ProductId.ToString()`) before building the event. `InventoryService`'s consumer parses `ProductId` back with `Guid.Parse(item.ProductId)`.
- **Purpose**: shared data contract between OrderService (producer) and InventoryService (consumer).

### 2. OrderService - Producer

#### Configuration (`src/OrderService/Infrastructure/Configuration/KafkaSettings.cs`)
```csharp
public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string OrderCreatedTopic { get; set; } = "order-created";
}
```
This typed class exists but `OrderEventProducer` doesn't actually consume it — it reads `configuration["Kafka:BootstrapServers"]` / `configuration["Kafka:OrderCreatedTopic"]` directly from `IConfiguration`, with the same string defaults hardcoded again inline. Harmless (both read the same `appsettings` section) but worth knowing if you go looking for where `KafkaSettings` is actually bound with `.Configure<KafkaSettings>()` — it isn't, in OrderService.

#### Messaging (`src/OrderService/Infrastructure/Messaging/OrderEventProducer.cs`)
Real producer configuration:
```csharp
var config = new ProducerConfig
{
    BootstrapServers = bootstrapServers,
    Acks = Acks.All,              // not Acks.Leader
    EnableIdempotence = true,
    MaxInFlight = 5,
    MessageSendMaxRetries = 3,
    LingerMs = 10
};
```
- Publishes with `Key = $"order-{orderEvent.OrderId}"`, JSON-serialized value, explicit UTC timestamp.
- On `ProduceException`, logs the error and **rethrows** — but the caller (`OrderApplicationService.CreateOrderAsync`) catches that rethrow, logs again, and swallows it. The order creation request itself never fails because of this.

#### Controller (`src/OrderService/Controllers/OrdersController.cs`)
`OrderCommandsController` (in the same file as `OrdersController`) handles `POST /api/commands/orders`. It delegates entirely to `IOrderApplicationService.CreateOrderAsync`, which does the persistence-then-publish orchestration described above. There's no separate "enhanced" controller layer beyond this — business logic lives in `OrderApplicationService` and the `Order` aggregate, not in the controller.

### 3. InventoryService - Consumer

#### Configuration (`src/InventoryService/Configuration/KafkaSettings.cs`)
```csharp
public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string OrderCreatedTopic { get; set; } = "order-created";
    public string ConsumerGroupId { get; set; } = "inventory-service";
}
```
Unlike OrderService, this one **is** actually bound via `builder.Services.Configure<KafkaSettings>(...)` in `Program.cs` — but `OrderCreatedConsumer` itself doesn't inject `IOptions<KafkaSettings>` either; it reads `configuration["Kafka:..."]` directly in its constructor, same pattern as the producer side.

#### Messaging (`src/InventoryService/Messaging/OrderCreatedConsumer.cs`)
`BackgroundService`, manual offset management:
```csharp
var config = new ConsumerConfig
{
    BootstrapServers = bootstrapServers,
    GroupId = groupId,
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = false,
    EnableAutoOffsetStore = false
};
```

Real per-message flow:
1. Consume message, log partition/offset.
2. Deserialize `OrderCreatedEvent`.
3. For each item in the event, call `AdjustInventoryQuantityAsync(Guid.Parse(item.ProductId), -item.Quantity)`:
   - Returns `true` → logs success, loop continues.
   - Returns `false` (item not found, or the adjustment would leave stock `<= 0` — see `INVENTORY_SERVICE_DOCUMENTATION.md` for the exact `<=` vs `<` detail) → logs a **warning**, loop continues.
   - Throws → logs an **error**, then `throw;` propagates out of `ProcessMessageAsync`.
4. If the foreach completes without an exception (even if some items logged step-3 warnings), the offset is committed and stored — the message is considered fully processed.
5. If an exception propagated, the offset is **not** committed; the consumer loop catches it, waits 5 seconds, and the message is redelivered on the next poll.

⚠️ **The most important correction in this document**: an insufficient-stock result is a normal, committed outcome — it is not retried. Only a technical failure (e.g. the database being unreachable) blocks the commit and forces redelivery. If you're testing "what happens when stock runs out," expect a warning log and a moved-forward offset, not a stuck consumer.

### 4. PaymentService — not part of this flow (yet)
`appsettings.Development.json` for PaymentService already has Kafka settings scaffolded:
```json
"Kafka": {
  "BootstrapServers": "localhost:29092",
  "PaymentProcessedTopic": "payment-processed",
  "OrderCreatedTopic": "order-created",
  "ConsumerGroupId": "payment-service"
}
```
But `PaymentService/Program.cs` is still the unmodified ASP.NET template — there is no consumer, no producer, and no controller in this service at all. If you're looking for where PaymentService reacts to `order-created` or publishes `payment-processed`, it doesn't exist yet; this config is scaffolding for future work only.

## Configuration

### OrderService — `appsettings.Development.json`
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:29092",
    "OrderCreatedTopic": "order-created"
  }
}
```

### InventoryService — `appsettings.Development.json`
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:29092",
    "OrderCreatedTopic": "order-created",
    "ConsumerGroupId": "inventory-service"
  }
}
```

Both use `29092`, not `9092` — see the dual-listener explanation below. This is a value added this session; previously both files pointed at `9092`, which only works when the service itself is also running inside the Docker network.

## Docker Infrastructure

### Kafka Setup (real `docker/docker-compose.yml`, KRaft mode, dual listener)
```yaml
kafka:
  image: confluentinc/cp-kafka:7.4.0
  container_name: dos_kafka
  hostname: kafka
  ports:
    - "9092:9092"
    - "29092:29092"
  environment:
    KAFKA_NODE_ID: 1
    KAFKA_PROCESS_ROLES: broker,controller
    KAFKA_LISTENERS: PLAINTEXT://0.0.0.0:9092,PLAINTEXT_HOST://0.0.0.0:29092,CONTROLLER://0.0.0.0:9093
    KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:9092,PLAINTEXT_HOST://localhost:29092
    KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
    KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT
    KAFKA_CONTROLLER_QUORUM_VOTERS: 1@kafka:9093
    KAFKA_AUTO_CREATE_TOPICS_ENABLE: "true"
    CLUSTER_ID: MkU3OEVBNTcwNTJENDM2Qk
```

⚠️ **Two listeners, added this session, and why**: `PLAINTEXT` on `9092` is advertised as `kafka:9092` — reachable only from other containers on `dos_network` (e.g. `order-service`, `inventory-service` when they run via Docker Compose). `PLAINTEXT_HOST` on `29092` is advertised as `localhost:29092` — reachable from the host machine, i.e. any service you launch with `dotnet run` or via the `AppHost`/.NET Aspire orchestrator directly against the dockerized broker. Match your bootstrap-servers value to how the connecting process is actually running:

| Where the service runs | BootstrapServers to use |
|---|---|
| Inside Docker Compose (`order-service`, `inventory-service` containers) | `kafka:9092` (set via `Kafka__BootstrapServers=kafka:9092` env var in `docker-compose.yml`) |
| On the host via `dotnet run` or the AppHost | `localhost:29092` (set in each service's `appsettings.Development.json`) |
| `docker exec` into the `dos_kafka` container itself (CLI tools below) | `localhost:9092` — you're inside the container's own network namespace, where the `PLAINTEXT` listener is bound locally |

`kafka-ui` (port `8080`, http://localhost:8080) is also part of this compose file and is the easiest way to browse topics, partitions, and consumer group lag without the CLI commands below.

## Reliability & Guarantees — precisely, not generically

- **At-least-once delivery for technical failures**: manual commit means a crash or exception mid-processing causes redelivery.
- **No delivery guarantee beyond "committed" for business failures**: as detailed above, insufficient stock is logged and committed, not retried.
- **Idempotent producer** (`EnableIdempotence = true`) prevents the *producer* from creating duplicate messages on retry — this says nothing about whether the *consumer's* effect (decrementing stock) is idempotent, and it currently is not guarded against reprocessing the same message twice after a technical-failure redelivery.
- **Order creation never blocks on Kafka** — verified in `OrderApplicationService.CreateOrderAsync`.

## Observability

⚠️ **The actual log messages are mostly in Italian, not English.** If you're grepping logs or writing alerting rules against the English phrases historically documented here, they won't match anything. Verified directly from source:

| Component | Real log message (verbatim) | Level |
|---|---|---|
| InventoryService, consumer constructor | `"Kafka consumer initialized for topic {Topic} with group {GroupId} at {BootstrapServers}"` | Info — **this one is in English** |
| InventoryService, consume loop start | `"Avvio consumer Kafka per topic: {Topic}"` | Info |
| InventoryService, message received | `"Ricevuto messaggio dalla partizione {Partition} all'offset {Offset}"` | Info |
| InventoryService, processing event | `"Elaborazione OrderCreatedEvent per Ordine {OrderId} con {ItemCount} articoli"` | Info |
| InventoryService, stock reduced | `"Ridotto inventario per Prodotto {ProductId} di {Quantity} unità (Ordine {OrderId})"` | Info |
| InventoryService, insufficient stock | `"Impossibile ridurre inventario per Prodotto {ProductId} ... - inventario insufficiente o prodotto non trovato"` | Warning |
| InventoryService, all items done | `"Completati aggiornamenti inventario per Ordine {OrderId}"` | Info |
| InventoryService, offset committed | `"Messaggio elaborato e confermato con successo all'offset {Offset}"` | Info |
| InventoryService, consume/process error | `"Errore durante il consumo del messaggio: {Error}"` / `"Errore durante l'elaborazione del messaggio"` | Error |
| OrderService, producer init | `"Producer Kafka inizializzato per topic {Topic} su {BootstrapServers}"` | Info |
| OrderService, publish success | `"Pubblicato evento OrderCreated per Ordine {OrderId} partizione {Partition} offset {Offset}"` | Info |
| OrderService, publish failure | `"Impossibile pubblicare evento OrderCreated per Ordine {OrderId}: {Error}"` | Error |
| OrderService (Application layer), publish success/failure | `"Pubblicato evento OrderCreated per Ordine {OrderId}"` / `"Impossibile pubblicare evento OrderCreated per Ordine {OrderId}"` | Info / Error |

## Testing the Integration

See `KAFKA_TESTING_GUIDE.md` for the full step-by-step walkthrough with corrected ports, ids, and expected (Italian) log output.

## Security Considerations

### Current Setup (Development)
- No authentication, plaintext, `KAFKA_AUTO_CREATE_TOPICS_ENABLE: "true"` — appropriate for local dev only.

### Production Recommendations (not implemented — aspirational)
SASL/SCRAM or mTLS, TLS in transit, ACLs per topic, network isolation, security event logging. None of this exists in the current `docker-compose.yml`.

## Summary

The Kafka integration provides asynchronous inventory updates decoupled from order creation, with the caveats documented above:
- ✅ Order creation never blocks on Kafka being reachable.
- ✅ At-least-once delivery for technical failures via manual commit.
- ⚠️ No retry for the business condition "insufficient stock" — that message is committed regardless.
- ⚠️ Consumer-side idempotency is not explicitly guarded; only the producer is idempotent.
- ⚠️ PaymentService is not part of this flow despite having Kafka settings scaffolded.

Treat this as a working development-mode integration with known gaps, not a production-hardened pipeline — there's no dead-letter queue, no schema validation, no metrics/health endpoint for Kafka connectivity, and no authentication on the broker.
