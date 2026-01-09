# Complete Distributed Order System Architecture

## Table of Contents
1. [System Overview](#system-overview)
2. [High-Level Architecture](#high-level-architecture)
3. [Implemented Services](#implemented-services)
4. [Architectural Patterns](#architectural-patterns)
5. [Business Flows](#business-flows)
6. [Infrastructure](#infrastructure)
7. [Security](#security)
8. [Scalability](#scalability)
9. [Deployment](#deployment)

---

## System Overview

### What is this System?
A distributed application for e-commerce order management built with microservices architecture.

### Key Features
- 🏗️ **Microservices Architecture**: Independent, separately scalable services
- ⚡ **Event-Driven**: Asynchronous communication via Kafka
- 🚪 **BFF Pattern**: Backend-for-Frontend for data aggregation
- 💾 **Caching Strategy**: Redis for optimal performance
- 📊 **CQRS Light**: Separation of reads (cache) and writes (DB)
- 🐳 **Containerized**: Docker Compose for orchestration
- 🔄 **Asynchronous Processing**: Decoupling via message broker

### Technology Stack

**Backend**:
- .NET 9 (C#)
- ASP.NET Core Web API
- Entity Framework Core 9
- MediatR (CQRS/Command pattern)

**Database**:
- SQL Server 2022 (relational)
- Redis (in-memory cache)

**Messaging**:
- Apache Kafka 3.x (message broker in KRaft mode)

**Frontend**:
- Angular 19
- Standalone Components
- Signals API
- TypeScript

**Infrastructure**:
- Docker & Docker Compose
- Linux containers

---

## High-Level Architecture

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         FRONTEND LAYER                          │
│                                                                 │
│   ┌───────────────────────────────────────────────────────┐   │
│   │  Angular 19 SPA (Port: 4200)                          │   │
│   │  - Shopping Cart                                       │   │
│   │  - Product Catalog                                     │   │
│   │  - Order Management                                    │   │
│   │  - Signals + Reactive Forms                           │   │
│   └───────────────────┬───────────────────────────────────┘   │
│                       │ HTTP/REST                              │
└───────────────────────┼─────────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────────┐
│                    API GATEWAY / BFF LAYER                      │
│                                                                 │
│   ┌───────────────────────────────────────────────────────┐   │
│   │  GatewayBff (Port: 5189)                              │   │
│   │  - Request Aggregation                                │   │
│   │  - Commands (MediatR)                                 │   │
│   │  - Queries (Direct)                                   │   │
│   │  - Response Composition                               │   │
│   └───┬───────────────┬───────────────┬───────────────────┘   │
└───────┼───────────────┼───────────────┼───────────────────────────┘
        │               │               │
        │               │               │
┌───────▼───────────────▼───────────────▼───────────────────────────┐
│                   MICROSERVICES LAYER                             │
│                                                                   │
│  ┌────────────────┐  ┌────────────────┐  ┌─────────────────┐   │
│  │ ProductService │  │InventoryService│  │  OrderService   │   │
│  │   Port: 5198   │  │   Port: 5051   │  │   Port: 5003    │   │
│  │                │  │                │  │                 │   │
│  │ - CRUD API     │  │ - Stock Mgmt   │  │ - Order CRUD    │   │
│  │ - Redis Cache  │  │ - Redis Cache  │  │ - Kafka Pub     │   │
│  │ - SQL Server   │  │ - Kafka Sub    │  │ - SQL Server    │   │
│  └────────┬───────┘  └────────┬───────┘  └────────┬────────┘   │
│           │                   │                    │            │
└───────────┼───────────────────┼────────────────────┼────────────┘
            │                   │                    │
            ▼                   ▼                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                      DATA LAYER                                 │
│                                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐         │
│  │  ProductDb   │  │ InventoryDb  │  │   OrderDb    │         │
│  │ SQL Server   │  │ SQL Server   │  │  SQL Server  │         │
│  │ Port: 1433   │  │ Port: 1433   │  │  Port: 1433  │         │
│  └──────────────┘  └──────────────┘  └──────────────┘         │
│                                                                 │
│  ┌──────────────────────────────────────────────────────┐     │
│  │              Redis Cache (Port: 6379)                │     │
│  │  DB 0: Products | DB 1: Inventory                    │     │
│  └──────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────┘
            │                   │                    │
            └───────────────────┼────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                   MESSAGING LAYER                               │
│                                                                 │
│  ┌──────────────────────────────────────────────────────┐     │
│  │    Apache Kafka (Port: 9092) - KRaft Mode           │     │
│  │    Topic: order-created                              │     │
│  │                                                      │     │
│  │    Producer: OrderService                            │     │
│  │    Consumer: InventoryService                        │     │
│  │                                                      │     │
│  │    Controller: Port 9093 (Internal)                 │     │
│  │    Metadata: Raft consensus (no Zookeeper)          │     │
│  └──────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────┘
```

### Ports Summary
| Service | Port | Protocol |
|---------|------|----------|
| Frontend (Angular) | 4200 | HTTP |
| GatewayBff | 5189 | HTTP/REST |
| ProductService | 5198 | HTTP/REST |
| InventoryService | 5051 | HTTP/REST |
| OrderService | 5003 | HTTP/REST |
| SQL Server | 1433 | TDS |
| Redis | 6379 | RESP |
| Kafka | 9092 | Kafka Protocol |
| Kafka Controller | 9093 | Raft (Internal) |

---

## Implemented Services

### 1. GatewayBff (Backend for Frontend)

**Responsibilities**:
- Single entry point for frontend
- Aggregation of multiple microservice calls
- Response composition for UI
- Routing commands and queries

**Implemented Patterns**:
- **BFF Pattern**: APIs optimized for UI needs
- **CQRS**: Separation of Commands (MediatR) and Queries
- **Facade Pattern**: Hides backend complexity

**API Endpoints**:

#### Commands (POST)
```
POST /api/commands/orders
  → Create order
  → Calls: OrderService + Publishes Kafka event

POST /api/commands/inventory/adjust
  → Adjust inventory
  → Calls: InventoryService

POST /api/commands/inventory/set
  → Set inventory
  → Calls: InventoryService
```

#### Queries (GET)
```
GET /api/queries/catalog
  → Aggregation of Product + Inventory
  → Returns: [{product: {...}, inventory: {...}}]
  → Calls: ProductService + InventoryService
  → In-memory composition

GET /api/queries/catalog/{id}
  → Single product with inventory
  → Calls: ProductService + InventoryService
```

**Data Aggregation Example**:
```csharp
// Frontend makes ONE single request
GET /api/queries/catalog

// BFF makes TWO backend calls
1. GET ProductService/api/products → [{id:1, name:"..."}]
2. GET InventoryService/api/inventory/{id} (for each product)

// BFF composes unified response
[
  {
    product: {id:1, name:"Product A", price:100},
    inventory: {availableQuantity: 50}
  }
]
```

**BFF Benefits**:
- ✅ Frontend makes 1 call instead of N
- ✅ Reduces latency (parallel backend calls)
- ✅ Adapts response to UI needs
- ✅ Hides backend changes

**Technologies**:
- .NET 9 Minimal API
- MediatR for Commands
- HttpClient for microservice calls

### 2. ProductService

**Responsibilities**:
- Product catalog management
- Product CRUD operations
- Caching of popular products

**Database**: `ProductDb`
```sql
CREATE TABLE Products (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(200) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(MAX),
    IsActive BIT NOT NULL DEFAULT 1
);
```

**Implemented Patterns**:
- **Repository Pattern**: ProductRepository
- **Service Layer**: ProductWorkerService
- **Cache-Aside**: Redis caching
- **Dependency Injection**: IoC container

**Read Flow with Cache**:
```
1. Client → GET /api/products/1
2. Controller → ProductWorkerService.GetProductByIdAsync(1)
3. Service → Check Redis (dev:product:1)
4a. CACHE HIT → Return cached (5ms)
4b. CACHE MISS → Query DB → Populate cache → Return (50ms)
```

**API Endpoints**:
```
GET    /api/products          → List all products
GET    /api/products/{id}     → Single product
POST   /api/products          → Create product
PUT    /api/products/{id}     → Update product
DELETE /api/products/{id}     → Delete product (soft delete)
POST   /api/products/seed     → Seed database (dev/test)
```

**Cache Strategy**:
- **Pattern**: Cache-Aside (Lazy Loading)
- **TTL**: 10 minutes
- **Key Pattern**: `dev:product:{id}`
- **Eviction**: LRU (Least Recently Used)
- **Invalidation**: Manual on update/delete

### 3. InventoryService

**Responsibilities**:
- Product stock management
- Inventory updates
- Kafka event consumption (orders)

**Database**: `InventoryDb`
```sql
CREATE TABLE InventoryItems (
    Id INT PRIMARY KEY IDENTITY,
    ProductId INT NOT NULL UNIQUE,
    AvailableQuantity INT NOT NULL CHECK (AvailableQuantity >= 0),
    ReservedQuantity INT NOT NULL DEFAULT 0,
    LastUpdatedUtc DATETIME2 NOT NULL
);

CREATE UNIQUE INDEX IX_ProductId ON InventoryItems(ProductId);
```

**Implemented Patterns**:
- **Service Layer**: InventoryWorkerService
- **Cache-Aside**: Redis caching
- **Consumer Pattern**: Kafka OrderCreatedConsumer
- **Event-Driven**: Asynchronous updates

**API Endpoints**:
```
GET  /api/inventory/{productId}  → Stock for product
POST /api/inventory/adjust       → Adjust stock (+/-)
POST /api/inventory/seed         → Seed database (dev/test)
```

**Kafka Consumer**:
```csharp
// Background Service continuously listening
public class OrderCreatedConsumer : BackgroundService
{
    // Subscribed to: "order-created" topic
    // Consumer Group: "inventory-service"
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = consumer.Consume(stoppingToken);
            var orderEvent = Deserialize(message.Value);
            
            foreach (var item in orderEvent.Items)
            {
                // Reduces stock for each ordered item
                await AdjustInventoryQuantityAsync(
                    item.ProductId, 
                    -item.Quantity  // Negative = reduction
                );
            }
            
            consumer.Commit(message);  // Ack message
        }
    }
}
```

**Guarantees**:
- **At-Least-Once Delivery**: Each order processed at least once
- **Ordering**: Orders from same partition processed in sequence
- **Idempotency**: Same message reprocessable without duplicates

### 4. OrderService

**Responsibilities**:
- Customer order management
- Order and items persistence
- Kafka event publishing

**Database**: `OrderDb`
```sql
CREATE TABLE Orders (
    Id INT PRIMARY KEY IDENTITY,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
    TotalAmount DECIMAL(18,2) NOT NULL,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE OrderItems (
    Id INT PRIMARY KEY IDENTITY,
    OrderId INT NOT NULL FOREIGN KEY REFERENCES Orders(Id),
    ProductId INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    PriceAtOrder DECIMAL(18,2) NOT NULL
);
```

**Implemented Patterns**:
- **Repository Pattern**: OrderRepository
- **Service Layer**: OrderWorkerService
- **Producer Pattern**: Kafka OrderEventProducer
- **Aggregate Pattern**: Order + OrderItems as aggregate root

**API Endpoints**:
```
GET  /api/orders           → List orders
GET  /api/orders/{id}      → Order detail with items
POST /api/orders           → Create new order
```

**Order Creation Flow**:
```csharp
public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
{
    // 1. Create Order entity
    var order = new Order {
        Status = "Pending",
        TotalAmount = CalculateTotal(dto.Items),
        CreatedAtUtc = DateTime.UtcNow
    };
    
    // 2. Add OrderItems
    foreach (var item in dto.Items) {
        order.OrderItems.Add(new OrderItem {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            PriceAtOrder = item.Price
        });
    }
    
    // 3. Save to database
    await _repository.AddAsync(order);
    await _repository.SaveChangesAsync();
    
    // 4. Publish Kafka event (asynchronous, non-blocking)
    try {
        await _eventProducer.PublishOrderCreatedAsync(order);
    }
    catch (Exception ex) {
        // Log error but don't fail order
        _logger.LogError(ex, "Failed to publish Kafka event");
    }
    
    // 5. Return created order
    return order;
}
```

**Kafka Producer**:
```csharp
// Producer Configuration
var config = new ProducerConfig {
    BootstrapServers = "localhost:9092",
    Acks = Acks.Leader,              // Wait leader ack
    EnableIdempotence = true,         // No duplicates
    MaxInFlight = 5                   // Pipeline requests
};

// Message Publishing
await _producer.ProduceAsync("order-created", new Message<string, string> {
    Key = $"order-{order.Id}",
    Value = JsonSerializer.Serialize(new OrderCreatedEvent {
        OrderId = order.Id,
        CreatedAt = order.CreatedAtUtc,
        Items = order.OrderItems.Select(i => new OrderItemEvent {
            ProductId = i.ProductId,
            Quantity = i.Quantity
        }).ToList()
    })
});
```

**Non-Blocking Design**:
- ✅ Order saved even if Kafka is down
- ✅ Inventory updated asynchronously
- ✅ Frontend receives immediate response
- ✅ Failure isolation between services

---

## Architectural Patterns

### 1. Microservices Architecture

**Characteristics**:
- Small, independent, focused services
- Database per service (No shared DB)
- Communication via HTTP APIs + Events
- Independent deployment
- Independent scalability

**Advantages**:
- ✅ **Scalability**: Scale only services under load
- ✅ **Resilience**: Failure isolation
- ✅ **Technology Diversity**: Different stacks per service
- ✅ **Team Autonomy**: Teams own services

**Disadvantages**:
- ❌ **Complexity**: Orchestration, monitoring
- ❌ **Data Consistency**: Eventual consistency
- ❌ **Network Latency**: Inter-service calls

### 2. Backend for Frontend (BFF)

**Problem Solved**:
- Frontend must call N services
- Multiple network roundtrips
- Responses not optimized for UI
- Aggregation logic in frontend

**BFF Solution**:
```
WITHOUT BFF:
Frontend → ProductService (50ms)
Frontend → InventoryService (50ms)
Frontend → OrderService (50ms)
= 150ms + frontend overhead

WITH BFF:
Frontend → BFF (10ms)
  BFF → ProductService (50ms) ┐
  BFF → InventoryService (50ms) } Parallel
  BFF → OrderService (50ms)    ┘
= 10ms + 50ms = 60ms
```

**Benefits**:
- ⚡ Reduces latency (parallel calls)
- 📉 Reduces frontend network load
- 🎯 APIs tailored for UI
- 🔒 Centralizes authentication
- 🛡️ Hides backend complexity

### 3. Event-Driven Architecture

**Implementation**: Kafka Message Broker

**Characteristics**:
- **Asynchronous**: Producer doesn't wait for consumer
- **Decoupled**: Services don't know dependencies
- **Scalable**: Multiple consumers per topic
- **Durable**: Messages persisted to disk

**Event Flow**:
```
OrderService (Producer)
    ↓
Publish OrderCreatedEvent
    ↓
Kafka Broker (Topic: order-created)
    ↓
InventoryService (Consumer Group)
    ↓
Process Event → Update Stock
    ↓
Commit Offset
```

**Advantages**:
- 🚀 **Performance**: Non-blocking operations
- 🔌 **Loose Coupling**: Independent services
- 📈 **Scalability**: Horizontal scaling consumers
- 🔄 **Resilience**: Automatic retry on failure
- 📊 **Audit Trail**: All events logged

**Use Cases**:
- Inventory update after order ✅
- Send email/SMS notifications (future)
- Update analytics (future)
- Synchronize multiple warehouses (future)

### 4. CQRS (Command Query Responsibility Segregation)

**Implementation**: GatewayBff with MediatR

**Separation**:
```csharp
// COMMANDS (Write) - Via MediatR
public class CreateOrderCommand : IRequest<OrderDto>
{
    public List<OrderItemDto> Items { get; set; }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // Validate, create order, publish event
        var order = await _orderService.CreateOrderAsync(request);
        await _kafkaProducer.PublishAsync(order);
        return MapToDto(order);
    }
}

// QUERIES (Read) - Direct
public async Task<CatalogResponse> GetCatalogAsync()
{
    // Read from cache if possible
    var products = await _productService.GetAllAsync();
    var inventories = await _inventoryService.GetBulkAsync(productIds);
    return Combine(products, inventories);
}
```

**Benefits**:
- 📝 **Commands**: Validation, business logic, side effects
- 📖 **Queries**: Optimized for reading, cache-friendly
- 🎯 **Separation of Concerns**: Cleaner code
- ⚡ **Performance**: Queries optimized differently
- 🔒 **Security**: Granular permissions

### 5. Cache-Aside Pattern

**Implementation**: Redis

**Flow**:
```
┌─────────┐          ┌─────────┐          ┌──────────┐
│ Client  │          │  App    │          │  Redis   │
└────┬────┘          └────┬────┘          └────┬─────┘
     │                    │                    │
     │ GET /products/1    │                    │
     ├───────────────────>│                    │
     │                    │ GET product:1      │
     │                    ├───────────────────>│
     │                    │                    │
     │                    │ CACHE MISS (null)  │
     │                    │<───────────────────┤
     │                    │                    │
     │                    │  ┌──────────┐      │
     │                    │  │ Database │      │
     │                    │  └─────┬────┘      │
     │                    │ SELECT * WHERE Id=1│
     │                    ├───────────────────>│
     │                    │                    │
     │                    │ {product data}     │
     │                    │<───────────────────┤
     │                    │                    │
     │                    │ SET product:1 EX 600
     │                    ├───────────────────>│
     │                    │                    │
     │ {product data}     │                    │
     │<───────────────────┤                    │
```

**Characteristics**:
- **Lazy Loading**: Populate cache only when requested
- **TTL**: Automatic expiration (10 minutes)
- **Cache Invalidation**: Manual on update/delete
- **Fallback**: If Redis down, read from DB

**Benefits**:
- ⚡ 100x faster than database
- 📉 Reduces database load 80-90%
- 🎯 Cache only hot data (frequently accessed)
- 💰 Cost-efficient (memory < DB CPU)

---

## Complete Business Flows

### Scenario 1: Catalog Viewing

```
┌──────────┐     ┌────────────┐     ┌────────────────┐     ┌──────────────┐
│ Frontend │     │ GatewayBff │     │ ProductService │     │InventoryService│
└─────┬────┘     └──────┬─────┘     └───────┬────────┘     └───────┬────────┘
      │                 │                   │                      │
T=0ms │ GET /catalog    │                   │                      │
      ├────────────────>│                   │                      │
      │                 │                   │                      │
T=10ms│                 │ GET /products     │                      │
      │                 ├──────────────────>│                      │
      │                 │                   │ Check Redis          │
      │                 │                   │ CACHE HIT            │
T=15ms│                 │ [{products}]      │                      │
      │                 │<──────────────────┤                      │
      │                 │                   │                      │
T=20ms│                 │ GET /inventory/1  │                      │
      │                 ├────────────────────────────────────────>│
      │                 │                   │                      │ Check Redis
      │                 │                   │                      │ CACHE HIT
T=25ms│                 │ {inventory}       │                      │
      │                 │<────────────────────────────────────────┤
      │                 │                   │                      │
      │                 │ (Repeats for each product - parallel)   │
      │                 │                   │                      │
T=50ms│                 │ Aggregate data    │                      │
      │                 │ Compose response  │                      │
      │                 │                   │                      │
T=60ms│ [{catalog}]     │                   │                      │
      │<────────────────┤                   │                      │
      │                 │                   │                      │
```

**Performance**:
- Frontend → BFF: 1 HTTP call
- BFF → Backend: 1 + N calls (parallel)
- Total Time: ~60ms (with cache)
- Without Cache: ~200ms
- Without BFF: ~500ms (serial from frontend)

### Scenario 2: Order Creation with Inventory Update

```
┌──────────┐  ┌────────────┐  ┌────────────┐  ┌───────┐  ┌─────────────┐
│ Frontend │  │ GatewayBff │  │OrderService│  │ Kafka │  │InventoryServ│
└─────┬────┘  └──────┬─────┘  └──────┬─────┘  └───┬───┘  └──────┬──────┘
      │              │                │            │             │
T=0ms │ POST /orders │                │            │             │
      │ {items:[...]}│                │            │             │
      ├─────────────>│                │            │             │
      │              │                │            │             │
T=10ms│              │ CreateOrderCmd │            │             │
      │              │ (MediatR)      │            │             │
      │              ├───────────────>│            │             │
      │              │                │            │             │
T=20ms│              │                │ 1. Validate│             │
      │              │                │ 2. Create  │             │
      │              │                │    Order   │             │
      │              │                │ 3. Save DB │             │
      │              │                │            │             │
T=100ms              │                │ DB INSERT  │             │
      │              │                │ SUCCESS    │             │
      │              │                │            │             │
T=110ms              │                │ Publish    │             │
      │              │                │ Event      │             │
      │              │                ├───────────>│             │
      │              │                │            │             │
T=120ms              │ OrderDto       │            │ Topic:      │
      │              │<───────────────┤            │ order-      │
      │              │                │            │ created     │
T=130ms {order}      │                │            │             │
      │<─────────────┤                │            │             │
      │              │                │            │             │
      │   ✅ ORDER CREATED            │            │             │
      │   Frontend shows confirmation │            │             │
      │              │                │            │             │
      │              │                │            │             │
T=200ms              │                │            │ Consumer    │
      │              │                │            │ Poll        │
      │              │                │            ├────────────>│
      │              │                │            │             │
T=250ms              │                │            │ Process     │
      │              │                │            │ Event       │
      │              │                │            │             │
T=300ms              │                │            │             │ Adjust
      │              │                │            │             │ Inventory
      │              │                │            │             │ (DB + Cache)
      │              │                │            │             │
T=350ms              │                │            │ Commit      │
      │              │                │            │ Offset      │
      │              │                │            │<────────────┤
      │              │                │            │             │
      │   ✅ INVENTORY UPDATED (asynchronous)     │             │
```

**Timeline**:
- `T=0-130ms`: Order creation (synchronous)
- `T=130ms`: Frontend receives confirmation ✅
- `T=200-350ms`: Inventory update (asynchronous)
- Total User Wait: 130ms (order only)
- Total Processing: 350ms (includes inventory)

**Characteristics**:
- ⚡ **Non-Blocking**: Frontend doesn't wait for inventory
- 🔌 **Decoupled**: OrderService and InventoryService independent
- 🔄 **Resilient**: If Inventory down, order still saved
- 📊 **Eventual Consistency**: Inventory updated within ~200ms

---

## Infrastructure

### Docker Compose Setup

**File**: `docker/docker-compose.yml`

```yaml
version: '3.8'

services:
  # SQL Server Database
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong_Password123
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

  # Redis Cache
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    command: redis-server --appendonly yes

  # Kafka Message Broker (KRaft mode - no Zookeeper)
  kafka:
    image: confluentinc/cp-kafka:7.4.0
    hostname: kafka
    ports:
      - "9092:9092"
    networks:
      - dos_network
    environment:
      # KRaft Configuration (Zookeeper-less)
      KAFKA_NODE_ID: 1
      KAFKA_PROCESS_ROLES: broker,controller
      KAFKA_LISTENERS: PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_CONTROLLER_LISTENER_NAMES: CONTROLLER
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT
      KAFKA_CONTROLLER_QUORUM_VOTERS: 1@kafka:9093
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS: 0
      KAFKA_AUTO_CREATE_TOPICS_ENABLE: "true"
      KAFKA_LOG_RETENTION_HOURS: 168
      CLUSTER_ID: MkU3OEVBNTcwNTJENDM2Qk

networks:
  dos_network:
    driver: bridge

volumes:
  sqlserver_data:
  redis_data:
```

**Infrastructure Startup**:
```bash
cd docker
docker-compose up -d

# Check status
docker-compose ps

# Logs
docker-compose logs -f kafka
```

### Network Architecture

```
┌───────────────────────────────────────────────────────┐
│                 Docker Bridge Network                 │
│                                                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐           │
│  │SQLServer │  │  Redis   │  │  Kafka   │           │
│  │  :1433   │  │  :6379   │  │  :9092   │           │
│  └──────────┘  └──────────┘  └──────────┘           │
│       ▲             ▲              ▲                  │
└───────┼─────────────┼──────────────┼──────────────────┘
        │             │              │
┌───────┼─────────────┼──────────────┼──────────────────┐
│  HOST │ MACHINE     │              │                  │
│       │             │              │                  │
│  ┌────┴────┐  ┌────┴────┐  ┌──────┴─────┐           │
│  │ Product │  │Inventory│  │   Order    │           │
│  │ Service │  │ Service │  │  Service   │           │
│  │  :5198  │  │  :5051  │  │   :5003    │           │
│  └────┬────┘  └────┬────┘  └──────┬─────┘           │
│       │             │              │                  │
│       └─────────────┴──────────────┘                 │
│                     │                                 │
│              ┌──────┴─────┐                          │
│              │ GatewayBff │                          │
│              │   :5189    │                          │
│              └──────┬─────┘                          │
│                     │                                 │
│              ┌──────┴─────┐                          │
│              │  Frontend  │                          │
│              │   :4200    │                          │
│              └────────────┘                          │
└───────────────────────────────────────────────────────┘
```

### Service Discovery

**Currently**: Hard-coded URLs in appsettings.json

```json
{
  "ServiceUrls": {
    "ProductService": "http://localhost:5198",
    "InventoryService": "http://localhost:5051",
    "OrderService": "http://localhost:5003"
  }
}
```

**Future (Production)**:
- **Consul**: Service registry
- **Kubernetes**: Built-in service discovery
- **Azure Service Fabric**: Naming service

---

## Security

### Current Implementations

**CORS Configuration**:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

**SQL Injection Protection**:
- ✅ EF Core parameterized queries
- ✅ No raw SQL with string concatenation

**Input Validation**:
- ✅ Data Annotations on DTOs
- ✅ ModelState validation in controllers

### Required Improvements (Production)

#### 1. Authentication & Authorization
```csharp
// JWT Bearer Token
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

// Role-based authorization
[Authorize(Roles = "Admin")]
public async Task<IActionResult> DeleteProduct(int id) { }
```

#### 2. API Rate Limiting
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

#### 3. Secrets Management
```csharp
// Azure Key Vault
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());

// Secrets in environment variables
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
```

#### 4. HTTPS Only
```csharp
app.UseHttpsRedirection();
app.UseHsts();
```

#### 5. Kafka Security
```yaml
# SASL/SSL Configuration
KAFKA_SECURITY_PROTOCOL: SASL_SSL
KAFKA_SASL_MECHANISM: PLAIN
KAFKA_SASL_USERNAME: ${KAFKA_USER}
KAFKA_SASL_PASSWORD: ${KAFKA_PASSWORD}
```

---

## Scalability

### Horizontal Scaling Strategy

#### Per Service:

**ProductService (Stateless)**:
```
Load Balancer
    ↓
[Instance 1] [Instance 2] [Instance 3]
    ↓             ↓             ↓
    └─────────────┴─────────────┘
              ↓
        Shared Redis
              ↓
      Shared SQL Server
```

**InventoryService (Kafka Consumer)**:
```
Kafka Topic: order-created (3 partitions)
    ↓
Consumer Group: inventory-service
    ├─ Partition 0 → Instance 1
    ├─ Partition 1 → Instance 2
    └─ Partition 2 → Instance 3

Automatic rebalancing on instance failure
```

### Vertical Scaling Considerations

**Database**:
- CPU: Query execution
- RAM: Query cache, buffer pool
- Disk: IOPS for writes

**Redis**:
- RAM: All data in memory
- CPU: Serialization/deserialization
- Network: Throughput

**Microservices**:
- CPU: Business logic, serialization
- RAM: Request handling, caching
- Network: HTTP calls

### Potential Bottlenecks

1. **Database Writes** (OrderService, InventoryService)
   - Solution: Connection pooling, batch writes
   - Future: Write replicas, sharding

2. **Redis Memory** (ProductService, InventoryService)
   - Solution: LRU eviction, increase RAM
   - Future: Redis Cluster

3. **Kafka Consumer Lag** (InventoryService)
   - Solution: More partitions, more consumers
   - Monitoring: Kafka lag metrics

4. **Network Latency** (BFF ↔ Microservices)
   - Solution: Service mesh, gRPC
   - Future: Deploy in same region/AZ

### Auto-Scaling Rules (Kubernetes)

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: product-service-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: product-service
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

---

## Deployment

### Local Development

```bash
# 1. Start infrastructure
cd docker
docker-compose up -d

# 2. Start backend services
cd src/ProductService && dotnet run &
cd src/InventoryService && dotnet run &
cd src/OrderService && dotnet run &
cd src/GatewayBff && dotnet run &

# 3. Start frontend
cd frontend/distributed-order-app
npm install
npm start

# 4. Access
http://localhost:4200
```

### Production Deployment (Azure)

#### Option 1: Azure Container Apps

```bash
# Build images
docker build -t productservice:latest ./src/ProductService
docker build -t inventoryservice:latest ./src/InventoryService
docker build -t orderservice:latest ./src/OrderService
docker build -t gatewaybff:latest ./src/GatewayBff

# Push to ACR
az acr login --name myregistry
docker tag productservice:latest myregistry.azurecr.io/productservice:latest
docker push myregistry.azurecr.io/productservice:latest
# (repeat for other services)

# Deploy Container Apps
az containerapp create \
  --name product-service \
  --resource-group mygroup \
  --environment myenv \
  --image myregistry.azurecr.io/productservice:latest \
  --target-port 8080 \
  --ingress external \
  --min-replicas 2 \
  --max-replicas 10
```

#### Option 2: Azure Kubernetes Service (AKS)

```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: product-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: product-service
  template:
    metadata:
      labels:
        app: product-service
    spec:
      containers:
      - name: product-service
        image: myregistry.azurecr.io/productservice:latest
        ports:
        - containerPort: 8080
        env:
        - name: ConnectionStrings__ProductDb
          valueFrom:
            secretKeyRef:
              name: db-secrets
              key: connection-string
        - name: Redis__ConnectionString
          valueFrom:
            configMapKeyRef:
              name: redis-config
              key: connection-string
---
apiVersion: v1
kind: Service
metadata:
  name: product-service
spec:
  selector:
    app: product-service
  ports:
  - port: 80
    targetPort: 8080
  type: LoadBalancer
```

#### Infrastructure as Code (Bicep)

```bicep
// Azure Services
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: 'mysqlserver'
  location: location
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: sqlPassword
  }
}

resource redisCache 'Microsoft.Cache/redis@2022-06-01' = {
  name: 'myredis'
  location: location
  properties: {
    sku: {
      name: 'Standard'
      family: 'C'
      capacity: 1
    }
  }
}

resource eventHub 'Microsoft.EventHub/namespaces@2021-11-01' = {
  name: 'myeventhub'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}
```

### CI/CD Pipeline (GitHub Actions)

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Build Docker images
      run: |
        docker build -t ${{ secrets.ACR_NAME }}.azurecr.io/productservice:${{ github.sha }} ./src/ProductService
        docker build -t ${{ secrets.ACR_NAME }}.azurecr.io/inventoryservice:${{ github.sha }} ./src/InventoryService
    
    - name: Push to ACR
      run: |
        az acr login --name ${{ secrets.ACR_NAME }}
        docker push ${{ secrets.ACR_NAME }}.azurecr.io/productservice:${{ github.sha }}
        docker push ${{ secrets.ACR_NAME }}.azurecr.io/inventoryservice:${{ github.sha }}
    
    - name: Deploy to AKS
      run: |
        az aks get-credentials --resource-group ${{ secrets.RESOURCE_GROUP }} --name ${{ secrets.AKS_CLUSTER }}
        kubectl set image deployment/product-service product-service=${{ secrets.ACR_NAME }}.azurecr.io/productservice:${{ github.sha }}
        kubectl set image deployment/inventory-service inventory-service=${{ secrets.ACR_NAME }}.azurecr.io/inventoryservice:${{ github.sha }}
```

---

## Conclusion

This system implements a modern and scalable architecture with:

✅ **Microservices**: Independent, small, focused services  
✅ **Event-Driven**: Kafka for asynchronous communication  
✅ **BFF Pattern**: Data aggregation optimized for UI  
✅ **Caching Strategy**: Redis for optimal performance  
✅ **CQRS Light**: Separation of commands/queries  
✅ **Docker**: Containerization for consistent deployment  
✅ **Scalability**: Horizontal scaling ready  
✅ **Resilience**: Failure isolation between services  

**Ready for**:
- ✅ Local development
- ✅ Integration testing
- ⚠️ Production (with security enhancements)

**Next Steps**:
1. Implement PaymentService and NotificationService
2. Add authentication/authorization
3. Implement monitoring (Prometheus + Grafana)
4. Setup CI/CD pipeline
5. Deploy to cloud (Azure/AWS)
