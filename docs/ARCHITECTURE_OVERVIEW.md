# Distributed Order System – Detailed Architecture Guide

This document provides an **in-depth description** of the architecture across all microservices in the `DistributedOrderSystem` solution. It goes beyond the previous overview by detailing structure, reasoning, and practical navigation for each service and explaining why every layer behaves as it does.

---

## Overview

The solution contains multiple independent microservices plus a Backend-for-Frontend (BFF) gateway:

- **OrderService** – built using Domain-Driven Design (DDD)
- **ProductService** – implemented with Clean Architecture and full CQRS
- **InventoryService, PaymentService, NotificationService, UserService, CustomerService, ChatbotService, etc.** – remain N-tier for now but can be refactored later
- **GatewayBff** – an API gateway using Mediator (CQRS-light) to orchestrate calls to backend services
- **Shared** – a library with common DTOs/messages (Kafka events)

The architectural goal is to demonstrate multiple approaches in one codebase:
1. DDD for a complex domain with rich business rules (`OrderService`).
2. Clean Architecture with strict dependency inversion and CQRS for a service with high read/write separation (`ProductService`).
3. A simple layered service pattern for the remainder (existing code).
4. A BFF that offers well-structured commands/queries and uses HTTP clients.

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
  Migrations/          # EF Core migrations updated to new namespaces
  Program.cs           # DI container config
```

#### Key Decisions
- **Aggregate Root (`Order`)** ensures all business invariants are enforced before saving.
- **Value Objects** (`Money`, `OrderStatus`) encapsulate concepts and prevent invalid states.
- **Domain Events** collected on the aggregate and dispatched by the application service support eventual consistency (inventory update via Kafka).
- **Repository interface in Domain** decouples the domain from EF Core; infrastructure implements it.
- **Application Service** orchestrates domain operations and side-effects (publishing events).
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
- **Clean dependency direction**: Domains have no references to infrastructure. Interfaces point inward.
- **CQRS**: Commands mutate state; Queries return read-only DTOs. Handlers live in the Application layer.
- **MediatR Pipeline**: ValidationBehavior and LoggingBehavior apply to all requests automatically.
- **FluentValidation**: Validators are defined per command; the pipeline triggers them before handlers.
- **Caching**: `IProductCache` interface in Application; Redis/InMemory implementations in Infrastructure.
- **Presentation**: Controllers are extremely thin; every HTTP call becomes a Mediatr request.

### Navigation Tips
- Read `Application/Commands/CreateProduct/CreateProductCommandHandler.cs` to see write-side flow.
- Inspect the corresponding validator to understand business constraints.
- Query handlers in `Application/Queries` show filtering logic.
- Domain/entity `Product.cs` is a simple POCO; logic resides in handlers.
- Infrastructure `ProductDbContext` maps the domain entity without polluting the domain.

---

## Other Services (Inventory, Payment, etc.)

These remain simpler N-tier services with `Controllers/`, `Models/`, `Services/`, `Data/` folders. They can be refactored later as needed. Their current pattern is:

- **Controllers** handle HTTP and use simple `IService` interfaces.
- **Services** contain business logic and interact with EF Core.
- **Data** hosts the `DbContext` and mapping configuration.

They serve as examples of a standard layered architecture and are easier to convert when requirements evolve.

---

## GatewayBff

The Backend-for-Frontend sits in `src/GatewayBff`. It uses "+MediatR+" but keeps a much lighter CQRS structure.

- **Commands/** and **Queries/** folders define request/handler pairs.
- **Contracts/** contains DTOs used for HTTP calls to backend microservices.
- The BFF orchestrates synchronous calls to multiple services for composite operations (e.g., order creation).
- Dependency injection is configured in `Program.cs` with HTTP clients for each service.

Its pattern demonstrates how to apply CQRS at the edge without full domain complexity.

---

## Shared Library

The `Shared` project holds cross-service contracts such as Kafka events (`OrderCreatedEvent`). It has no dependencies other than .NET base libraries so it can be referenced by any service.

---

## Philosophical Rationale

- **Separation of Concerns**: Each architectural style isolates concerns; DDD isolates rich behavior, Clean Architecture isolates business policies from delivery mechanisms.
- **Dependency Rule**: Higher-level policies are independent; low-level details (ORM, caching) depend on abstractions.
- **Scalability & Maintainability**: CQRS allows read-models to evolve separately from write-models, simplifying scaling and optimization.
- **Explicitness**: Value Objects, domain events, and explicit command/query names make business intent clear.
- **Testability**: With dependencies inverted and logic living in small focused units, unit testing is straightforward.

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
   - Changes occur in `Application/Common/Interfaces` and `Infrastructure` implementations.
   - The rest of the code interacts via interfaces, so no widespread changes.

4. **Testing strategy**:
   - Domain layer: unit-test aggregates and value objects without any infrastructure.
   - Application layer: test handlers by mocking repository/cache interfaces.
   - Controllers: thin; use integration tests hitting HTTP endpoints, or test through Mediator with in-memory pipeline.

---

## Summary

This repository showcases multiple architectural patterns coexisting:
- DDD for complex domains,
- Clean Architecture with CQRS for read-heavy services,
- Simple layered design where appropriate, and
- A CQRS-based API gateway.

Each part of the codebase has a clear responsibility and a defined navigation path. The architecture encourages scalability, clarity, and clean dependency management, making the system easier to evolve over time.

Feel free to use this document as the starting point for onboarding or design discussions.