# InventoryService - Complete Documentation

> Updated to reflect the real code. Previous versions of this document assumed `ProductId` was an `int` and SQL Server as the database — in the current code `ProductId` is a `Guid` (consistent with ProductService/OrderService) and the database is **PostgreSQL**. Some real behavioral discrepancies were also found (see the "⚠️" notes in the sections below) that were not present in previous versions.

## Overview
InventoryService is a microservice responsible for managing product inventory in the distributed system. It tracks available quantities, handles asynchronous updates via Kafka, and implements Redis for high-performance caching.

## Architecture

### Architectural Patterns Used
1. **Repository Pattern**: Not implemented (direct DbContext access for simplicity) — confirmed in the code: `InventoryWorkerService` injects `InventoryContext` directly
2. **Service Layer Pattern**: Business logic in `InventoryWorkerService` (the class is called `InventoryWorkerService`, singular, even though it lives in the file `InventoryWorkerServices.cs`)
3. **Event-Driven Architecture**: Kafka consumer for asynchronous updates
4. **Cache-Aside Pattern**: Redis to optimize frequent reads
5. **CQRS Light**: Separation of reads (cache) and writes (database)

### Project Structure
```
InventoryService/
├── Cache/                          # Redis cache implementation
│   ├── Interfaces/
│   │   └── IInventoryCache.cs     # Cache interface
│   └── RedisInventoryCache.cs     # Redis implementation
├── Configuration/                  # Configuration
│   ├── RedisSettings.cs           # Redis settings
│   └── KafkaSettings.cs           # Kafka settings
├── Controllers/                    # API Controllers
│   └── InventoryController.cs     # REST endpoints
├── Data/                          # Database Context
│   └── InventoryContext.cs        # EF Core DbContext (Npgsql)
├── Messaging/                      # Kafka Integration
│   └── OrderCreatedConsumer.cs    # Real Kafka consumer
├── Models/                         # Domain entities
│   └── InventoryItem.cs           # Inventory model
├── Services/                       # Business logic
│   ├── Interfaces/
│   │   └── IInventoryWorkerServices.cs
│   └── InventoryWorkerServices.cs
├── Program.cs                      # Configuration and startup
└── appsettings.json               # Application configuration
```

## Main Components

### 1. Models - InventoryItem Entity

#### InventoryItem.cs (real code)
```csharp
public class InventoryItem
{
    public int Id { get; set; }                     // Primary Key (identity, int)
    public Guid ProductId { get; set; } = Guid.Empty; // Foreign Key → ProductService (Guid, not int)
    public int AvailableQuantity { get; set; }       // Quantity available for sale
    public int ReservedQuantity { get; set; }        // Reserved quantity (never updated today — see Limitations)
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
```

**Fields Explained**:
- **Id**: auto-incrementing `int` primary key — internal to this service only
- **ProductId**: `Guid`, the same type used by ProductService and by `OrderItem.ProductId` in OrderService (no physical cross-service foreign key)
- **AvailableQuantity**: stock actually sellable
- **ReservedQuantity**: present in the model but **never written by any method** other than the initialization to `0` in `SetInventoryQuantityAsync` — there is no reservation logic yet (see [Limitations](#limitations--improvements))
- **LastUpdatedUtc**: updated on every `AdjustInventoryQuantityAsync`/`SetInventoryQuantityAsync`

**EF Core Mapping** (`InventoryContext.OnModelCreating`):
- Table: `Inventory` (not `InventoryItems` — the `DbSet` is called `InventoryItems` but `entity.ToTable("Inventory")` renames the physical table)
- Primary key on `Id`
- **Unique** index on `ProductId` (only one record per product)

### 2. Data Layer - Database Context

#### InventoryContext.cs (real code)
```csharp
public class InventoryContext : DbContext
{
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.ToTable("Inventory");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProductId).IsUnique();
            entity.Property(e => e.AvailableQuantity).IsRequired();
            entity.Property(e => e.ReservedQuantity).IsRequired();
            entity.Property(e => e.LastUpdatedUtc).IsRequired();
        });
    }
}
```

**Database**: PostgreSQL 16 (`Npgsql.EntityFrameworkCore.PostgreSQL`), not SQL Server
**Connection String** (`appsettings.Development.json`): `Host=localhost;Port=5432;Database=InventoryDb;Username=postgres;Password=YourStrong_Password123;` — when containerized via Docker Compose, the host becomes `postgres` instead of `localhost`.
`context.Database.EnsureCreated()` runs at startup in `Program.cs` — no EF Core migrations for this service.

### 3. Service Layer

#### IInventoryWorkerServices.cs
```csharp
public interface IInventoryWorkerService
{
    Task<InventoryItem?> GetInventoryByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task<bool> AdjustInventoryQuantityAsync(Guid productId, int delta, CancellationToken ct = default);
    Task<InventoryItem?> SetInventoryQuantityAsync(Guid productId, int quantity, CancellationToken ct = default);
}
```

#### InventoryWorkerServices.cs
**Responsibilities**:
- Inventory business logic management
- Coordination between database and cache
- Business rule validation
- Operation logging

##### 1. GetInventoryByProductIdAsync(productId)
**Purpose**: Retrieve inventory for a specific product

**Cache-Aside Flow** (real code):
```
1. Check Redis cache (key: see note below on RedisInventoryCache.Key)
2. If found → CACHE HIT
   - Log: "Inventory for product {productId} served from cache"
   - Return cached item
3. If not found → CACHE MISS
   - Query database with AsNoTracking() (read-only)
   - If found → Save to cache for next requests
   - Return item from DB
4. If it doesn't exist → Return null
```

**Optimizations**:
- `AsNoTracking()`: EF Core doesn't track entities (faster for read-only)
- Cache warming: populates cache after a DB read

##### 2. AdjustInventoryQuantityAsync(productId, delta)
**Purpose**: Modify inventory by a delta (+/- quantity)

**Parameters**:
- `productId`: product `Guid`
- `delta`: quantity to add (positive) or subtract (negative)

**Business Validation — real code**:
```csharp
var item = await _db.InventoryItems.FirstOrDefaultAsync(i => i.ProductId == productId, ct);
if (item == null)
{
    _logger.LogWarning("Inventory item for product {ProductId} not found.", productId);
    return false;
}

var newQuantity = item.AvailableQuantity + delta;
if (newQuantity <= 0)   // ⚠️ not "< 0": the check is "<= 0"
{
    _logger.LogWarning("Insufficient inventory for product {ProductId}. ...");
    return false;
}

item.AvailableQuantity = newQuantity;
item.LastUpdatedUtc = DateTime.UtcNow;
await _db.SaveChangesAsync(ct);
await _cache.SetInventoryItemAsync(item, ct);
return true;
```

⚠️ **Real behavior to be aware of**: the condition is `newQuantity <= 0`, not `< 0`. This means **an adjustment that would bring stock to exactly 0 is rejected** as "insufficient inventory" — it's not possible to sell the last available unit through this method. Whether this is intentional or an off-by-one isn't documented in the code; treat it as known behavior, not a bug to be "silently fixed" in this document.

**Use Cases**:
- **Stock replenishment**: `AdjustInventory(productId, delta: +100)`
- **Sale/Order**: `AdjustInventory(productId, delta: -5)`
- **Inventory correction**: `AdjustInventory(productId, delta: -2)`
- **Return**: `AdjustInventory(productId, delta: +1)`

**Called By**:
- GatewayBff `POST /api/commands/inventory/adjust` (manual, via named HTTP client `InventoryService`)
- Kafka Consumer `OrderCreatedConsumer` (automatic after an order)

##### 3. SetInventoryQuantityAsync(productId, quantity)
**Purpose**: Set an absolute quantity (not relative). If the item doesn't exist, creates it with `ReservedQuantity = 0`; if it exists, updates `AvailableQuantity` and `LastUpdatedUtc`. No minimum/maximum limit applied here (unlike `AdjustInventoryQuantityAsync`).

**Called By**:
- GatewayBff `POST /api/commands/inventory/set` (manual)
- `POST /api/inventory/seed` (bulk initialization)

### 4. Cache Layer - Redis

#### RedisSettings.cs (real code — note carefully what is actually bound)
```csharp
public class RedisSettings
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public string InventoryPrefix  { get; set; } = "Inventory:";
}
```

⚠️ `appsettings.Development.json` also defines `Prefix`, `Host`, `Port`, `Database`, and `TtlMinutes` under `"Redis"`, but **the `RedisSettings` class has no matching properties** for any of them — they are not bound and have no effect whatsoever:
- The **TTL is hardcoded to 5 minutes** in `RedisInventoryCache.SetInventoryItemAsync` (`TimeSpan.FromMinutes(5)`), not the 10 minutes stated in `appsettings`, and not configurable without changing the code.
- The **Redis database is always the default (db 0)** — `Program.cs` calls `connectionMultiplexer.GetDatabase()` with no index, so `"Database": 1` in config is never applied.

#### IInventoryCache.cs
```csharp
public interface IInventoryCache
{
    Task<InventoryItem?> GetInventoryItemByProductIdAsync(Guid productId, CancellationToken ct);
    Task SetInventoryItemAsync(InventoryItem item, CancellationToken ct);
    Task RemoveInventoryItemAsync(Guid productId, CancellationToken ct);
}
```
`RemoveInventoryItemAsync` exists in the interface and is implemented, but it is **never called** by `InventoryWorkerService` — the only form of invalidation today is natural TTL expiry after 5 minutes, not an explicit invalidation on write.

#### RedisInventoryCache.cs — real key
```csharp
private string Key(Guid productId) => $"{_settings.InventoryPrefix}:{productId}";
```
⚠️ With the default value (`"Inventory:"`, already ending in `:`) or the one configured in `appsettings.Development.json` (`"dev:inventory:item:"`, which also already ends in `:`), this produces a key with a **double colon**, e.g. `dev:inventory:item::3fa85f64-5717-4562-b3fc-2c963f66afa6`. This isn't a blocking error (Redis accepts the key as-is, and it's used consistently on both write and read), but it's a detail worth knowing if you inspect Redis manually (`KEYS dev:inventory:item:*` still works thanks to the prefix, but the exact key has the double `:`).

**Operations**:
1. `GetInventoryItemByProductIdAsync(productId)` → `GET <key>` → deserializes JSON
2. `SetInventoryItemAsync(item)` → `SET <key> "{json}" EX 300` (300s = 5 minutes, hardcoded)
3. `RemoveInventoryItemAsync(productId)` → `DEL <key>` (implemented, never invoked by the service layer)

### 5. Controller - REST API

#### InventoryController.cs (real routes)
**Base Route**: `/api/inventory`

##### 1. `GET /api/inventory/{productId:guid}`
```http
GET /api/inventory/3fa85f64-5717-4562-b3fc-2c963f66afa6 HTTP/1.1
Host: localhost:5051
```
**Response 200 OK**:
```json
{
  "id": 1,
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "availableQuantity": 95,
  "reservedQuantity": 0,
  "lastUpdatedUtc": "2026-09-04T10:30:00Z"
}
```
**Status Codes**: `200 OK` (found), `404 Not Found` (no inventory for the product).

##### 2. `POST /api/inventory/seed`
```http
POST /api/inventory/seed HTTP/1.1
Content-Type: application/json

[
  {"productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "quantity": 100},
  {"productId": "9c858901-8a57-4791-aeb7-1e5989a8f0c4", "quantity": 50}
]
```
Iterates the items and calls `SetInventoryQuantityAsync` for each one. No structured response body (just `200 OK`).

##### 3. `POST /api/inventory/adjust`
```http
POST /api/inventory/adjust HTTP/1.1
Content-Type: application/json

{ "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "delta": -5 }
```
**Response 200 OK** on success; **`400 Bad Request`** with body `"Not enough inventory or product not found"` if `AdjustInventoryQuantityAsync` returns `false` (item not found, or the result would be `<= 0` — see the note above).

### 6. Messaging - Kafka Consumer

#### KafkaSettings.cs (code defaults)
```csharp
public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string OrderCreatedTopic { get; set; } = "order-created";
    public string ConsumerGroupId { get; set; } = "inventory-service";
}
```
`appsettings.Development.json` overrides `BootstrapServers` to **`localhost:29092`** — Kafka exposes two listeners (`9092` container-to-container, `29092` host toward the dockerized broker); this service, if launched with `dotnet run` on the host against a Kafka running in Docker, must use `29092`. If containerized via Docker Compose, the environment variable `Kafka__BootstrapServers=kafka:9092` overrides it again toward the internal listener.

#### OrderCreatedConsumer.cs
**Type**: `BackgroundService`, manual offset commit (`EnableAutoCommit = false`, `EnableAutoOffsetStore = false`).

**Real message processing flow**:
```
1. Consumer receives a message from Kafka, logs partition/offset
2. Deserializes OrderCreatedEvent (JSON)
3. For each item in the order:
   a. Calls AdjustInventoryQuantityAsync(Guid.Parse(item.ProductId), -item.Quantity)
   b. If it returns true  → logs info "Reduced inventory..."
   c. If it returns false → logs warning "Unable to reduce inventory..." — BUT the loop CONTINUES
   d. If it throws an exception → logs error, then `throw;` (explicit rethrow)
4. If the foreach completes WITHOUT exceptions (even if some items were "false" at step 3c):
   - Commit offset + Store offset → the message is considered processed
5. If an exception was thrown at step 3d:
   - NO commit happens → the message (the entire batch consumed up to that point) will be reprocessed
   - 5-second delay before the next consumption attempt
```

⚠️ **Important point not present in previous versions of this document**: insufficient stock (`AdjustInventoryQuantityAsync` returning `false`) **does not prevent the offset commit**. Only a technical exception (e.g. the database being unreachable) blocks the commit and causes reprocessing. This means an order for which inventory turned out to be insufficient at the time of consumption **is still marked as "processed"** — there's no automatic retry for the "insufficient stock" business condition, only for technical failures.

**Guarantees**:
- ✅ **At-Least-Once Delivery** for technical failures (exceptions)
- ⚠️ **No retry** for business failures (insufficient stock) — the message is committed anyway
- ✅ **Ordering**: messages in the same partition processed in order
- ⚠️ **Idempotency**: there is no explicit idempotency check in the consumer — a reprocessing after a technical exception would reapply the same delta if the previous commit hadn't actually happened (expected behavior for an at-least-once consumer, but worth keeping in mind)

### 7. Configuration - Program.cs (real code)

```csharp
// Database — PostgreSQL, not SQL Server
builder.Services.AddDbContext<InventoryContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("InventoryDb")
             ?? "Host=localhost;Port=5432;Database=InventoryDb;Username=postgres;Password=YourStrong_Password123;";
    options.UseNpgsql(cs);
});

// Redis
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(/* ConnectionString */));
builder.Services.AddSingleton<IDatabase>(provider =>
    provider.GetRequiredService<IConnectionMultiplexer>().GetDatabase()); // always db 0

builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));

builder.Services.AddSingleton<IInventoryCache, RedisInventoryCache>();
builder.Services.AddScoped<IInventoryWorkerService, InventoryWorkerService>();
builder.Services.AddHostedService<OrderCreatedConsumer>();
```

**Service Lifetimes**:
- **Singleton**: `IConnectionMultiplexer`, `IDatabase`, `IInventoryCache`
- **Scoped**: `IInventoryWorkerService`, `InventoryContext`
- **HostedService**: `OrderCreatedConsumer` (continuous background)

**Real Startup Sequence**: build services → `app.Build()` → `context.Database.EnsureCreated()` in a dedicated scope → (Development only) Scalar/OpenAPI → `app.MapControllers()` → `app.Run()`. `UseHttpsRedirection()` is **disabled** in Development to allow direct HTTP calls between services.

## Configuration

### appsettings.Development.json (real content)
```json
{
  "ConnectionStrings": {
    "InventoryDb": "Host=localhost;Port=5432;Database=InventoryDb;Username=postgres;Password=YourStrong_Password123;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Prefix": "dev:inventory:",
    "InventoryPrefix": "dev:inventory:item:",
    "Host": "localhost",
    "Port": 6379,
    "Database": 1,
    "TtlMinutes": 10
  },
  "Kafka": {
    "BootstrapServers": "localhost:29092",
    "OrderCreatedTopic": "order-created",
    "ConsumerGroupId": "inventory-service"
  }
}
```
As explained above, only `Redis:ConnectionString` and `Redis:InventoryPrefix` have any real effect; `Prefix`, `Host`, `Port`, `Database`, `TtlMinutes` are present in the file but read by no configuration class.

## Business Flows

### Scenario 1: Order Creation → Inventory Update (success path)
```
T=0s  | User creates an order (5x product {productId})
      | Frontend → GatewayBff → OrderService
T=0.1s| OrderService: saves order, publishes OrderCreatedEvent to Kafka (topic order-created)
T=0.3s| InventoryService Consumer: receives message, logs partition/offset
T=0.5s| AdjustInventoryQuantityAsync(productId, -5) → true (sufficient stock)
      | AvailableQuantity: 100 → 95, LastUpdatedUtc updated
T=0.6s| Database (Postgres) updated; Redis SET <key> ... EX 300
T=0.7s| Kafka offset commit
DONE  | Stock updated; next GET /api/inventory/{productId} reflects it
```

### Scenario 2: Inventory Query with Cache
```
T=0s   | GET /api/inventory/{productId} — cache MISS
T=0.1s | Query PostgreSQL (AsNoTracking) → populates Redis cache (hardcoded 5-minute TTL)
T=0.2s | Return 200 OK
---
T=60s  | GET /api/inventory/{productId} — cache HIT, no DB query
```

## Performance & Scalability

The estimates below (hit ratio, response times) are indicative — no real metrics are collected/exposed today by this service (no `/metrics` endpoint, no Prometheus integration).

### Scalability Strategies

#### Horizontal Scaling
```
Load Balancer → [Instance 1] [Instance 2] [Instance 3]
                     ↓             ↓             ↓
              Shared Redis, shared PostgreSQL
                     ↓
        Kafka (Consumer Group: inventory-service, automatic rebalancing)
```

#### Potential Bottlenecks
1. **DB writes** during order spikes (only partially mitigated by the cache, which is for reads)
2. **Redis memory** if too many distinct products are cached at once
3. **Kafka consumer lag** if order throughput exceeds processing capacity

## Monitoring & Observability

### Key Logs to Monitor (real messages in the code)
```
✅ INFO:
- "Inventory for product {ProductId} served from cache"
- "Reduced inventory for Product {ProductId} by {Quantity} units (Order {OrderId})"
- "Message processed and successfully committed at offset {Offset}"

⚠️ WARNING:
- "Inventory item for product {ProductId} not found."
- "Insufficient inventory for product {ProductId}. Requested adjustment: {Delta}, Available: {AvailableQuantity}"
- "Unable to reduce inventory for Product {ProductId} ... - insufficient inventory or product not found"

❌ ERROR:
- "Error consuming message: {Error}"
- "Error updating inventory for Product {ProductId} (Order {OrderId})"
```

All log messages are in English (translated from Italian as of 2026-09-24).

There is no `/health` endpoint yet in this service (unlike, for example, NotificationService, which exposes one).

## Testing

### Manual Testing (with real Guids — replace with ids of products actually created in ProductService)

```bash
# 1. Seed initial inventory
curl -X POST http://localhost:5051/api/inventory/seed \
  -H "Content-Type: application/json" \
  -d '[
    {"productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "quantity": 100},
    {"productId": "9c858901-8a57-4791-aeb7-1e5989a8f0c4", "quantity": 50}
  ]'

# 2. Query inventory
curl http://localhost:5051/api/inventory/3fa85f64-5717-4562-b3fc-2c963f66afa6

# 3. Adjust inventory (remove 10 units)
curl -X POST http://localhost:5051/api/inventory/adjust \
  -H "Content-Type: application/json" \
  -d '{"productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "delta": -10}'

# 4. Test Kafka integration: create an order via GatewayBff
curl -X POST http://localhost:5189/api/commands/orders \
  -H "Content-Type: application/json" \
  -d '{"items": [{"productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "quantity": 5, "unitPrice": 9.99}]}'

# Wait a few seconds for asynchronous processing, then verify
curl http://localhost:5051/api/inventory/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

## Limitations & Improvements

### Current Limitations (verified in the code)
1. **`ReservedQuantity` is never updated** — initialized to `0` and never written again; there is no stock reservation logic during checkout yet.
2. **`AdjustInventoryQuantityAsync` refuses to bring stock to exactly 0** (`newQuantity <= 0`) — the last unit isn't sellable through this path.
3. **Insufficient stock during Kafka consumption doesn't trigger a retry** — the message is committed anyway (see the Messaging section above); only technical exceptions block the commit.
4. **Cache TTL hardcoded to 5 minutes**, not configurable from `appsettings` despite the file containing a `TtlMinutes` key that seems to suggest otherwise.
5. **`RemoveInventoryItemAsync` is never invoked** — no explicit cache invalidation on write, only natural expiry.
6. **No audit trail**: no history of inventory changes.
7. **No multi-warehouse**: a single virtual "warehouse."
8. **No automated tests** for this service.

### Proposed Improvements
The design proposals that follow (inventory reservation, low stock alerts, inventory history, multi-warehouse support) remain valid as future directions but **are not implemented** — none of them have corresponding code in the repository today. Before implementing them, it's worth consciously deciding whether the `<= 0` behavior of `AdjustInventoryQuantityAsync` is intentional, since a reservation logic would probably need to be based on a different comparison.

## Integration with Other Services

### 1. OrderService
- **Direction**: OrderService → Kafka (`order-created`) → InventoryService
- **Action**: stock reduction, with the commit caveats described above

### 2. GatewayBff
- **Direction**: GatewayBff ↔ InventoryService, via named HTTP client `"InventoryService"` configured from `ServiceUrls:InventoryService` (`http://localhost:5051` in `appsettings.Development.json`)
- **Endpoints used**: `GET /api/inventory/{productId}` (catalog aggregation in `GetCatalogQueryHandler`), `POST /api/inventory/adjust`, `POST /api/inventory/adjust`/`set` (manual commands)

### 3. ProductService
- **Relationship**: indirect via a shared `ProductId` (Guid) — no direct communication between the two services

### 4. NotificationService (not connected today)
- No "low stock" event is currently published by InventoryService toward NotificationService — this remains a future extension, not a real state.

## Conclusion

InventoryService implements an event-driven architecture with cache-aside, functional for the main use case (reducing stock when an order is created, serving reads from cache). However, it is not "production-ready" without further work: non-configurable TTL, no retry for insufficient stock during Kafka consumption, no audit trail, no automated tests, and a `<= 0` behavior in `AdjustInventoryQuantityAsync` that deserves an explicit decision before building a reservation logic on top of it.
