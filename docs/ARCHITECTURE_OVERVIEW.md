# Distributed Order System – Detailed Architecture Guide

This document provides an **in-depth description** of the architecture across all microservices in the `DistributedOrderSystem` solution. It goes beyond the previous overview by detailing structure, reasoning, and practical navigation for each service and explaining why every layer behaves as it does.

> Updated to match the code as it actually stands today. The previous version of this document lumped six very different services together as "remain N-tier for now" — in practice one of them (`PaymentService`) has no implementation at all, and the rest range from a plain layered service to framework-heavy auth/ML services that aren't meaningfully "simple." Those claims have been replaced with what's actually in each service's folders and `Program.cs` below.

---

## Overview

The solution contains multiple independent microservices, a Backend-for-Frontend (BFF) gateway, and a new local orchestrator:

- **OrderService** – built using Domain-Driven Design (DDD); the only service with a full DDD structure
- **ProductService** – implemented with Clean Architecture and full CQRS via MediatR
- **InventoryService** – simple layered service plus a Kafka consumer `BackgroundService`
- **CustomerService** – simple layered service, but with **no database at all** (Redis is its only store)
- **UserService** – ASP.NET Core Identity + JWT + OAuth2, not a "simple" service by folder shape alone
- **NotificationService** – layered service, but built around Hangfire (background jobs) and SignalR (real-time), with per-channel provider implementations (Twilio/MailKit/Firebase)
- **ChatbotService** – layered service wrapping an ONNX Runtime/DialoGPT-small NLP engine, its own JWT auth, an admin dashboard, and an embeddable JS widget
- **PaymentService** – **not implemented**: `Program.cs` is still the unmodified ASP.NET template; there are no Controllers/Models/Services/Data folders to speak of
- **GatewayBff** – an API gateway using MediatR (CQRS-light) to orchestrate calls to backend services
- **AppHost** – a new .NET Aspire orchestrator (targets `net10.0`, one version ahead of every other service's `net9.0`) that can start ProductService, InventoryService, OrderService, and GatewayBff together as an alternative to running each with `dotnet run`
- **Shared** – a library with common DTOs/messages (Kafka events)

The architectural goal is to demonstrate multiple approaches in one codebase:
1. DDD for a complex domain with rich business rules (`OrderService`).
2. Clean Architecture with strict dependency inversion and CQRS for a service with high read/write separation (`ProductService`).
3. A simple layered service pattern where the domain is genuinely simple (`InventoryService`, `CustomerService`).
4. Framework-driven services where most of the complexity comes from a third-party integration rather than custom layering (`UserService`'s Identity/OAuth stack, `NotificationService`'s Hangfire/SignalR, `ChatbotService`'s ONNX/ML pipeline).
5. A BFF that offers well-structured commands/queries and uses HTTP clients — with one part of it (cart/wishlist) not actually following that structure (see [GatewayBff](#gatewaybff) below).

Each pattern answers different needs; the documentation below explains them in detail.

---

## OrderService (Domain-Driven Design)

### Purpose & Philosophy
Order processing has non-trivial business rules (status transitions, money invariants, event publication). DDD helps encapsulate this complexity in a rich domain model and makes rules explicit.

### Project Structure
```
OrderService/
  Domain/
    SeedWork/          # base DDD primitives (Entity, AggregateRoot, ValueObject)
    Aggregates/        # aggregate roots and entities (Order, OrderItem)
    ValueObjects/      # immutable objects with equality (Money, OrderStatus)
    Events/            # domain events
    Exceptions/        # domain-specific exception types
    Interfaces/        # repository contracts
  Application/
    Services/          # application service orchestrating use cases
  Infrastructure/
    Data/               # EF Core DbContext with value-object conversions
    Repositories/       # concrete repository implementations
    Messaging/          # Kafka producers and mocks
    Configuration/      # strongly-typed settings
  Controllers/         # thin presentation layer
  Program.cs           # DI container config
```

#### Key Decisions
- **Aggregate Root (`Order`)** ensures all business invariants are enforced before saving. `Order.Create(...)` is the only entry point — there's no public constructor, and creating an order with zero items throws `OrderDomainException`.
- **Value Objects** (`Money`, `OrderStatus`) encapsulate concepts and prevent invalid states. `OrderStatus` enforces a fixed transition table (`Pending → {Confirmed, Cancelled}`, `Confirmed → {Shipped, Cancelled}`, `Shipped → Delivered`; `Delivered`/`Cancelled` are terminal) — an invalid transition throws rather than silently succeeding.
- **Domain Events** collected on the aggregate and dispatched by the application service support eventual consistency (inventory update via Kafka).
- **Repository interface in Domain** decouples the domain from EF Core; infrastructure implements it.
- **Application Service** orchestrates domain operations and side-effects (publishing events). If the Kafka publish throws, `OrderApplicationService.CreateOrderAsync` catches and logs it — **the order is still persisted and returned to the caller**.
- **Controllers** merely map HTTP to the application service; no business logic.

### Navigation Tips
- Start with `Domain/Aggregates/Order.cs` to see the core business rules.
- Check `Domain/ValueObjects` for supporting logic (e.g. state transitions).
- Look at `Application/Services/OrderApplicationService.cs` for use case orchestration.
- Repository implementation is in `Infrastructure/Repositories/OrderRepository.cs`.
- Configuration and Kafka logic live under `Infrastructure`.

---

## ProductService (Clean Architecture + Full CQRS)

### Purpose & Philosophy
The product catalog is read-heavy with many query shapes. Using Clean Architecture ensures business rules are isolated from infrastructure. CQRS separates read and write models, enabling asynchronous scaling, efficient caching, and clearer reasoning about behavior.

### Project Structure
```
ProductService/
  Domain/
    Entities/          # core domain entities (Product)
    Interfaces/        # repository contracts
  Application/
    Commands/          # all write operations (create/update/delete)
      CreateProduct/
      UpdateProduct/
      DeleteProduct/
    Queries/           # read operations (get all, get by id, search)
      GetAllProducts/
      GetProductById/
      SearchProducts/
    DTOs/              # read-model projections
    Common/
      Interfaces/       # application-level contracts (cache)
      Behaviors/        # MediatR pipeline behaviors (logging, validation)
  Infrastructure/
    Data/              # EF Core context
    Repositories/      # implementations of domain interfaces
    Cache/             # Redis and in-memory cache implementations
    Configuration/     # settings classes
  Controllers/         # thin endpoints sending commands/queries via MediatR
  Program.cs           # DI config including MediatR & FluentValidation
```

#### Key Decisions
- **Clean dependency direction**: `Domain/Entities/Product.cs` is a plain POCO (`Id`, `Name`, `Price`, `Description`, `IsActive`) with zero references to EF Core, Redis, or MediatR.
- **CQRS**: Commands mutate state; Queries return read-only DTOs. Handlers live in the Application layer.
- **MediatR Pipeline**: every request passes through `ValidationBehavior` (FluentValidation) then `LoggingBehavior` before reaching its handler — registered as `IPipelineBehavior<,>` in `Program.cs`.
- **FluentValidation**: Validators are defined per command (`CreateProductCommandValidator`, `UpdateProductCommandValidator`); the pipeline triggers them before handlers.
- **Caching**: `IProductCache` interface in Application; `RedisProductCache` (registered) and `InMemoryProductCache` (fallback implementation, not wired by default) in Infrastructure.
- **Presentation**: Controllers are extremely thin; every HTTP call becomes a MediatR request.
- `context.Database.EnsureCreated()` runs at startup — this service isn't deployed via EF Core migrations.

### Navigation Tips
- Read `Application/Commands/CreateProduct/CreateProductCommandHandler.cs` to see write-side flow.
- Inspect the corresponding validator to understand business constraints.
- Query handlers in `Application/Queries` show filtering logic.
- Domain/entity `Product.cs` is a simple POCO; logic resides in handlers.
- Infrastructure `ProductDbContext` maps the domain entity without polluting the domain.

---

## The Other Services

Unlike the "Other Services (Inventory, Payment, etc.)" framing this document used to have, these six services are not interchangeable examples of the same simple pattern. Each is described on its own terms below.

### InventoryService — genuinely simple layered service + Kafka consumer
```
InventoryService/
  Cache/          IInventoryCache, RedisInventoryCache
  Configuration/  KafkaSettings, RedisSettings
  Controllers/    InventoryController
  Data/           InventoryContext (EF Core)
  Messaging/      OrderCreatedConsumer (BackgroundService), MockOrderCreatedConsumer
  Models/         InventoryItem (Id, ProductId, AvailableQuantity, ReservedQuantity, LastUpdatedUtc)
  Services/       IInventoryWorkerService / InventoryWorkerServices
```
This is the one service that matches the original "Controllers → Services → Data, simple IService interfaces" description closely. Its one piece of real complexity is `OrderCreatedConsumer`: a `BackgroundService` with manual offset commit (`EnableAutoCommit = false`) — the offset is only committed and stored after every item in the event is processed successfully, so a mid-loop exception is rethrown (not swallowed) to force redelivery. This is at-least-once delivery, not exactly-once; there's no additional idempotency check beyond that ordering.

### CustomerService — layered, but no database
```
CustomerService/
  Controllers/  CartController, WishlistController
  Models/       CartItem, WishlistItem
  Services/     ICartService/CartService, IWishlistService/WishlistService
  Storage/      ICartStorage/RedisCartStorage, IWishlistStorage/RedisWishlistStorage
```
There is no `Data/` folder and no `DbContext` — Redis (via `RedisCartStorage`/`RedisWishlistStorage`, registered as singletons) is the only persistence this service has. Cart and wishlist are explicitly ephemeral data, not a simplified stand-in for a future database.

### UserService — Identity/JWT/OAuth framework service
```
UserService/
  Controllers/  AuthController, UsersController, AddressesController, PaymentMethodsController
  Data/         UserDbContext (ASP.NET Core Identity)
  Models/       ApplicationUser, Address, PaymentMethod, RefreshToken, DTOs/
  Services/     ITokenService/TokenService, IPaymentEncryptionService, ICommunicationService
```
Most of this service's behavior comes from `Microsoft.AspNetCore.Identity` + `Microsoft.AspNetCore.Authentication.JwtBearer` + Google/Facebook OAuth2 providers, all wired in `Program.cs`, not from custom layering. It seeds `Admin`/`Customer`/`Vendor` roles at startup. Calling this "simple N-tier" undersells what's actually configured here.

### NotificationService — Hangfire + SignalR, multi-channel
```
NotificationService/
  Controllers/  NotificationController, NotificationChannelControllers, PushAndInAppControllers
  Data/         NotificationContext (EF Core)
  Models/       NotificationModels, NotificationRequests, NotificationResponses
  Services/     Implementations/{EmailService, SmsService, PushService, InAppNotificationService, NotificationTemplateService}
```
`Program.cs` wires `Hangfire` (PostgreSQL-backed storage, dashboard at `/hangfire`) for background jobs and `SignalR` (`NotificationHub` at `/hubs/notifications`) for real-time in-app notifications. Channel implementations are swapped by environment: `MockSmsService`/`MockEmailService`/`MockPushService` in Development, `TwilioSmsService`/`MailKitEmailService`/`FirebasePushService` otherwise. The Redis connection multiplexer is registered, but the SignalR Redis backplane itself is not wired — the code comments on this directly ("AddStackExchangeRedis extension is not available").

### ChatbotService — ONNX/ML NLP pipeline + admin surface
```
ChatbotService/
  Controllers/  ChatController, ChatWidgetController, FineTuningController, SimpleAdminController, AiDashboardController
  Data/         ChatContext (chat sessions, messages, training data)
  Services/     AdvancedNLPService (ONNX Runtime + DialoGPT-small), LightweightNLPService (rule-based fallback),
                AuthenticationService, FineTuningService, ServiceIntegrationService
```
The NLP engine (`AdvancedNLPService`) runs a locally-hosted DialoGPT-small model via ONNX Runtime/ML.NET (model files under `Models/Downloaded/DialoGPT-small/`). The service has its own JWT auth and an `Admin` authorization policy, a fine-tuning subsystem, an admin dashboard (root `/` redirects to `/api/admin`), and ships an embeddable JS chat widget (`wwwroot/chat-widget/`) supporting both a "BFF routing" and a "direct service" integration mode. `ServiceIntegrationService` is a typed `HttpClient` meant to call the other microservices, but the wiring for it is closer to scaffolding than a complete integration.

### PaymentService — not implemented
```
PaymentService/
  Program.cs        # unmodified ASP.NET template
  appsettings*.json
```
There is no `Controllers/`, `Models/`, `Services/`, or `Data/` folder. `Program.cs` still exposes the default `/weatherforecast` minimal API from `dotnet new webapi`. `appsettings.Development.json` does have Kafka settings scaffolded (`OrderCreatedTopic`, `PaymentProcessedTopic`, consumer group `payment-service`) for a consumer that doesn't exist yet. `docs/PAYMENT_AND_NOTIFICATION_SERVICES_DESIGN.md` describes a design for this service, but none of it has been implemented.

---

## GatewayBff

The Backend-for-Frontend sits in `src/GatewayBff`. It uses MediatR but keeps a much lighter CQRS structure, and it is **not internally consistent** — two different integration styles coexist:

- **Commands/** and **Queries/** folders define request/handler pairs.
- **Contracts/** contains DTOs used for HTTP calls to backend microservices.
- `QueriesController`/`CommandsController` (catalog, orders, product/inventory commands) use **named** `IHttpClientFactory` clients (`"ProductService"`, `"InventoryService"`, `"OrderService"`) configured from `ServiceUrls` in `appsettings` — this is the pattern the rest of this section describes.
- `CartBffController`/`WishlistBffController`, however, are proxies to CustomerService built with an **unnamed** `HttpClient` and the URL hardcoded inline (`http://localhost:5009/...`) in all eight of their command/query handlers, bypassing `ServiceUrls` entirely. See `docs/BFF_ARCHITECTURE.md` for the full detail on this path, including the port mismatches in `CustomerWebsite`'s configuration that currently keep it from working end-to-end.
- The BFF orchestrates synchronous calls to multiple services for composite operations (e.g., catalog = Product + Inventory merged in `GetCatalogQueryHandler`).
- Dependency injection is configured in `Program.cs` with the three named HTTP clients above; the cart/wishlist handlers don't go through that configuration.

Its pattern demonstrates how to apply CQRS at the edge without full domain complexity — with the caveat that one whole feature area of it doesn't follow the pattern the rest does.

---

## AppHost — .NET Aspire Orchestrator (new)

`src/AppHost` is a new addition: a .NET Aspire orchestrator, added as an alternative to running each service with `dotnet run` or via Docker Compose.

```csharp
var productService = builder.AddProject<Projects.ProductService>("productservice", launchProfileName: "http");
var inventoryService = builder.AddProject<Projects.InventoryService>("inventoryservice", launchProfileName: "http");
var orderService = builder.AddProject<Projects.OrderService>("orderservice", launchProfileName: "http");

builder.AddProject<Projects.GatewayBff>("gatewaybff", launchProfileName: "http")
    .WaitFor(productService).WaitFor(inventoryService).WaitFor(orderService)
    .WithReference(productService).WithReference(inventoryService).WithReference(orderService);
```

Notes:
- Targets `net10.0` — one version ahead of every other service (`net9.0`), because that's what the current Aspire AppHost SDK requires. This is intentional, not an oversight to fix.
- Does **not** manage Postgres, Redis, or Kafka — those still come from `docker compose -f docker/docker-compose.yml up -d postgres redis kafka`.
- Only orchestrates ProductService, InventoryService, OrderService, and GatewayBff. PaymentService, NotificationService, UserService, CustomerService, and ChatbotService still need to be started separately if you need them.
- Launched via the `AppHost (Aspire)` configuration in `.vscode/launch.json`, which runs the `build AppHost` task from `.vscode/tasks.json` first.

---

## Shared Library

The `Shared` project holds cross-service contracts such as Kafka events (`OrderCreatedEvent`, `OrderItemEvent`). It has no dependencies other than .NET base libraries so it can be referenced by any service. Note that `OrderId`/`ProductId` travel as `string` inside the event even though `Order.Id` is an `int` (DB identity) and `ProductId` is a `Guid` in Product/Inventory/OrderItem — the event contract normalizes both to strings rather than mirroring each service's native id type.

---

## Philosophical Rationale

- **Separation of Concerns**: Each architectural style isolates concerns; DDD isolates rich behavior, Clean Architecture isolates business policies from delivery mechanisms.
- **Dependency Rule**: Higher-level policies are independent; low-level details (ORM, caching) depend on abstractions.
- **Scalability & Maintainability**: CQRS allows read-models to evolve separately from write-models, simplifying scaling and optimization.
- **Explicitness**: Value Objects, domain events, and explicit command/query names make business intent clear.
- **Testability**: With dependencies inverted and logic living in small focused units, unit testing is straightforward — though as of today there are no test projects anywhere in the solution to exercise that testability.

---

## Using & Extending the Architecture

1. **Add a new use case in OrderService**:
   - Modify `Order` aggregate or add new domain types if needed.
   - Update Application Service or add a new one if orchestration changes.
   - Adjust controllers if new endpoints are required.
   - Add/modify EF migrations as required.

2. **Add a new feature in ProductService**:
   - Define a new Command or Query in `Application/Commands` or `Queries`.
   - Implement a handler and, if necessary, a validator.
   - Use existing domain interfaces; create new ones in `Domain/Interfaces` only if new infrastructure behavior is needed.
   - Add endpoint to controller by sending the command/query.

3. **Introduce caching or messaging**:
   - Changes occur in `Application/Common/Interfaces` and `Infrastructure` implementations (ProductService), or the equivalent `Cache`/`Messaging` folders in the simpler services.
   - The rest of the code interacts via interfaces, so no widespread changes — except in GatewayBff's cart/wishlist handlers, which would need each hardcoded URL updated individually rather than a single config change.

4. **Implement PaymentService**:
   - There's nothing to refactor — start from `docs/PAYMENT_AND_NOTIFICATION_SERVICES_DESIGN.md`'s design and build the Controllers/Models/Services/Data folders from scratch, following whichever of the patterns above fits (InventoryService's simple layering is the closest existing template for a Kafka-consuming service).

5. **Testing strategy**:
   - Domain layer: unit-test aggregates and value objects without any infrastructure.
   - Application layer: test handlers by mocking repository/cache interfaces.
   - Controllers: thin; use integration tests hitting HTTP endpoints, or test through MediatR with an in-memory pipeline.
   - None of this exists yet — there are no test projects in the solution today.

---

## Summary

This repository showcases multiple architectural patterns coexisting, at varying levels of completeness:
- DDD for a complex domain (`OrderService`),
- Clean Architecture with CQRS for a read-heavy service (`ProductService`),
- Simple layered design where the domain is genuinely simple (`InventoryService`, `CustomerService`),
- Framework-heavy services where the complexity is in a third-party integration, not custom layers (`UserService`, `NotificationService`, `ChatbotService`),
- A CQRS-based API gateway that is internally inconsistent in one feature area (`GatewayBff`'s cart/wishlist path), and
- One unimplemented stub (`PaymentService`).

Each part of the codebase has a clear responsibility and a defined navigation path, but "clear responsibility" doesn't mean "uniformly built" — read the per-service sections above before assuming a pattern applies system-wide. Feel free to use this document as the starting point for onboarding or design discussions, alongside `../COMPREHENSIVE_SOLUTION_ARCHITECTURE_EN.md` at the repository root for the full picture including ports, infrastructure, and both frontends.
