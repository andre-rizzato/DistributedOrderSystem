# Kafka Integration for Asynchronous Inventory Updates

> Updated to match the real code. The previous version of this document had `OrderCreatedEvent.OrderId`/`OrderItemEvent.ProductId` typed as `int`, `Acks.Leader` on the producer, and a single-listener Kafka config. Every discrepancy below was checked against the source directly. Note: as of 2026-09-24 all log messages across the codebase were translated from Italian to English — the log lines quoted in the [Observability](#observability) section below are current.
>
> **2026-09-26 update**: the publish-and-swallow-on-failure behavior and the "insufficient stock is committed silently, with no compensating action" behavior described below have both been replaced — OrderService now uses a transactional outbox to publish, and InventoryService now rolls back partial reservations and reports the outcome back to OrderService via a new `InventoryReservationResultEvent`, which OrderService uses to confirm or cancel the order. See `docs/SERVICE_COMMUNICATION_AND_CONSISTENCY.md` for the full design; this file is left in place for the parts that are still accurate (message shapes, dual-listener setup, testing guide) with the changed sections corrected below.

## Overview
Kafka-based event-driven architecture that asynchronously updates inventory when orders are created. This keeps OrderService and InventoryService loosely coupled while still reaching eventual consistency, and (as of 2026-09-26) closes the loop with a compensating-transaction saga instead of leaving a partially-failed reservation with no follow-up.

## Architecture

### Event Flow
```
1. User creates order via Frontend / GatewayBff / any HTTP client
2. Frontend → GatewayBff → OrderService (POST /api/commands/orders)
3. OrderService saves the order (status: Pending) AND an outbox row describing
   OrderCreatedEvent, in one DB transaction (OrderRepository.AddAsync)
4. OutboxDispatcherService (a BackgroundService, polling every 5s) picks up the
   outbox row and publishes OrderCreatedEvent to Kafka topic "order-created" —
   if Kafka is unreachable, the row just stays unprocessed and is retried on
   the next poll (up to 5 attempts) instead of the event being lost
5. InventoryService's OrderCreatedConsumer (a BackgroundService) consumes the event
6. InventoryService reserves (decrements) AvailableQuantity for each item, all-or-nothing
   per order — if any item can't be reserved, every item already reserved for that same
   order is rolled back (see the corrected "InventoryService - Consumer" section below)
7. InventoryService updates InventoryDb (PostgreSQL) and the Redis cache, then publishes
   exactly one InventoryReservationResultEvent per order to topic
   "inventory-reservation-result" (Success=true, or Success=false with a Reason)
8. OrderService's InventoryReservationResultConsumer applies the outcome to the order:
   Confirmed on success, Cancelled (a compensating action) on failure
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

Real per-message flow (as of 2026-09-26 — see the update note at the top of this file):
1. Consume message, log partition/offset.
2. Deserialize `OrderCreatedEvent`.
3. For each item in the event, call `AdjustInventoryQuantityAsync(Guid.Parse(item.ProductId), -item.Quantity)`:
   - Returns `true` → logs success, item added to a `reserved` list, loop continues.
   - Returns `false` (item not found, or insufficient stock — the reject condition is now `newQuantity < 0`, so reserving the exact last unit in stock is allowed, unlike before) → logs a **warning**, loop **stops** (`break`, not `continue`).
   - Throws (a technical failure, e.g. DB unreachable) → logs an **error**, then `throw;` propagates out of `ProcessMessageAsync`, same as before.
4. If step 3 stopped early on insufficient stock: every item in `reserved` (i.e. everything already decremented for this same order) is rolled back with a compensating `AdjustInventoryQuantityAsync(productId, +quantity)` call, then `InventoryReservationResultEvent { Success = false, Reason }` is published, and the method returns normally — the offset **is** committed (this is a handled business outcome, not a fault).
5. If step 3 completed without stopping: `InventoryReservationResultEvent { Success = true }` is published, offset committed as before.
6. If an exception propagated in step 3: the offset is **not** committed; the consumer loop catches it, waits 5 seconds, and the message is redelivered on the next poll — unchanged from before.

⚠️ **Correction to the previous version of this document**: insufficient stock is still a
normal, committed outcome — it is still not retried by Kafka redelivery — but it is no
longer silent. It now triggers a rollback of any partial reservation for the same order and
a published `InventoryReservationResultEvent(Success=false)`, which OrderService consumes to
cancel the order (see `docs/SERVICE_COMMUNICATION_AND_CONSISTENCY.md` §4b). If you're testing
"what happens when stock runs out," expect: a warning log, a moved-forward offset, any other
items in that same order rolled back, and the order itself transitioning to `Cancelled`
shortly after (not immediately — it depends on `InventoryReservationResultConsumer`'s poll of
that topic).

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
    "OrderCreatedTopic": "order-created",
    "InventoryReservationResultTopic": "inventory-reservation-result",
    "ConsumerGroupId": "order-service"
  }
}
```
`InventoryReservationResultTopic`/`ConsumerGroupId` are new as of 2026-09-26 — consumed by `InventoryReservationResultConsumer`.

### InventoryService — `appsettings.Development.json`
```json
{
  "Kafka": {
    "BootstrapServers": "localhost:29092",
    "OrderCreatedTopic": "order-created",
    "InventoryReservationResultTopic": "inventory-reservation-result",
    "ConsumerGroupId": "inventory-service"
  }
}
```
`InventoryReservationResultTopic` is new as of 2026-09-26 — published by `InventoryEventProducer`.

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
- **The publish side no longer loses events on a Kafka outage**: `OrderCreatedEvent` is now written to a transactional outbox table in the same DB transaction as the order (`OrderRepository.AddAsync`), and `OutboxDispatcherService` retries unprocessed rows (up to 5 attempts) instead of the previous inline publish-and-swallow. See `docs/SERVICE_COMMUNICATION_AND_CONSISTENCY.md` §4a.
- **Business failures now trigger a compensating action, not silence**: insufficient stock is still committed (not redelivered) on the InventoryService side, but it now rolls back any partial reservation for the order and reports `Success=false` back to OrderService, which cancels the order. See §4b of the same document.
- **Idempotent producer** (`EnableIdempotence = true`) prevents the *producer* from creating duplicate messages on retry — this says nothing about whether the *consumer's* effect (decrementing stock) is idempotent, and it currently is not guarded against reprocessing the same message twice after a technical-failure redelivery. This gap is unchanged by the outbox/saga work.
- **Order creation never blocks on Kafka** — still true; `CreateOrderAsync` only writes to Postgres (order + outbox row), Kafka is only touched by the separate `OutboxDispatcherService` background loop.

## Observability

All log messages across the codebase are in English as of 2026-09-24. Verified directly from source:

| Component | Real log message (verbatim) | Level |
|---|---|---|
| InventoryService, consumer constructor | `"Kafka consumer initialized for topic {Topic} with group {GroupId} at {BootstrapServers}"` | Info |
| InventoryService, consume loop start | `"Starting Kafka consumer for topic: {Topic}"` | Info |
| InventoryService, message received | `"Received message from partition {Partition} at offset {Offset}"` | Info |
| InventoryService, processing event | `"Processing OrderCreatedEvent for Order {OrderId} with {ItemCount} items"` | Info |
| InventoryService, stock reduced | `"Reduced inventory for Product {ProductId} by {Quantity} units (Order {OrderId})"` | Info |
| InventoryService, insufficient stock | `"Unable to reduce inventory for Product {ProductId} ... - insufficient inventory or product not found"` | Warning |
| InventoryService, all items done | `"Completed inventory updates for Order {OrderId}"` | Info |
| InventoryService, offset committed | `"Message processed and successfully committed at offset {Offset}"` | Info |
| InventoryService, consume/process error | `"Error consuming message: {Error}"` / `"Error processing message"` | Error |
| OrderService, producer init | `"Kafka producer initialized for topic {Topic} at {BootstrapServers}"` | Info |
| OrderService, publish success | `"Published OrderCreated event for Order {OrderId} partition {Partition} offset {Offset}"` | Info |
| OrderService, publish failure | `"Unable to publish OrderCreated event for Order {OrderId}: {Error}"` | Error |
| OrderService, repository persist | `"Order {OrderId} persisted with {ItemCount} items"` | Info |

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
- ✅ (as of 2026-09-26) The publish side is a transactional outbox — no event is lost to a Kafka outage at request time, only delayed.
- ✅ (as of 2026-09-26) Insufficient stock is no longer a silent dead end — it rolls back any partial reservation and cancels the order via a compensating saga step.
- ⚠️ No retry for the business condition "insufficient stock" — that message is still committed regardless (by design: it's a handled outcome, not a fault).
- ⚠️ Consumer-side idempotency is not explicitly guarded; only the producer is idempotent. Unchanged by this update.
- ⚠️ PaymentService is not part of this flow despite having Kafka settings scaffolded.

Treat this as a working development-mode integration with known gaps, not a production-hardened pipeline — there's still no dead-letter queue, no schema validation, no metrics/health endpoint for Kafka connectivity, and no authentication on the broker. See `docs/SERVICE_COMMUNICATION_AND_CONSISTENCY.md` for the full design rationale, the saga diagram, and what's still open.
