# Service Communication, Roles, and Consistency

This document answers three questions that came up while wiring the chat agent and the
customer site through GatewayBff:

1. When one service needs something from another, does it call it directly, or go through GatewayBff?
2. Where does cross-service data validation live?
3. How do we keep data consistent across services that each own a separate database, and how do we roll back when something downstream fails?

It also documents the outbox + saga implementation added to answer (3) concretely, since
"the BFF validates it" turned out not to be a real answer to that question.

---

## 1. Service roles and responsibilities

| Service | Owns | Responsibility | Should NOT do |
|---|---|---|---|
| **GatewayBff** | Nothing (stateless) | Single public entry point for client-facing traffic (Angular SPA, CustomerWebsite, ChatbotService's agent calls). Aggregates/orchestrates downstream calls for a single client request (e.g. `CreateOrderCommand` checks product + stock before calling OrderService). Owns request-time, read-oriented cross-service checks. | Own durable state; be treated as a mandatory hop for service-to-service traffic that isn't serving a client request; own multi-step business transaction consistency (that's a saga's job, not a gateway's). |
| **OrderService** | Orders (DDD aggregate) | Owns the `Order` aggregate and its state machine (`Pending → Confirmed/Cancelled`, `Confirmed → Shipped`, `Shipped → Delivered`). Validates every invariant about the data it owns (positive quantity, non-empty items, valid status transitions) regardless of who's calling it. Publishes `OrderCreatedEvent` via a transactional outbox. Consumes `InventoryReservationResultEvent` to confirm or compensate. | Validate whether a `ProductId` exists or is active in ProductService — that's a stock/catalog concern owned by ProductService and checked by GatewayBff at request time, not re-checked here. Call InventoryService or ProductService synchronously. |
| **ProductService** | Product catalog | Product existence, pricing, active/inactive status. | Know anything about orders or inventory levels. |
| **InventoryService** | Stock levels | Owns `AvailableQuantity` per product. Reacts to `OrderCreatedEvent` by reserving (decrementing) stock, all-or-nothing per order, with compensation on partial failure. Reports the outcome back via `InventoryReservationResultEvent`. | Call OrderService or ProductService. Everything it needs (ProductId, Quantity) already travels in the event. |
| **CustomerService** | Cart/wishlist (Redis, ephemeral) | Session-scoped cart and wishlist state. | Own anything durable; call other services. |
| **UserService** | Identity (ASP.NET Identity + JWT) | Auth, registration, password reset. Calls NotificationService directly (see [§2](#2-when-to-call-directly-vs-through-gatewaybff) below) to send transactional email. | — |
| **NotificationService** | Notification delivery (SMS/Email/Push) + Hangfire jobs | Receiving `send-direct`/`send-template` requests and dispatching them. | — |
| **ChatbotService** | NLP/intent inference | Chat inference. When the chat needs real order data, it calls **GatewayBff**, never OrderService directly (see `ServiceIntegrationService.CancelOrderAsync`). | Call downstream business services directly. |
| **AgentService** (Python/LangGraph) | Order-support agent | Same rule as ChatbotService: `gateway_client.py` only ever calls GatewayBff. | Call downstream business services directly. |
| **PaymentService** | (stub, not implemented yet) | — | — |

---

## 2. When to call directly vs. through GatewayBff

**Client-facing traffic (browser, chat widget, the agent) always goes through GatewayBff.**
That's the rule enforced by the CustomerWebsite/ChatbotService/AgentService work already
done — no frontend or chat-adjacent service should hold a ProductService/OrderService/
InventoryService URL in its config anymore.

**Service-to-service traffic is a separate question, and "always through the BFF" is the
wrong rule for it.** A BFF (Backend-For-Frontend) exists to aggregate and adapt calls *for
a specific client*. Routing internal service-to-service calls through it too would:
- make GatewayBff a single point of failure for traffic that has nothing to do with any client,
- add a network hop and a second service's uptime to every internal call for no benefit,
- turn the BFF into a de facto service mesh / internal gateway, which is not its job.

Instead, pick per use case:

- **Synchronous direct call** — when the caller needs the answer immediately to proceed, and only for a shallow, one-hop dependency. Example already in this codebase: `UserService.CommunicationService` calls NotificationService's `api/notification/send-direct` directly. That's fine — it's one hop, NotificationService is a well-defined leaf dependency, and there's no multi-step transaction to keep consistent.
- **Asynchronous event** — when the caller shouldn't block on the callee, and eventual consistency is acceptable. Example: `OrderService → InventoryService` via Kafka's `order-created` topic. This is the right shape for "order placed, now go do something about stock" — it decouples the two services' uptime and lets InventoryService (or a future PaymentService, ShippingService, etc.) subscribe without OrderService knowing they exist.
- **Through GatewayBff** — only when the call is answering a specific client request and needs data/validation aggregated from more than one downstream service before responding (GatewayBff's `CreateOrderCommand` checking ProductService + InventoryService before calling OrderService is exactly this).

**What was actually wrong before this change** wasn't "InventoryService talks to OrderService without going through the BFF" (it never did — it uses Kafka, correctly). It was that a *client-facing* service (CustomerWebsite) was calling a backend service (ProductService) directly, which is now fixed (see `docs/BFF_ARCHITECTURE.md` and the `CustomerWebsite/Services/ProductService.cs` rewrite).

---

## 3. Where cross-service validation lived, and why that wasn't enough

Before this change, the only place that validated "does this product exist / is it active /
is there enough stock" was `GatewayBff.CreateOrderCommandHandler`, at the moment a client
asked to place an order. Two problems with that being the *only* place:

1. **Nothing stops a caller from reaching OrderService directly** in this dev setup (no network-level restriction — see [§5](#5-recommended-follow-up-network-isolation)). If that happened, an order could be created for a product that doesn't exist or is inactive, because OrderService itself never checks — it trusts whatever `CreateOrderCommand` already validated.
2. **The check was a classic check-then-act race.** GatewayBff asks InventoryService "is there stock?", gets "yes", and *then* calls OrderService to create the order. Between the check and the moment stock actually gets decremented (asynchronously, via Kafka, after the HTTP response already went back to the client), a second concurrent order for the same product could pass the same check. Oversold stock, discovered only after both orders already look "successful" to their callers.

Fix for (1): OrderService's own `Domain` layer already enforces its own invariants
independently of the caller — `OrderItem` rejects `ProductId == Guid.Empty`, `Quantity <= 0`,
and a missing `UnitPrice`; `Money` rejects negative amounts; `Order.Create` rejects an empty
item list. That's the correct scope for OrderService's defense-in-depth: **validate the
shape and rules of the data you own; don't re-implement another service's existence checks**
just because a caller might bypass the gateway. The actual fix for "a caller might bypass
the gateway" is a deployment/network concern, not a code-duplication one — see §5.

Fix for (2) is the real gap, and it's what the rest of this document (and this change) is about: the check-then-act race can't be closed by validating harder at request time. It needs the *outcome* of the reservation, discovered asynchronously, to be able to compensate the order that lost the race. That's a saga.

---

## 4. What was implemented: reliable delivery (outbox) + compensating rollback (saga)

### 4a. Transactional outbox — OrderService no longer loses events on Kafka downtime

**Before:** `OrderApplicationService.CreateOrderAsync` saved the order, then tried to publish
`OrderCreatedEvent` inline, wrapped in a try/catch that only logged on failure. If Kafka was
unreachable at that exact moment, the event was gone forever — the order existed, but
inventory would never be told about it, silently.

**Now:**
- `OrderRepository.AddAsync` writes the `Order` **and** an `OutboxMessage` row (`Type` +
  JSON `Payload`) in one database transaction (`BeginTransactionAsync` wrapping two
  `SaveChangesAsync` calls — the first assigns the order's identity column value, which the
  event payload needs, before the second call persists the outbox row alongside it).
- `OutboxDispatcherService` (a `BackgroundService`, polling every 5s) reads unprocessed
  outbox rows and publishes them via the existing `IOrderEventProducer`. A row that fails to
  publish just stays unprocessed (`ProcessedOnUtc == null`) and is retried on the next poll,
  up to 5 attempts, with the failure reason recorded on the row (`Error`, `Attempts`).

This means "the order was persisted" and "the event will eventually reach Kafka" are now the
same guarantee — a Kafka outage delays delivery, it no longer loses it.

Files: `OrderService/Infrastructure/Outbox/OutboxMessage.cs`,
`OrderService/Infrastructure/Repositories/OrderRepository.cs`,
`OrderService/Infrastructure/Messaging/OutboxDispatcherService.cs`,
migration `20260926020009_AddOutboxMessages`.

> ⚠️ **Known limitation**: `OrderService/Program.cs` calls `Database.EnsureCreated()` at
> startup, not `Database.Migrate()`. `EnsureCreated()` builds the schema from the current
> model **only when the database doesn't exist yet** — it does not apply migrations to an
> already-existing database. If you already have an `OrderDb_Dev` database from before this
> change, either drop it (`dotnet ef database drop --project src/OrderService`, then let
> `EnsureCreated()` rebuild it) or apply the migration by hand
> (`dotnet ef database update --project src/OrderService`) — a fresh database will pick up
> the new `OutboxMessages` table automatically either way.

### 4b. Saga with compensating rollback — InventoryService ↔ OrderService

This is the choreography-based saga that closes the check-then-act race from §3:

```text
OrderService                         InventoryService
─────────────                        ─────────────────
CreateOrder → Order (Pending)
     │
     ├─ outbox: OrderCreatedEvent ───────────────▶ OrderCreatedConsumer
     │                                                   │
     │                                        for each item: try to
     │                                        reserve stock (decrement)
     │                                                   │
     │                                     ┌─────────────┴─────────────┐
     │                                 all succeeded              one failed
     │                                     │                           │
     │                                     │                roll back every item
     │                                     │                already reserved for
     │                                     │                    this order
     │                                     ▼                           ▼
     │◀────── InventoryReservationResultEvent(Success=true) ───────────┤
     │◀────── InventoryReservationResultEvent(Success=false, Reason) ──┘
     │
InventoryReservationResultConsumer
     │
     ├─ Success  → UpdateOrderStatusAsync(orderId, "Confirmed")
     └─ !Success → CancelOrderAsync(orderId)   ← compensating action
```

- **InventoryService (`OrderCreatedConsumer`)**: reserves stock for each line item in order;
  the first failure stops the loop and triggers compensation — every line item already
  reserved *for that same order* is rolled back (`AdjustInventoryQuantityAsync` with the
  positive quantity), so a partially-failed order never leaves partial stock decremented with
  no way back. Either way (success or failure), it publishes exactly one
  `InventoryReservationResultEvent` per order to the new `inventory-reservation-result` topic.
  A **technical** failure (DB unreachable, etc.) still throws and blocks the Kafka commit for
  redelivery, same as before — only the **business** outcome ("insufficient stock") is
  handled via compensation instead of an exception.
- **OrderService (`InventoryReservationResultConsumer`)**: a new consumer that applies the
  result to the order's own state machine — `Confirmed` on success, `Cancelled` on failure.
  If the order already moved to a state that doesn't allow that transition (e.g. a customer
  manually canceled it before this event arrived), `OrderDomainException` is caught and
  logged — not a transient fault, so it doesn't block the consumer.
- **Fixed while implementing this**: `InventoryWorkerService.AdjustInventoryQuantityAsync`
  rejected `newQuantity <= 0`, which incorrectly treated "sell the exact last unit in stock"
  (`newQuantity == 0`) as a failure. Now rejects only `newQuantity < 0`.

Files: `Shared/Messages/InventoryReservationResultEvent.cs`,
`InventoryService/Messaging/{IInventoryEventProducer,InventoryEventProducer}.cs`,
`InventoryService/Messaging/OrderCreatedConsumer.cs` (rewritten `ProcessMessageAsync`),
`OrderService/Infrastructure/Messaging/InventoryReservationResultConsumer.cs`.

New Kafka topic: `inventory-reservation-result` (consumer group `order-service` on the
OrderService side — new `Kafka:ConsumerGroupId` setting, mirroring the one InventoryService
already had).

### What this does and doesn't solve

- ✅ Closes the check-then-act race's *consequence*: an oversold order is now automatically
  detected and canceled, instead of silently sitting there as "Pending" forever with stock
  quietly not reserved.
- ✅ An order created while Kafka (or InventoryService) is down is no longer lost — it just
  stays `Pending` until the outbox dispatcher successfully delivers `OrderCreatedEvent` and
  InventoryService processes it.
- ⚠️ **Does not eliminate the race itself** — two concurrent orders for the last unit of
  stock can still both reach InventoryService before either finishes; only one will
  successfully reserve, the other will correctly self-cancel via this saga. That's the
  intended behavior for eventual consistency (accept the race, compensate the loser),
  not a bug — a true "no oversell, ever" guarantee would require InventoryService to hold a
  synchronous reservation lock at order-creation time, which reintroduces the tight coupling
  this event-driven design deliberately avoids.
- ⚠️ **Consumer-side idempotency is still not guarded.** If `OrderCreatedConsumer` crashes
  after decrementing stock but before committing its Kafka offset, redelivery will decrement
  again. This was a pre-existing gap (see `docs/KAFKA_INTEGRATION.md`) and is unchanged by
  this work — a real fix needs an idempotency key (e.g. a `ProcessedOrderIds` table) checked
  before applying the adjustment.
- ⚠️ **No dead-letter queue.** A message that hits `MaxAttempts` in the outbox is simply left
  unprocessed with `Attempts == 5`; nothing pages anyone or moves it aside for inspection.

---

## 5. Recommended follow-up: network isolation

The one gap this change deliberately does **not** fix in code: nothing today stops a client
from calling `OrderService`, `ProductService`, or `InventoryService` directly instead of
going through GatewayBff — they're all just separate ports on `localhost` (or separate
containers with published ports in `docker-compose.yml`). Application-layer fixes (each
service re-validating everything upstream already validated) fight this at the wrong layer
and reintroduce the tight coupling this architecture is trying to avoid.

The correct fix is at the network/deployment layer: only GatewayBff (and ChatbotService's
static widget asset, per its own documented CORS exception) should be reachable from outside
the deployment; every other service should live on an internal-only network, reachable from
GatewayBff and from Kafka, but not from the public internet or the browser. This wasn't
implemented here because the project's everyday dev workflow (`dotnet run --project src/X`
per service, per `CLAUDE.md`) depends on every service's port being reachable from the host —
removing published ports in `docker-compose.yml` would only take effect for the
all-in-docker workflow and would silently break the normal one. Treat this as a production
deployment concern (e.g. a Docker network with only the gateway's port published, or
Kubernetes `NetworkPolicy` restricting ingress to non-gateway services to intra-cluster
traffic) rather than something to half-implement in the current compose file.
