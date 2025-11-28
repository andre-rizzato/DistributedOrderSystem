# Architettura Completa Sistema Distribuito Ordini

## Indice
1. [Panoramica Sistema](#panoramica-sistema)
2. [Architettura High-Level](#architettura-high-level)
3. [Servizi Implementati](#servizi-implementati)
4. [Pattern Architetturali](#pattern-architetturali)
5. [Flussi di Business](#flussi-di-business)
6. [Infrastruttura](#infrastruttura)
7. [Sicurezza](#sicurezza)
8. [Scalabilità](#scalabilità)
9. [Deployment](#deployment)

---

## Panoramica Sistema

### Cos'è questo Sistema?
Un'applicazione distribuita per la gestione ordini e-commerce costruita con architettura microservizi.

### Caratteristiche Principali
- 🏗️ **Microservices Architecture**: Servizi indipendenti, scalabili separatamente
- ⚡ **Event-Driven**: Comunicazione asincrona tramite Kafka
- 🚪 **BFF Pattern**: Backend-for-Frontend per aggregazione dati
- 💾 **Caching Strategy**: Redis per performance ottimali
- 📊 **CQRS Light**: Separazione letture (cache) e scritture (DB)
- 🐳 **Containerized**: Docker Compose per orchestrazione
- 🔄 **Asynchronous Processing**: Decoupling tramite message broker

### Tecnologie Stack

**Backend**:
- .NET 9 (C#)
- ASP.NET Core Web API
- Entity Framework Core 9
- MediatR (CQRS/Command pattern)

**Database**:
- SQL Server 2022 (relazionale)
- Redis (cache in-memory)

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

## Architettura High-Level

### Diagramma Architettura

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
| Servizio | Porta | Protocollo |
|----------|-------|------------|
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

## Servizi Implementati

### 1. GatewayBff (Backend for Frontend)

**Responsabilità**:
- Single entry point per frontend
- Aggregazione chiamate multiple microservizi
- Composizione response per UI
- Routing comandi e query

**Pattern Implementati**:
- **BFF Pattern**: API ottimizzate per bisogni UI
- **CQRS**: Separazione Commands (MediatR) e Queries
- **Facade Pattern**: Nasconde complessità backend

**API Endpoints**:

#### Commands (POST)
```
POST /api/commands/orders
  → Crea ordine
  → Chiama: OrderService + Pubblica Kafka event

POST /api/commands/inventory/adjust
  → Aggiusta inventario
  → Chiama: InventoryService

POST /api/commands/inventory/set
  → Imposta inventario
  → Chiama: InventoryService
```

#### Queries (GET)
```
GET /api/queries/catalog
  → Aggregazione Product + Inventory
  → Ritorna: [{product: {...}, inventory: {...}}]
  → Chiama: ProductService + InventoryService
  → Composizione in memoria

GET /api/queries/catalog/{id}
  → Singolo prodotto con inventario
  → Chiama: ProductService + InventoryService
```

**Aggregazione Dati Esempio**:
```csharp
// Frontend fa UNA sola richiesta
GET /api/queries/catalog

// BFF fa DUE chiamate backend
1. GET ProductService/api/products → [{id:1, name:"..."}]
2. GET InventoryService/api/inventory/{id} (per ogni prodotto)

// BFF compone response unificata
[
  {
    product: {id:1, name:"Product A", price:100},
    inventory: {availableQuantity: 50}
  }
]
```

**Vantaggi BFF**:
- ✅ Frontend fa 1 chiamata invece di N
- ✅ Riduce latency (chiamate parallele backend)
- ✅ Adatta response a bisogni UI
- ✅ Nasconde cambiamenti backend

**Tecnologie**:
- .NET 9 Minimal API
- MediatR per Commands
- HttpClient per chiamate microservizi

### 2. ProductService

**Responsabilità**:
- Gestione catalogo prodotti
- CRUD operazioni prodotti
- Caching prodotti popolari

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

**Pattern Implementati**:
- **Repository Pattern**: ProductRepository
- **Service Layer**: ProductWorkerService
- **Cache-Aside**: Redis caching
- **Dependency Injection**: IoC container

**Flusso Lettura con Cache**:
```
1. Client → GET /api/products/1
2. Controller → ProductWorkerService.GetProductByIdAsync(1)
3. Service → Check Redis (dev:product:1)
4a. CACHE HIT → Return cached (5ms)
4b. CACHE MISS → Query DB → Populate cache → Return (50ms)
```

**API Endpoints**:
```
GET    /api/products          → Lista tutti prodotti
GET    /api/products/{id}     → Singolo prodotto
POST   /api/products          → Crea prodotto
PUT    /api/products/{id}     → Aggiorna prodotto
DELETE /api/products/{id}     → Elimina prodotto (soft delete)
POST   /api/products/seed     → Seed database (dev/test)
```

**Cache Strategy**:
- **Pattern**: Cache-Aside (Lazy Loading)
- **TTL**: 10 minuti
- **Key Pattern**: `dev:product:{id}`
- **Eviction**: LRU (Least Recently Used)
- **Invalidation**: Manuale su update/delete

### 3. InventoryService

**Responsabilità**:
- Gestione stock prodotti
- Aggiornamenti inventario
- Consumo eventi Kafka (ordini)

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

**Pattern Implementati**:
- **Service Layer**: InventoryWorkerService
- **Cache-Aside**: Redis caching
- **Consumer Pattern**: Kafka OrderCreatedConsumer
- **Event-Driven**: Aggiornamenti asincroni

**API Endpoints**:
```
GET  /api/inventory/{productId}  → Stock per prodotto
POST /api/inventory/adjust       → Aggiusta stock (+/-)
POST /api/inventory/seed         → Seed database (dev/test)
```

**Kafka Consumer**:
```csharp
// Background Service in ascolto continuo
public class OrderCreatedConsumer : BackgroundService
{
    // Sottoscritto a: "order-created" topic
    // Consumer Group: "inventory-service"
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = consumer.Consume(stoppingToken);
            var orderEvent = Deserialize(message.Value);
            
            foreach (var item in orderEvent.Items)
            {
                // Riduce stock per ogni item ordinato
                await AdjustInventoryQuantityAsync(
                    item.ProductId, 
                    -item.Quantity  // Negativo = riduzione
                );
            }
            
            consumer.Commit(message);  // Ack message
        }
    }
}
```

**Guarantees**:
- **At-Least-Once Delivery**: Ogni ordine elaborato almeno una volta
- **Ordering**: Ordini stessa partizione elaborati in sequenza
- **Idempotency**: Stesso messaggio riprocessabile senza duplicati

### 4. OrderService

**Responsabilità**:
- Gestione ordini clienti
- Persistenza ordini e items
- Pubblicazione eventi Kafka

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

**Pattern Implementati**:
- **Repository Pattern**: OrderRepository
- **Service Layer**: OrderWorkerService
- **Producer Pattern**: Kafka OrderEventProducer
- **Aggregate Pattern**: Order + OrderItems come aggregate root

**API Endpoints**:
```
GET  /api/orders           → Lista ordini
GET  /api/orders/{id}      → Dettaglio ordine con items
POST /api/orders           → Crea nuovo ordine
```

**Flusso Creazione Ordine**:
```csharp
public async Task<Order> CreateOrderAsync(CreateOrderDto dto)
{
    // 1. Crea entità Order
    var order = new Order {
        Status = "Pending",
        TotalAmount = CalculateTotal(dto.Items),
        CreatedAtUtc = DateTime.UtcNow
    };
    
    // 2. Aggiungi OrderItems
    foreach (var item in dto.Items) {
        order.OrderItems.Add(new OrderItem {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            PriceAtOrder = item.Price
        });
    }
    
    // 3. Salva nel database
    await _repository.AddAsync(order);
    await _repository.SaveChangesAsync();
    
    // 4. Pubblica evento Kafka (asincrono, non-blocking)
    try {
        await _eventProducer.PublishOrderCreatedAsync(order);
    }
    catch (Exception ex) {
        // Log error ma non fallire ordine
        _logger.LogError(ex, "Failed to publish Kafka event");
    }
    
    // 5. Ritorna ordine creato
    return order;
}
```

**Kafka Producer**:
```csharp
// Configurazione Producer
var config = new ProducerConfig {
    BootstrapServers = "localhost:9092",
    Acks = Acks.Leader,              // Wait leader ack
    EnableIdempotence = true,         // No duplicati
    MaxInFlight = 5                   // Pipeline requests
};

// Pubblicazione Messaggio
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
- ✅ Ordine salvato anche se Kafka down
- ✅ Inventario aggiornato asincronamente
- ✅ Frontend riceve response immediata
- ✅ Failure isolation tra servizi

---

## Pattern Architetturali

### 1. Microservices Architecture

**Caratteristiche**:
- Servizi piccoli, indipendenti, focalizzati
- Database per servizio (No shared DB)
- Comunicazione via API HTTP + Events
- Deploy indipendente
- Scalabilità indipendente

**Vantaggi**:
- ✅ **Scalability**: Scala solo servizi sotto carico
- ✅ **Resilience**: Failure isolation
- ✅ **Technology Diversity**: Stack diversi per servizio
- ✅ **Team Autonomy**: Team possiedono servizi

**Svantaggi**:
- ❌ **Complexity**: Orchestrazione, monitoring
- ❌ **Data Consistency**: Eventual consistency
- ❌ **Network Latency**: Chiamate inter-servizio

### 2. Backend for Frontend (BFF)

**Problema Risolto**:
- Frontend deve chiamare N servizi
- Network roundtrips multipli
- Response non ottimizzate per UI
- Logica aggregazione in frontend

**Soluzione BFF**:
```
SENZA BFF:
Frontend → ProductService (50ms)
Frontend → InventoryService (50ms)
Frontend → OrderService (50ms)
= 150ms + overhead frontend

CON BFF:
Frontend → BFF (10ms)
  BFF → ProductService (50ms) ┐
  BFF → InventoryService (50ms) } Parallelo
  BFF → OrderService (50ms)    ┘
= 10ms + 50ms = 60ms
```

**Benefici**:
- ⚡ Riduce latency (chiamate parallele)
- 📉 Riduce carico rete frontend
- 🎯 API tailored per UI
- 🔒 Centralizza autenticazione
- 🛡️ Nasconde backend complexity

### 3. Event-Driven Architecture

**Implementazione**: Kafka Message Broker

**Caratteristiche**:
- **Asincrono**: Producer non aspetta consumer
- **Decoupled**: Servizi non conoscono dipendenze
- **Scalabile**: Più consumer per topic
- **Durable**: Messaggi persistiti su disco

**Flusso Eventi**:
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

**Vantaggi**:
- 🚀 **Performance**: Non-blocking operations
- 🔌 **Loose Coupling**: Servizi indipendenti
- 📈 **Scalability**: Horizontal scaling consumers
- 🔄 **Resilience**: Retry automatico su failure
- 📊 **Audit Trail**: Tutti eventi loggati

**Use Cases**:
- Aggiornamento inventario dopo ordine ✅
- Invio notifiche email/SMS (futuro)
- Aggiornamento analytics (futuro)
- Sincronizzazione warehouse multipli (futuro)

### 4. CQRS (Command Query Responsibility Segregation)

**Implementazione**: GatewayBff con MediatR

**Separazione**:
```csharp
// COMMANDS (Scrittura) - Via MediatR
public class CreateOrderCommand : IRequest<OrderDto>
{
    public List<OrderItemDto> Items { get; set; }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // Valida, crea ordine, pubblica evento
        var order = await _orderService.CreateOrderAsync(request);
        await _kafkaProducer.PublishAsync(order);
        return MapToDto(order);
    }
}

// QUERIES (Lettura) - Dirette
public async Task<CatalogResponse> GetCatalogAsync()
{
    // Legge da cache se possibile
    var products = await _productService.GetAllAsync();
    var inventories = await _inventoryService.GetBulkAsync(productIds);
    return Combine(products, inventories);
}
```

**Benefici**:
- 📝 **Commands**: Validazione, business logic, side effects
- 📖 **Queries**: Ottimizzate per lettura, cache-friendly
- 🎯 **Separation of Concerns**: Code più pulito
- ⚡ **Performance**: Query ottimizzate diversamente
- 🔒 **Security**: Permessi granulari

### 5. Cache-Aside Pattern

**Implementazione**: Redis

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

**Caratteristiche**:
- **Lazy Loading**: Popola cache solo quando richiesto
- **TTL**: Scadenza automatica (10 minuti)
- **Cache Invalidation**: Manuale su update/delete
- **Fallback**: Se Redis down, legge da DB

**Benefici**:
- ⚡ 100x più veloce del database
- 📉 Riduce carico database 80-90%
- 🎯 Cache solo dati hot (frequenti)
- 💰 Costo-efficiente (memoria < CPU DB)

---

## Flussi di Business Completi

### Scenario 1: Visualizzazione Catalogo

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
      │                 │ (Ripete per ogni prodotto - parallelo)  │
      │                 │                   │                      │
T=50ms│                 │ Aggregate data    │                      │
      │                 │ Compose response  │                      │
      │                 │                   │                      │
T=60ms│ [{catalog}]     │                   │                      │
      │<────────────────┤                   │                      │
      │                 │                   │                      │
```

**Performance**:
- Frontend → BFF: 1 chiamata HTTP
- BFF → Backend: 1 + N chiamate (parallele)
- Total Time: ~60ms (con cache)
- Without Cache: ~200ms
- Without BFF: ~500ms (seriali da frontend)

### Scenario 2: Creazione Ordine con Aggiornamento Inventario

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
      │   ✅ ORDINE CREATO           │            │             │
      │   Frontend mostra conferma    │            │             │
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
      │   ✅ INVENTARIO AGGIORNATO (asincrono)    │             │
```

**Timeline**:
- `T=0-130ms`: Creazione ordine (sincrono)
- `T=130ms`: Frontend riceve conferma ✅
- `T=200-350ms`: Aggiornamento inventario (asincrono)
- Total User Wait: 130ms (solo ordine)
- Total Processing: 350ms (include inventory)

**Caratteristiche**:
- ⚡ **Non-Blocking**: Frontend non aspetta inventory
- 🔌 **Decoupled**: OrderService e InventoryService indipendenti
- 🔄 **Resilient**: Se Inventory down, ordine comunque salvato
- 📊 **Eventual Consistency**: Inventory aggiornato entro ~200ms

---

## Infrastruttura

### Docker Compose Setup

**File**: `docker/docker-compose.yml`

```yaml
version: '3.8'

services:
  # Database SQL Server
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong_Password123
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

  # Cache Redis
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    command: redis-server --appendonly yes

  # Message Broker Kafka (KRaft mode - no Zookeeper)
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

**Avvio Infrastruttura**:
```bash
cd docker
docker-compose up -d

# Verifica status
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

**Attualmente**: Hard-coded URLs in appsettings.json

```json
{
  "ServiceUrls": {
    "ProductService": "http://localhost:5198",
    "InventoryService": "http://localhost:5051",
    "OrderService": "http://localhost:5003"
  }
}
```

**Futuro (Production)**:
- **Consul**: Service registry
- **Kubernetes**: Service discovery built-in
- **Azure Service Fabric**: Naming service

---

## Sicurezza

### Implementazioni Attuali

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
- ✅ No raw SQL con string concatenation

**Input Validation**:
- ✅ Data Annotations su DTOs
- ✅ ModelState validation in controllers

### Miglioramenti Necessari (Production)

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

## Scalabilità

### Horizontal Scaling Strategy

#### Per Servizio:

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
- RAM: Tutti dati in memoria
- CPU: Serialization/deserialization
- Network: Throughput

**Microservices**:
- CPU: Business logic, serialization
- RAM: Request handling, caching
- Network: HTTP calls

### Bottlenecks Potenziali

1. **Database Writes** (OrderService, InventoryService)
   - Soluzione: Connection pooling, batch writes
   - Futuro: Write replicas, sharding

2. **Redis Memory** (ProductService, InventoryService)
   - Soluzione: LRU eviction, aumenta RAM
   - Futuro: Redis Cluster

3. **Kafka Consumer Lag** (InventoryService)
   - Soluzione: Più partitions, più consumer
   - Monitoring: Kafka lag metrics

4. **Network Latency** (BFF ↔ Microservizi)
   - Soluzione: Service mesh, gRPC
   - Futuro: Deploy in stessa region/AZ

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
# 1. Avvia infrastruttura
cd docker
docker-compose up -d

# 2. Avvia backend services
cd src/ProductService && dotnet run &
cd src/InventoryService && dotnet run &
cd src/OrderService && dotnet run &
cd src/GatewayBff && dotnet run &

# 3. Avvia frontend
cd frontend/distributed-order-app
npm install
npm start

# 4. Accedi
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
# (ripeti per altri servizi)

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

## Conclusione

Questo sistema implementa un'architettura moderna e scalabile con:

✅ **Microservices**: Servizi indipendenti, piccoli, focalizzati  
✅ **Event-Driven**: Kafka per comunicazione asincrona  
✅ **BFF Pattern**: Aggregazione dati ottimizzata per UI  
✅ **Caching Strategy**: Redis per performance ottimali  
✅ **CQRS Light**: Separazione commands/queries  
✅ **Docker**: Containerization per deploy consistente  
✅ **Scalability**: Horizontal scaling ready  
✅ **Resilience**: Failure isolation tra servizi  

**Pronto per**:
- ✅ Development locale
- ✅ Integration testing
- ⚠️ Production (con security enhancements)

**Prossimi Passi**:
1. Implementare PaymentService e NotificationService
2. Aggiungere authentication/authorization
3. Implementare monitoring (Prometheus + Grafana)
4. Setup CI/CD pipeline
5. Deploy su cloud (Azure/AWS)
