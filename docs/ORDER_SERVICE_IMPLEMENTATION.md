# OrderService Implementation Summary

> Rewritten to match the current code. The previous version of this document described a flat `Models/Order.cs` + `Data/OrderContext.cs` + `Services/IOrderService`/`OrderWorkerService` layout on SQL Server with `int` product ids. **OrderService has since been refactored into a full DDD structure** (`Domain/Aggregates`, `Domain/ValueObjects`, `Domain/Events`, `Domain/SeedWork`, `Application/Services`, `Infrastructure/{Data,Repositories,Messaging,Configuration}`) on PostgreSQL with `Guid` product ids. This is the single biggest change — everything below reflects the DDD version. See `../COMPREHENSIVE_SOLUTION_ARCHITECTURE_EN.md` and `ARCHITECTURE_OVERVIEW.md` for the cross-service picture; this file stays focused on OrderService's own implementation.

## Overview
OrderService is the solution's DDD reference implementation: business rules (status transitions, money invariants, order creation) live in the Domain layer, not in controllers or a generic "WorkerService".

## Backend Implementation

### 1. OrderService (Port 5003, PostgreSQL `OrderDb_Dev`)

#### Domain (`src/OrderService/Domain/`)
- **`Aggregates/Order.cs`**: aggregate root. No public constructor — `Order.Create(items)` is the only way to build one, and it throws `OrderDomainException` if `items` is empty. Properties (`CreatedAt`, `Status`, `Total`, `Items`) all have `private set`; `Items` is exposed as `IReadOnlyList<OrderItem>` backed by a private `List<OrderItem>`.
- **`Aggregates/OrderItem.cs`**: child entity. Constructor is `internal` (only `Order` can create one) and validates `ProductId != Guid.Empty`, `Quantity > 0`, and a non-null `UnitPrice` — invalid input throws immediately rather than producing a half-valid row.
- **`ValueObjects/Money.cs`**: wraps a `decimal Amount`; the constructor throws `OrderDomainException` if `amount < 0`. `Add`/`Multiply` return new instances (immutable).
- **`ValueObjects/OrderStatus.cs`**: enforces a fixed transition table — `Pending → {Confirmed, Cancelled}`, `Confirmed → {Shipped, Cancelled}`, `Shipped → Delivered`; `Delivered`/`Cancelled` are terminal. `TransitionTo(newStatus)` throws on any transition not in that table.
- **`Events/`**: `OrderCreatedDomainEvent`, `OrderStatusChangedDomainEvent` — collected on the aggregate via `AddDomainEvent`, not yet dispatched to any in-process handler (the Kafka integration event published on order creation is a separate concept, built manually in the Application layer — see below).
- **`Interfaces/IOrderRepository.cs`**: the only thing Infrastructure implements for persistence access.
- **`SeedWork/`**: `AggregateRoot`, `Entity`, `ValueObject`, `IDomainEvent` — the shared DDD primitives.

#### Application (`src/OrderService/Application/Services/`)
- **`OrderApplicationService`** (`IOrderApplicationService`): orchestrates the domain and its side effects. `CreateOrderAsync` calls `Order.Create(items)`, persists via the repository, builds an `OrderCreatedEvent` (from the `Shared` project) from the persisted order, and calls `IOrderEventProducer.PublishOrderCreatedAsync`. **If that publish throws, the exception is caught and logged — the already-persisted order is still returned to the caller.** `UpdateOrderStatusAsync` loads the order, calls `order.ChangeStatus(newStatus)` (which raises `OrderDomainException` through `OrderStatus.TransitionTo` on an invalid transition), then persists.

#### Infrastructure (`src/OrderService/Infrastructure/`)
- **`Data/OrderContext.cs`**: EF Core mapping for the DDD aggregate, notably:
  - `Order.Status` (a value object) is mapped via `HasConversion(v => v.Value, v => OrderStatus.From(v))` to a `varchar(50)` column.
  - `Order.Total` and `OrderItem.UnitPrice` (both `Money`) are mapped via `HasConversion(v => v.Amount, v => new Money(v))` with `HasPrecision(18, 2)`.
  - `entity.Ignore(e => e.DomainEvents)` — the collected domain events are never persisted.
  - `Order.Items` navigation is configured with `SetPropertyAccessMode(PropertyAccessMode.Field)` so EF Core populates the private `_items` backing field directly, bypassing the constructor-enforced invariants on load (expected for DDD aggregates — EF needs a way in that application code doesn't).
  - `OnDelete(DeleteBehavior.Cascade)` on the `Order → OrderItem` relationship.
- **`Repositories/OrderRepository.cs`**: implements `IOrderRepository` against `OrderContext`.
- **`Messaging/OrderEventProducer.cs`**: real Kafka producer. `ProducerConfig`: `Acks = Acks.All`, `EnableIdempotence = true`, `MaxInFlight = 5`, `MessageSendMaxRetries = 3`, `LingerMs = 10`. Publishes to `Kafka:OrderCreatedTopic` (`order-created`) with key `order-{orderId}`. A `ProduceException` is logged and rethrown to the Application layer, which is what `OrderApplicationService` catches.
- **`Messaging/MockOrderEventProducer.cs`**: implements the same interface, just logs "[MOCK] would publish" instead of calling Kafka. ⚠️ **This class exists but is never registered** — `Program.cs` always wires the real `OrderEventProducer`, in every environment, so there is currently no way to run OrderService without a reachable Kafka broker without editing `Program.cs` yourself.
- **`Configuration/KafkaSettings.cs`**: `BootstrapServers` (default `localhost:9092` in code, overridden to `localhost:29092` in `appsettings.Development.json` — Kafka's host-facing listener; containers use `kafka:9092` via the `Kafka__BootstrapServers` env var in `docker-compose.yml`), `OrderCreatedTopic` (`order-created`).

#### Controllers (`src/OrderService/Controllers/OrdersController.cs` — both controllers below live in this one file)
- **`OrdersController`** (read side, `/api/orders`):
  - `GET /api/orders` → all orders
  - `GET /api/orders/{id:int}` → single order, `404` if not found
  - Both thin: map the DDD `Order` to a local `OrderDto`/`OrderItemResponseDto` record, no business logic.
- **`OrderCommandsController`** (write side, `/api/commands`):
  - `POST /api/commands/orders` — body `{ items: [{ productId: Guid, quantity, unitPrice }] }`; `400` if `Items` is empty or on `OrderDomainException` (e.g., an item with `quantity <= 0`); `201 Created` pointing at `GetOrder` on success.
  - `PUT /api/commands/orders/{id:int}/status` — body `{ status: string }`; `404` if the order doesn't exist, `400` on an invalid transition (`OrderDomainException` from `OrderStatus.TransitionTo`), `204 No Content` on success.

⚠️ Note the routing: OrderService itself exposes `/api/commands/orders`, distinct from **GatewayBff's own** `/api/commands/orders` (in `GatewayBff/Controllers/CommandsController.cs`). The two are not the same endpoint — the BFF's command controller calls this one over HTTP via the named `"OrderService"` client.

#### Configuration
- **`appsettings.Development.json`**: `ConnectionStrings:OrderDb` = `Host=localhost;Port=5432;Database=OrderDb_Dev;Username=postgres;Password=YourStrong_Password123;` (PostgreSQL, not SQL Server); `Kafka:BootstrapServers` = `localhost:29092`.
- **`OrderService.csproj`**: `Npgsql.EntityFrameworkCore.PostgreSQL` (not `Microsoft.EntityFrameworkCore.SqlServer`), `Confluent.Kafka`, `Microsoft.EntityFrameworkCore.Tools`, `Scalar.AspNetCore`, `Swashbuckle.AspNetCore`.
- **`Program.cs`**: registers `OrderContext` (Npgsql), `IOrderRepository → OrderRepository`, `IOrderEventProducer → OrderEventProducer` (singleton — see the Mock note above), `IOrderApplicationService → OrderApplicationService`, permissive CORS, `context.Database.EnsureCreated()` at startup (no EF Core migrations for this service).

### 2. GatewayBff (order-related pieces — unchanged in shape since the original implementation)

#### Commands (`src/GatewayBff/Commands/CreateOrderCommand.cs`)
- Fetches product prices from ProductService, checks inventory via InventoryService, forwards a `CreateOrderRequest` to OrderService's `/api/commands/orders`.

#### Queries (`src/GatewayBff/Queries/`)
- `GetAllOrdersQuery` / `GetOrderByIdQuery` — call OrderService's `/api/orders` and `/api/orders/{id}` via the named `"OrderService"` HTTP client (configured from `ServiceUrls:OrderService` in `appsettings.Development.json`, `http://localhost:5003`).

#### Controllers (`src/GatewayBff/Controllers/QueriesController.cs`)
- `GET /api/queries/orders`, `GET /api/queries/orders/{id:int}`.

## Frontend Implementation

This part of the original document still matches the Angular SPA's structure and hasn't needed correction:

### Models (`frontend/src/app/models/order.ts`)
`OrderItem` (ProductId, Quantity, UnitPrice), `CreateOrderRequest`, `CreateOrderResponse`, `OrderItemDetail`, `Order`.

### Services (`frontend/src/app/services/order.ts`)
`getAllOrders()`, `getOrderById(id)`, `createOrder(request)` — base URL `http://localhost:5189/api` (GatewayBff).

### Components
- **Orders list** (`components/orders/`): status badges, order items summary, Italian locale formatting.
- **Create Order** (`components/create-order/`): product grid + cart, quantity controls, real-time totals, low-stock warnings.

### Routing
`/orders` → `OrdersComponent`, `/create-order` → `CreateOrderComponent`.

## Architecture Flow

### Create Order Flow
1. Frontend submits `CreateOrderRequest` to GatewayBff `/api/commands/orders`.
2. `CreateOrderCommandHandler` fetches prices from ProductService, checks inventory via InventoryService, forwards to OrderService.
3. OrderService's `OrderCommandsController.CreateOrder` calls `IOrderApplicationService.CreateOrderAsync`, which calls `Order.Create(items)` (Domain), persists via `IOrderRepository`, and publishes `OrderCreatedEvent` to Kafka — catching and logging any publish failure without failing the request.
4. Response: `CreateOrderResponse(OrderId, Status, Total)`.
5. Asynchronously, InventoryService's `OrderCreatedConsumer` consumes the event and decrements stock (see `INVENTORY_SERVICE_DOCUMENTATION.md` for the exact commit/retry semantics — insufficient stock at that point does **not** roll back or block the order; the order this flow already returned stays as-is).

### View Orders Flow
Frontend → GatewayBff `GetAllOrdersQueryHandler`/`GetOrderByIdQueryHandler` → OrderService `OrdersController` → `OrderDto[]`.

## Data Model (as persisted — not a hand-written SQL schema, this is what EF Core's conventions + the mappings in `OrderContext` produce)

### Orders
| Column | Type | Notes |
|---|---|---|
| `Id` | `int` (identity) | PK |
| `CreatedAt` | `timestamp` | |
| `Status` | `varchar(50)` | via `OrderStatus` conversion |
| `Total` | `decimal(18,2)` | via `Money` conversion |

### OrderItems
| Column | Type | Notes |
|---|---|---|
| `Id` | `int` (identity) | PK |
| `OrderId` | `int` | FK → `Orders.Id`, cascade delete |
| `ProductId` | `uuid` (Guid) | **not `int`** — matches ProductService/InventoryService's product id type |
| `Quantity` | `int` | |
| `UnitPrice` | `decimal(18,2)` | via `Money` conversion |

Note: the Kafka `OrderCreatedEvent` (in `Shared/Messages/OrderCreatedEvent.cs`) carries `OrderId` and `ProductId` as **strings**, regardless of their native `int`/`Guid` types above — the event contract normalizes both rather than mirroring each field's real type.

## Key Features (verified against current code)

1. **DDD Domain layer**: aggregate root, value objects, enforced invariants and state transitions — not present in the pre-refactor version this document used to describe.
2. **BFF Pattern**: GatewayBff aggregates prices/inventory before forwarding to OrderService.
3. **CQRS-shaped routing**: OrderService itself splits `OrdersController` (query) from `OrderCommandsController` (command) in one file; GatewayBff does the same at its own layer.
4. **Non-blocking Kafka publish**: an order is never lost because Kafka is unreachable — but see the Mock producer caveat above; today there's no way to run without Kafka reachable at all in Development.
5. **Price integrity**: prices are fetched from ProductService by GatewayBff at order-creation time, not trusted from the client.
6. **Status management**: enforced via `OrderStatus.TransitionTo`, not a free-form string field.
7. **Responsive Angular UI**: unchanged from the original implementation.

## Known Gaps

1. **`MockOrderEventProducer` is unused** — `Program.cs` always registers the real Kafka producer regardless of environment, unlike NotificationService's mock/real split by `IsDevelopment()`.
2. **No automated tests** — none exist for this service (or any service) in the solution.
3. **`context.Database.EnsureCreated()` instead of migrations** — schema changes require dropping/recreating the database in Development; there's no migration history to review.
4. **Domain events (`OrderCreatedDomainEvent`, `OrderStatusChangedDomainEvent`) are collected but not dispatched** to any in-process handler — the actual Kafka publish is a separate, manually-constructed `OrderCreatedEvent` built directly in `OrderApplicationService`, not driven by the collected domain event.

## File Summary (current DDD layout — supersedes the flat-file list this document used to have)

```
OrderService/
  Domain/{SeedWork, Aggregates, ValueObjects, Events, Exceptions, Interfaces}/
  Application/Services/OrderApplicationService.cs
  Infrastructure/{Data, Repositories, Messaging, Configuration}/
  Controllers/OrdersController.cs   (contains OrdersController + OrderCommandsController)
  Program.cs
```

The GatewayBff and Angular frontend files listed in earlier revisions of this document (`CreateOrderCommand.cs`, `GetAllOrdersQuery.cs`, `GetOrderByIdQuery.cs`, `OrderDtos.cs`, the `orders`/`create-order` Angular components) are unchanged in shape and still accurate.
