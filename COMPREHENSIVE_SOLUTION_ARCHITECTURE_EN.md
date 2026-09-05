# Distributed Order System — Solution Architecture

## Table of Contents
1. [System Overview](#system-overview)
2. [Solution Layout](#solution-layout)
3. [High-Level Architecture](#high-level-architecture)
4. [Ports Reference](#ports-reference)
5. [Services](#services)
6. [Event-Driven Flow](#event-driven-flow)
7. [Architectural Patterns](#architectural-patterns)
8. [Frontends](#frontends)
9. [Infrastructure](#infrastructure)
10. [Local Development](#local-development)
11. [Current Gaps](#current-gaps)

---

## System Overview

A .NET 9 microservices e-commerce backend: nine independently deployable services plus a custom BFF, fronted by two separate, independently-evolving frontends (an Angular SPA and a Razor Pages storefront) and orchestrated locally either via Docker Compose or a .NET Aspire AppHost.

The services are not uniformly built. This is deliberate: **OrderService is the reference DDD implementation**, ProductService is a Clean Architecture + CQRS reference, and the rest use simpler, pragmatic layering appropriate to their scope (several — PaymentService in particular — are stubs). The system is best read as "one thoroughly-built vertical slice, several supporting services at varying levels of completeness," not as a uniformly production-ready platform.

### Technology Stack

**Backend**
- .NET 9 (C#) for all services; the new `AppHost` project targets .NET 10 (required by the Aspire SDK preview it uses)
- ASP.NET Core Web API (Controllers, not Minimal APIs, except for a couple of health/demo endpoints)
- Entity Framework Core 9 + Npgsql
- MediatR 12 (CQRS) in ProductService and GatewayBff
- FluentValidation in ProductService (MediatR pipeline behavior) and CustomerWebsite

**Data**
- PostgreSQL 16 (`postgres:16-alpine`) — one physical instance, one database per service (`OrderDb_Dev`, `ProductDb_Dev`, `InventoryDb`, `UserServiceDb`, `ChatbotDb_Dev`, `DistributedOrderSystemNotifications`)
- Redis (`redis:latest`) — cache-aside for Product/Inventory/Chatbot, and the *only* store for CustomerService (cart/wishlist — no database)

**Messaging**
- Apache Kafka 7.4 (Confluent, KRaft mode, no Zookeeper), single broker
- Topic: `order-created`, producer: OrderService, consumer: InventoryService

**AI**
- ChatbotService: ONNX Runtime + Microsoft.ML running a local DialoGPT-small model, plus a lightweight rule-based NLP fallback

**Frontends**
- Angular 20.1 SPA (standalone components, Signals API, SSR-capable) — the primary client, talks to GatewayBff
- ASP.NET Core MVC/Razor "CustomerWebsite" — a second, independent storefront that calls the backend services **directly**, with its own session-based cart, Stripe checkout integration, and Serilog logging. It does not go through GatewayBff.

**Orchestration**
- Docker Compose (`docker/docker-compose.yml`) — the main way to run the full stack
- .NET Aspire `AppHost` (new) — an alternative local orchestrator that currently wires up only ProductService, InventoryService, OrderService, and GatewayBff (infra — Postgres/Redis/Kafka — is expected to already be running via Compose)

---

## Solution Layout

```
src/
  AppHost/              .NET Aspire orchestrator (net10.0) — Product/Inventory/Order/GatewayBff only
  GatewayBff/            Custom MediatR-based BFF (not Ocelot/YARP)
  OrderService/          DDD reference implementation, Kafka producer
  ProductService/        Clean Architecture + CQRS (MediatR) reference, Redis cache-aside
  InventoryService/      Kafka consumer (BackgroundService), Redis cache-aside
  PaymentService/        Stub — WeatherForecast template only, no business logic
  NotificationService/   Postgres + Hangfire + SignalR; SMS/Email/Push (mocked in Development)
  UserService/           ASP.NET Identity + JWT + Google/Facebook OAuth2
  CustomerService/       Redis-only cart/wishlist (no database)
  ChatbotService/        Postgres + Redis + ONNX Runtime (DialoGPT-small)
  Shared/                Shared.Messages.OrderCreatedEvent (the Kafka contract)
  frontend/
    distributed-order-app/          Angular 20.1 SPA
    customer-facing e-commerce/
      CustomerWebsite/               Independent ASP.NET MVC storefront (Stripe, session cart)
```

14 projects total in `DistributedOrderSystem.sln`.

---

## High-Level Architecture

```
┌──────────────────────────────┐        ┌────────────────────────────────────┐
│  Angular SPA (4200)           │        │  CustomerWebsite Razor MVC (5100)  │
│  standalone components        │        │  independent storefront             │
│  + Signals                    │        │  Stripe checkout, session cart      │
└───────────────┬───────────────┘        └───────────────┬─────────────────────┘
                │ HTTP                                    │ HTTP (calls services directly,
                ▼                                         │  bypasses GatewayBff)
┌───────────────────────────────┐                         │
│  GatewayBff (5189)             │◄────────────────────────┘ (partially — see note below)
│  MediatR Commands + Queries    │
│  named HttpClients: Product/   │
│  Inventory/Order               │
└──┬───────────┬───────────┬────┘
   │           │           │
   ▼           ▼           ▼
┌────────┐ ┌───────────┐ ┌────────────┐        ┌──────────────┐ ┌───────────────┐
│Product │ │Inventory  │ │Order       │        │Customer      │ │Payment (stub) │
│Service │ │Service    │ │Service     │        │Service        │ │               │
│ :5198  │ │ :5051     │ │ :5003      │        │ :5009         │ │ :5034         │
│CQRS +  │ │Kafka      │ │DDD         │        │Redis cart/    │ │no business    │
│Redis   │ │consumer   │ │aggregate + │        │wishlist only  │ │logic yet      │
│cache   │ │+ Redis    │ │Kafka       │        └──────────────┘ └───────────────┘
└───┬────┘ └────┬──────┘ │producer    │
    │           │        └─────┬──────┘
    │           │              │ publishes OrderCreatedEvent
    │           │              ▼
    │           │        ┌──────────────────────┐
    │           └───────►│ Kafka (order-created)│
    │                    └──────────┬───────────┘
    │                               │ consumed by InventoryService
    │                               ▼
    │                      (stock decremented)
    ▼
┌─────────────────────────────────────────────────────────────┐
│ PostgreSQL 16 (one instance, one DB per service)             │
│ Redis (one instance, shared)                                 │
└─────────────────────────────────────────────────────────────┘

Also present, not on the GatewayBff request path:
┌────────────────┐  ┌──────────────────┐  ┌────────────────────────┐
│ UserService     │  │ NotificationService│ │ ChatbotService          │
│ :5010           │  │ :5246 (Hangfire,   │ │ :5055 (ONNX DialoGPT,  │
│ Identity + JWT  │  │ SignalR, Twilio/   │ │ JWT, admin dashboard,  │
│ + OAuth2        │  │ MailKit/Firebase)  │ │ JS chat widget)         │
└────────────────┘  └──────────────────┘  └────────────────────────┘
```

**Note on the Angular ↔ GatewayBff path**: GatewayBff's `CommandsController`/`QueriesController` use named `IHttpClientFactory` clients (`ProductService`, `InventoryService`, `OrderService`) configured from `ServiceUrls` in `appsettings`. Its `CartBffController`/`WishlistBffController`, however, call CustomerService through an ad-hoc `HttpClient` with the URL **hardcoded** to `http://localhost:5009` (see `AddToCartCommand.cs`) rather than through a named/configured client. Functionally it's still "SPA → GatewayBff → downstream service," but the cart/wishlist path isn't wired as consistently as the catalog/order path.

---

## Ports Reference

| Service | Port (HTTP) | Backing store | Notes |
|---|---|---|---|
| Angular SPA | 4200 | — | `ng serve` |
| CustomerWebsite (Razor) | 5100 | — (calls services directly) | independent storefront |
| GatewayBff | 5189 | — | stateless |
| ProductService | 5198 | PostgreSQL + Redis | |
| InventoryService | 5051 | PostgreSQL + Redis | Kafka consumer |
| OrderService | 5003 | PostgreSQL | Kafka producer |
| PaymentService | 5034 | — | stub |
| NotificationService | 5246 | PostgreSQL + Redis | Hangfire dashboard at `/hangfire`, SignalR hub at `/hubs/notifications` |
| UserService | 5010 | PostgreSQL | Identity + JWT |
| CustomerService | 5009 | Redis only | no database |
| ChatbotService | 5055 | PostgreSQL + Redis | admin dashboard at `/api/admin` |
| PostgreSQL | 5432 | — | `postgres:16-alpine` |
| Redis | 6379 | — | |
| Kafka (internal) | 9092 | — | `PLAINTEXT`, used by containerized services (`kafka:9092`) |
| Kafka (host) | 29092 | — | `PLAINTEXT_HOST`, used by services running directly on the host against the dockerized broker |
| Kafka controller | 9093 | — | KRaft, internal only |
| Kafka UI | 8080 | — | `provectuslabs/kafka-ui` |
| AppHost dashboard | 15011 / 17204 | — | Aspire dashboard, when launched via `AppHost` |

---

## Services

### OrderService — DDD Reference Implementation

The only service with a full DDD structure; treat it as the pattern to copy when a service's business rules justify the ceremony.

```
Domain/
  Aggregates/     Order (aggregate root), OrderItem (child entity, internal constructor)
  ValueObjects/   Money (non-negative invariant), OrderStatus (enforced state machine)
  Events/         OrderCreatedDomainEvent, OrderStatusChangedDomainEvent
  Interfaces/     IOrderRepository
  SeedWork/       AggregateRoot, Entity, ValueObject, IDomainEvent
Application/
  Services/       OrderApplicationService — orchestrates Domain + Infrastructure, no business rules
Infrastructure/
  Data/           OrderContext (EF Core / Npgsql)
  Messaging/      OrderEventProducer (Confluent.Kafka), MockOrderEventProducer
  Repositories/   OrderRepository
Controllers/      OrdersController — thin, delegates to IOrderApplicationService
```

- `Order.Create(...)` is the only way to construct an order; it throws `OrderDomainException` if no items are supplied.
- `OrderStatus` enforces a fixed transition table: `Pending → {Confirmed, Cancelled}`, `Confirmed → {Shipped, Cancelled}`, `Shipped → Delivered`; `Delivered`/`Cancelled` are terminal. Invalid transitions throw.
- `Order.Id` and `OrderItem.OrderId` are `int` (DB identity), while `OrderItem.ProductId` is a `Guid` — the same type ProductService/InventoryService use for products. Cross-service, the order id travels as a `string` inside `OrderCreatedEvent`.
- Kafka publish failures are caught and logged in `OrderApplicationService.CreateOrderAsync` — **the order is still persisted and returned** even if Kafka is unreachable.

### ProductService — Clean Architecture + CQRS

```
Domain/Entities/Product.cs        Id (Guid), Name, Price, Description, IsActive — no external deps
Application/
  Commands/    CreateProduct, UpdateProduct, DeleteProduct (+ FluentValidation validators)
  Queries/     GetAllProducts, GetProductById, SearchProducts
  Common/Behaviors/  ValidationBehavior, LoggingBehavior (MediatR IPipelineBehavior<,>)
Infrastructure/
  Data/        ProductDbContext
  Cache/       RedisProductCache (IProductCache), InMemoryProductCache (fallback)
  Repositories/ ProductRepository
Controllers/   ProductsController
```

- Every command/query goes through the MediatR pipeline: request → `ValidationBehavior` (FluentValidation) → `LoggingBehavior` → handler.
- Cache-aside via Redis, registered as a singleton `IProductCache`.
- `context.Database.EnsureCreated()` runs at startup — no migrations-based deployment for this service.

### InventoryService

```
Data/InventoryContext.cs                InventoryItem: Id (int), ProductId (Guid), AvailableQuantity,
                                          ReservedQuantity, LastUpdatedUtc
Cache/RedisInventoryCache.cs
Messaging/OrderCreatedConsumer.cs       BackgroundService, Confluent.Kafka
Services/InventoryWorkerServices.cs
Controllers/InventoryController.cs
```

`OrderCreatedConsumer` (group `inventory-service`, manual offset commit):
- `EnableAutoCommit = false`, `EnableAutoOffsetStore = false` — the offset is committed and stored explicitly only after `ProcessMessageAsync` succeeds for **every** item in the event.
- If adjusting inventory for any item throws, the exception is rethrown (not swallowed) so the offset is *not* committed and the message is redelivered — i.e., **at-least-once**, not exactly-once.
- A 2-second startup delay is hardcoded before the consume loop begins ("let the host finish starting").

### GatewayBff — Custom MediatR BFF

Not Ocelot/YARP — a hand-rolled BFF. Two aggregation-facing controllers plus two pass-through ones:

- `QueriesController` (`/api/queries`): `GET catalog`, `GET catalog/{id}`, `GET orders`, `GET orders/{id}` — `GetCatalogQueryHandler` calls ProductService then, per product, calls InventoryService and merges (inventory lookup failures are caught per-item and default to 0, not fatal to the whole request).
- `CommandsController` (`/api/commands`): order creation, product CRUD, inventory adjust/set — all via MediatR commands over the named `ProductService`/`InventoryService`/`OrderService` HTTP clients.
- `CartBffController` (`/api/cart`) / `WishlistBffController` (`/api/wishlist`): proxy to CustomerService, but via a raw `HttpClient` with `http://localhost:5009` hardcoded rather than the named-client pattern used elsewhere (see note above).

### CustomerService

Redis-only — no database, by design (cart/wishlist are ephemeral). `RedisCartStorage`/`RedisWishlistStorage` implement `ICartStorage`/`IWishlistStorage`; `CartService`/`WishlistService` sit on top. Registered as singletons.

### PaymentService — Stub

`Program.cs` is still the default ASP.NET template (`/weatherforecast` minimal API). No controllers, no domain code, no database wiring, even though `appsettings.Development.json` already has `Kafka` settings scaffolded (`OrderCreatedTopic`, `PaymentProcessedTopic`, consumer group `payment-service`) for whenever it's built out.

### NotificationService

- PostgreSQL (`NotificationContext`) + Hangfire (`Hangfire.PostgreSql` storage, dashboard at `/hangfire`, open authorization filter in dev) + SignalR (`NotificationHub` at `/hubs/notifications`) + Redis (`IConnectionMultiplexer`, wired but the SignalR Redis backplane itself is not — noted directly in the code as "AddStackExchangeRedis extension is not available").
- Channel implementations are swapped by environment: `MockSmsService`/`MockEmailService`/`MockPushService` in Development, `TwilioSmsService`/`MailKitEmailService`/`FirebasePushService` otherwise.
- `context.Database.EnsureCreated()` runs at startup in Development.

### UserService

ASP.NET Core Identity (`ApplicationUser`, `IdentityRole<Guid>`) + JWT Bearer (15-minute expiry, per CLAUDE.md) + Google and Facebook OAuth2 providers, all configured in `Program.cs`. Seeds `Admin`/`Customer`/`Vendor` roles on startup. Controllers: `AuthController`, `UsersController`, `AddressesController`, `PaymentMethodsController` (the latter two nested under `/api/users/{userId}/...`).

### ChatbotService

- PostgreSQL (`ChatContext`: chat sessions, messages, training data) + Redis (session/response cache).
- NLP: `AdvancedNLPService` drives a locally-hosted **DialoGPT-small** model via ONNX Runtime / ML.NET (model files under `Models/Downloaded/DialoGPT-small/`), with a `LightweightNLPService` fallback.
- JWT auth + an `Admin` authorization policy; `FineTuningController`/`FineTuningService` for retraining; `ServiceIntegrationService` (typed `HttpClient`) is meant to call the other services but currently only the client itself is wired, not the full integration.
- Ships its own embeddable JS chat widget (`ChatWidgetController`, `wwwroot/chat-widget/`) with both a "BFF routing" and a "direct service" integration mode.
- Root path (`GET /`) redirects to `/api/admin`.

### AppHost — .NET Aspire Orchestrator (new)

Added this session. Targets `net10.0` (the Aspire AppHost SDK's current requirement) while every other service stays on `net9.0` — this is a real, intentional version split, not an inconsistency to fix.

```csharp
var productService = builder.AddProject<Projects.ProductService>("productservice", launchProfileName: "http");
var inventoryService = builder.AddProject<Projects.InventoryService>("inventoryservice", launchProfileName: "http");
var orderService = builder.AddProject<Projects.OrderService>("orderservice", launchProfileName: "http");

builder.AddProject<Projects.GatewayBff>("gatewaybff", launchProfileName: "http")
    .WaitFor(productService).WaitFor(inventoryService).WaitFor(orderService)
    .WithReference(productService).WithReference(inventoryService).WithReference(orderService);
```

It does **not** manage Postgres, Redis, or Kafka — those still come from `docker compose -f docker/docker-compose.yml up -d postgres redis kafka`, and it does not (yet) include PaymentService, NotificationService, UserService, CustomerService, or ChatbotService. Launch via VS Code's `AppHost (Aspire)` configuration (`.vscode/launch.json`), which runs the `build AppHost` task first (`.vscode/tasks.json`).

---

## Event-Driven Flow

1. Client calls `POST /api/commands/orders` on GatewayBff → `CreateOrderCommand` → OrderService `POST /api/orders`.
2. OrderService: `Order.Create(...)` builds the aggregate and enforces invariants → persisted via `OrderRepository` → `OrderEventProducer` publishes `Shared.Messages.OrderCreatedEvent` to Kafka topic `order-created` (acks=all, idempotent producer per CLAUDE.md).
3. **Kafka publish failure is non-fatal**: caught, logged, the already-persisted order is still returned to the caller.
4. InventoryService's `OrderCreatedConsumer` (consumer group `inventory-service`, manual commit) picks up the event, decrements `AvailableQuantity` per item. A processing exception skips the commit, so the broker redelivers the message — at-least-once delivery, no idempotency guard in the consumer beyond that.

Kafka itself runs with **two listeners** for exactly this reason: `PLAINTEXT` on `9092` (container-to-container, e.g. `kafka:9092`) and `PLAINTEXT_HOST` on `29092` (host-to-container, e.g. a service running via `dotnet run` or the AppHost against the dockerized broker on `localhost:29092`). Services running *inside* Docker Compose use `9092`; `appsettings.Development.json` for OrderService/InventoryService/PaymentService (for local, non-containerized runs) use `29092`.

---

## Architectural Patterns

**DDD** — OrderService only (see above). Not applied elsewhere; don't expect aggregates/value objects/domain events in ProductService or InventoryService.

**CQRS**
- ProductService: full MediatR command/query separation with a validation + logging pipeline.
- GatewayBff: `CommandsController`/`QueriesController` split, but commands here are mostly thin proxies to downstream services rather than carrying their own business logic.

**BFF (Backend for Frontend)** — GatewayBff aggregates ProductService + InventoryService calls into a single catalog response and fans out order/product/inventory commands, so the Angular SPA makes one call instead of several. The cart/wishlist path bypasses this pattern (direct proxy, hardcoded URL — see GatewayBff section).

**Cache-Aside (Redis)** — ProductService, InventoryService, CustomerService (as primary store, not just cache), ChatbotService, NotificationService (partial — connection wired, SignalR backplane not).

**Repository Pattern** — OrderService (`IOrderRepository`), ProductService (`IProductRepository`).

**Consumer/Producer (Kafka)** — OrderService produces, InventoryService consumes; PaymentService has consumer *settings* but no consumer implementation yet.

---

## Frontends

### Angular SPA (`distributed-order-app`, port 4200)
Angular 20.1, standalone components, Signals API for state. Talks to GatewayBff. Structure: `components/{create-order, inventory, not-found, orders, product-form, products}`, `models/`, `services/`.

### CustomerWebsite — Independent Razor MVC Storefront (port 5100)
A second, separately-built storefront under `src/frontend/customer-facing e-commerce/CustomerWebsite/`. Notable because it does **not** go through GatewayBff or the Angular app at all:
- Calls ProductService/OrderService directly via its own `IProductService`/`IOrderService`/`IShoppingCartService` wrappers over a plain `HttpClient` (base URLs read from `Services:*:BaseUrl` config, defaulting to hardcoded `localhost` ports).
- Session-based cart (`AddDistributedMemoryCache` + cookie session), Stripe.net for checkout, AutoMapper, FluentValidation, Serilog file logging.
- References `Microsoft.EntityFrameworkCore.SqlServer`, but `Program.cs` never calls `AddDbContext` — the package is present but unused; this project has no database of its own.
- Custom global exception middleware and status-code re-execution for error pages.

Treat this as an experimental/parallel frontend, not a component of the GatewayBff-centered request flow described in CLAUDE.md.

---

## Infrastructure

`docker/docker-compose.yml` runs, on one bridge network (`dos_network`):

| Container | Image | Notes |
|---|---|---|
| `dos_postgres` | `postgres:16-alpine` | healthcheck-gated; all service containers `depends_on` it as `service_healthy` |
| `dos_redis` | `redis:latest` | |
| `dos_kafka` | `confluentinc/cp-kafka:7.4.0` | KRaft, dual listener (see above) |
| `dos_kafka_ui` | `provectuslabs/kafka-ui` | port 8080, dynamic config enabled |
| `dos_order_service` … `dos_gateway_bff` | built from each service's `Dockerfile` (`context: ../src`) | connection strings and `Kafka__BootstrapServers` are passed as container env vars pointing at `postgres`/`kafka`/`redis` (service names), separately from each service's own `appsettings.Development.json`, which targets `localhost` for non-containerized runs |

Volumes: `postgres_data` only (Redis and Kafka are not persisted across `docker compose down`).

---

## Local Development

Three ways to run the backend, not mutually exclusive:

1. **Full Docker Compose**: `docker compose -f docker/docker-compose.yml up -d` — everything containerized.
2. **Infra-only Compose + services on host**: `docker compose -f docker/docker-compose.yml up -d dos_postgres dos_redis dos_kafka dos_kafka_ui`, then `dotnet run --project src/<Service>` per service (uses `localhost:29092` for Kafka, `localhost:5432`/`localhost:6379` for Postgres/Redis).
3. **AppHost (Aspire)**: infra via Compose as in (2), then launch `AppHost (Aspire)` from VS Code (or `dotnet run --project src/AppHost`) to start ProductService, InventoryService, OrderService, and GatewayBff together under the Aspire dashboard. PaymentService/NotificationService/UserService/CustomerService/ChatbotService still need to be started separately if needed.

Frontend: `cd src/frontend/distributed-order-app && npm install && ng serve` → `http://localhost:4200`.

---

## Current Gaps

Matches CLAUDE.md's "what doesn't exist yet," plus specifics found while writing this doc:

- **No tests** — no test projects anywhere in the solution.
- **No CI/CD** — `.github/workflows/` is empty.
- **No Kubernetes/Helm.**
- **PaymentService is unimplemented** — template code only.
- **No API gateway authentication** — UserService issues JWTs and ChatbotService/UserService validate them independently, but GatewayBff does not propagate or enforce auth on the Product/Inventory/Order path.
- **CustomerWebsite and the Angular SPA are two independent, non-integrated frontends** hitting the backend two different ways (direct-to-service vs. through GatewayBff).
- **GatewayBff's cart/wishlist proxying uses a hardcoded URL**, not the configured `ServiceUrls`/named-client pattern used for Product/Inventory/Order.
- **AppHost covers 4 of 9 services** — it's a new, partial orchestration path, not a full replacement for Docker Compose yet.
- **Kafka messages are at-least-once, not exactly-once** — consumers must tolerate redelivery; InventoryService currently has no explicit idempotency check beyond the manual-commit ordering.
