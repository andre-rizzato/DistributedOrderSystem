# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Commands

### .NET Services

Run a single service (from its directory):
```bash
dotnet run --project src/OrderService
```

Build the entire solution:
```bash
dotnet build DistributedOrderSystem.sln
```

Run all tests (no test projects exist yet — this is where they'd go):
```bash
dotnet test DistributedOrderSystem.sln
```

Add a new EF Core migration for a service:
```bash
dotnet ef migrations add <MigrationName> --project src/OrderService
```

Apply migrations:
```bash
dotnet ef database update --project src/OrderService
```

### Infrastructure (Docker)

Start all infrastructure (PostgreSQL, Redis, Kafka, Kafka UI):
```bash
docker compose -f docker/docker-compose.yml up -d dos_postgres dos_redis dos_kafka dos_kafka_ui
```

Start everything including all services:
```bash
docker compose -f docker/docker-compose.yml up -d
```

### Angular Frontend

From `src/frontend/distributed-order-app/`:
```bash
npm install       # first time
ng serve          # dev server at http://localhost:4200
ng build          # production build → dist/
ng test           # unit tests via Karma
```

---

## Architecture

### Request Flow

```
Angular SPA (4200)
    └── HTTP → GatewayBff (5189)
                    ├── ProductService (5198)   — product catalog
                    ├── InventoryService (5051) — stock levels
                    └── OrderService (5003)     — order creation
                                └── Kafka (9092) → InventoryService (consumer)
```

The GatewayBff is a **custom MediatR-based BFF** (not Ocelot/YARP). Each incoming request maps to a MediatR command or query; the handlers call downstream services via named `IHttpClientFactory` clients. Downstream URLs are configured in `appsettings.Development.json` under `ServiceUrls`.

### Event-Driven Flow

1. `OrderService` creates an order and publishes `OrderCreatedEvent` to Kafka topic `order-created` via `OrderEventProducer` (acks=all, idempotent).
2. `InventoryService` runs `OrderCreatedConsumer` as a `BackgroundService`, consuming that topic (group `inventory-service`, manual offset commit) and decrements stock via `InventoryWorkerService`.
3. The shared contract lives in `src/Shared/Messages/OrderCreatedEvent.cs`.

Kafka failure during publish is **non-fatal** — the order is still persisted and an error is logged (see `OrderApplicationService.CreateOrderAsync`).

### DDD in OrderService (reference implementation)

OrderService is the only service with a full DDD structure:

```
Domain/
  Aggregates/   — Order (aggregate root), OrderItem (child entity)
  ValueObjects/ — Money, OrderStatus (with enforced state transitions)
  Events/       — OrderCreatedDomainEvent, OrderStatusChangedDomainEvent
  Interfaces/   — IOrderRepository (contract, implemented in Infrastructure)
  SeedWork/     — AggregateRoot, Entity, ValueObject, IDomainEvent base types
Application/
  Services/     — OrderApplicationService (orchestrates Domain + Infrastructure; no business logic here)
Infrastructure/
  Data/         — OrderContext (EF Core), migrations
  Messaging/    — OrderEventProducer (Confluent.Kafka)
  Repositories/ — OrderRepository (implements IOrderRepository)
Controllers/    — thin; delegate entirely to IOrderApplicationService
```

Business rules live exclusively in the Domain layer. The aggregate exposes factory methods (`Order.Create(...)`) instead of public constructors. State transitions are enforced inside value objects (`OrderStatus.TransitionTo`).

Other services (ProductService, InventoryService, etc.) use simpler layering without full DDD aggregates.

### Key Services

| Service | Persistence | Notes |
|---------|------------|-------|
| OrderService | PostgreSQL | DDD aggregate, Kafka producer |
| ProductService | PostgreSQL + Redis | MediatR commands/queries |
| InventoryService | PostgreSQL + Redis | Kafka consumer (BackgroundService) |
| CustomerService | Redis only | No DB — cart/wishlist are ephemeral |
| NotificationService | PostgreSQL + Hangfire | SMS (Twilio), Email (MailKit), Push (Firebase), SignalR |
| UserService | PostgreSQL | ASP.NET Identity + JWT (15 min expiry) + Google/Facebook OAuth2 |
| ChatbotService | PostgreSQL + Redis | ONNX Runtime + TensorFlow inference |
| PaymentService | PostgreSQL | Stub — no business logic yet |
| GatewayBff | None | Stateless; MediatR + named HttpClients |

### Infrastructure

- **PostgreSQL 16** — primary database; EF Core 9 + Npgsql; each service has its own DB
- **Redis** — distributed cache (TTL 30 min for products/inventory) and session/cart storage
- **Kafka 7.4 KRaft** — no Zookeeper; single broker; Kafka UI at port 8080
- **Hangfire** — background job scheduler in NotificationService, backed by PostgreSQL

### Frontend

- **Angular 20.1** at `src/frontend/distributed-order-app/` — uses standalone components and the Signals API for reactive state; SSR enabled
- **ASP.NET Razor Pages** at `src/frontend/customer-facing e-commerce/` — customer-facing e-commerce site

### What doesn't exist yet

- No CI/CD (`.github/workflows/` is empty)
- No Kubernetes or Helm charts
- No test projects
