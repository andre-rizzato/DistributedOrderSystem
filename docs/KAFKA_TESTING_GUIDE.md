# Quick Testing Guide - Kafka Integration

> Updated to match the real code. Product/order ids are `Guid`, not `int`; infrastructure is PostgreSQL, not SQL Server. Note: as of 2026-09-24 all log messages across the codebase were translated from Italian to English — see `KAFKA_INTEGRATION.md`'s Observability section for the verbatim strings; this guide's log excerpts reflect the current English text.

## Prerequisites
```bash
# Start Docker infrastructure (PostgreSQL, Redis, Kafka in KRaft mode, Kafka UI)
cd docker
docker-compose up -d

# Verify Kafka is running
docker ps | grep kafka
docker logs dos_kafka | tail -20

# Kafka runs in KRaft mode (no Zookeeper) with TWO listeners:
#   9092  → PLAINTEXT, container-to-container only (e.g. kafka:9092)
#   29092 → PLAINTEXT_HOST, for anything running on the host (dotnet run, AppHost)
# The commands in this guide that use `docker exec -it dos_kafka ...` run *inside*
# the container, so they correctly use `localhost:9092` — don't confuse that with
# the `localhost:29092` a host-side dotnet process needs.
```

## Start All Services

### Terminal 1: OrderService (Port 5003)
```bash
cd src/OrderService
dotnet run
```
**Watch for**: `Kafka producer initialized for topic order-created at localhost:29092`

### Terminal 2: InventoryService (Port 5051)
```bash
cd src/InventoryService
dotnet run
```
**Watch for**:
- `Kafka consumer initialized for topic order-created with group inventory-service at localhost:29092`
- `Starting Kafka consumer for topic: order-created` (logged once the consume loop starts, ~2s after startup)

### Terminal 3: GatewayBff (Port 5189)
```bash
cd src/GatewayBff
dotnet run
```

### Terminal 4: ProductService (Port 5198)
```bash
cd src/ProductService
dotnet run
```

### Terminal 5: Frontend (Port 4200)
```bash
cd src/frontend/distributed-order-app
npm start
```

## Test Sequence

### 1. Create a Product (via GatewayBff)
```bash
curl -X POST http://localhost:5189/api/commands/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Product",
    "description": "Test Description",
    "price": 29.99
  }'
```
Response includes the product's `id` (a `Guid`) — copy it, you'll need it for every step below. This guide uses `3fa85f64-5717-4562-b3fc-2c963f66afa6` as a placeholder; substitute your real id.

### 2. Initialize Inventory
```bash
curl -X POST http://localhost:5051/api/inventory/seed \
  -H "Content-Type: application/json" \
  -d '[
    {"productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "quantity": 100}
  ]'
```
⚠️ `productId` is a `Guid` string, not an integer — `InventoryController.Get`/`Seed`/`Adjust` all bind `Guid`, and the route itself is constrained (`{productId:guid}`).

### 3. Check Initial Inventory
```bash
curl http://localhost:5051/api/inventory/3fa85f64-5717-4562-b3fc-2c963f66afa6

# Expected response:
# {"id":1,"productId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","availableQuantity":100,"reservedQuantity":0,"lastUpdatedUtc":"..."}
```

### 4. Create Order (via Frontend or API)
1. Open browser: http://localhost:4200
2. Navigate to "Create Order", add the product, quantity 5, submit.

**OR via API** (GatewayBff looks up the product's real price itself before forwarding to OrderService, so `unitPrice` in this request is optional/ignored):
```bash
curl -X POST http://localhost:5189/api/commands/orders \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {"productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6", "quantity": 5}
    ]
  }'
```
GatewayBff's `CreateOrderCommandHandler` validates the product is active, checks `InventoryService` for sufficient stock *before* forwarding to `OrderService` — if inventory is insufficient at this stage, you get an error back from GatewayBff itself (`InvalidOperationException` → the request fails) rather than an order that later fails asynchronously. The Kafka-driven stock reduction described below only fires for orders that pass this upfront check.

### 5. Verify Kafka Event Published

**OrderService logs should show**:
```
Published OrderCreated event for Order 1 partition 0 offset 0
```

### 6. Verify Inventory Updated

**InventoryService logs should show**:
```
Received message from partition 0 at offset 0
Processing OrderCreatedEvent for Order 1 with 1 items
Reduced inventory for Product 3fa85f64-5717-4562-b3fc-2c963f66afa6 by 5 units (Order 1)
Completed inventory updates for Order 1
Message processed and successfully committed at offset 0
```

**Check inventory**:
```bash
curl http://localhost:5051/api/inventory/3fa85f64-5717-4562-b3fc-2c963f66afa6

# Expected: availableQuantity: 95 (100 - 5)
```

### 7. View Order
```bash
# All orders (via GatewayBff aggregation)
curl http://localhost:5189/api/queries/orders

# Specific order
curl http://localhost:5189/api/queries/orders/1

# Or directly against OrderService (no aggregation, same data)
curl http://localhost:5003/api/orders/1
```

## Kafka Debugging Commands

Run from the host, or use **kafka-ui at http://localhost:8080** for the same information (topics, partitions, consumer group lag) without the CLI. `kafka-ui` is already part of `docker-compose.yml` (container `dos_kafka_ui`), pointed at `kafka:9092` internally.

### View Kafka Topics
```bash
docker exec -it dos_kafka kafka-topics \
  --bootstrap-server localhost:9092 \
  --list
```

### View Messages in Topic
```bash
docker exec -it dos_kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic order-created \
  --from-beginning \
  --property print.key=true \
  --property print.timestamp=true
```
Message values are the JSON-serialized `OrderCreatedEvent` — `orderId` and each item's `productId` will appear as JSON **strings**, e.g. `{"orderId":"1","createdAt":"...","items":[{"productId":"3fa85f64-...","quantity":5}]}`.

### Check Consumer Group Status
```bash
docker exec -it dos_kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --describe \
  --group inventory-service
```
**Look for**: LAG column (should be 0 if the consumer is caught up).

## Troubleshooting

### Problem: Consumer not receiving messages
```bash
# Check the actual container name (this is Docker Compose, not "dos_inventoryservice")
docker ps --filter name=dos_inventory_service

docker logs dos_inventory_service | grep "Avvio consumer"

docker exec -it dos_kafka kafka-consumer-groups --bootstrap-server localhost:9092 --list
docker exec -it dos_kafka kafka-topics --bootstrap-server localhost:9092 --list
```

### Problem: Inventory not updating
```bash
docker logs dos_inventory_service | grep "OrderCreatedEvent"

curl http://localhost:5051/api/inventory/3fa85f64-5717-4562-b3fc-2c963f66afa6

# Verify in Postgres directly (not SQL Server / sqlcmd)
docker exec -it dos_postgres psql -U postgres -d InventoryDb -c 'SELECT * FROM "Inventory";'
```
Note the table name is `Inventory` (singular), not `InventoryItems` — the EF Core `DbSet` is named `InventoryItems` but `entity.ToTable("Inventory")` maps it to a differently-named physical table.

### Problem: Kafka not starting
```bash
docker ps | grep kafka
docker logs dos_kafka

cd docker
docker-compose restart kafka

# If issues persist, recreate Kafka
docker-compose down kafka
docker-compose up -d kafka
```

## Test Scenarios

### Scenario 1: Single Order, Single Item
1. Inventory: Product = 100
2. Create order: Qty 5
3. Expected: Inventory = 95

### Scenario 2: Multiple Orders
1. Inventory: Product = 100
2. Create order 1: Qty 5; create order 2: Qty 3
3. Expected: Inventory = 92

### Scenario 3: Insufficient Inventory
This scenario behaves differently depending on **where** the shortfall is caught:
- **Via GatewayBff** (`POST /api/commands/orders`): `CreateOrderCommandHandler` checks `InventoryService` for sufficient stock before creating the order at all. If insufficient, GatewayBff returns an error and **no order is created**, no Kafka event is published.
- **Via OrderService directly** (`POST /api/orders` bypassing GatewayBff, or an order that passed GatewayBff's check but the stock changed before the Kafka message was consumed): the order **is** created and the event **is** published; `InventoryService`'s consumer then finds `AdjustInventoryQuantityAsync` returns `false` (stock would go to `<= 0`), logs a warning, and **still commits the Kafka offset** — there is no retry for this condition. Inventory is left unchanged.

### Scenario 4: Kafka Failure Recovery
1. Stop Kafka: `docker stop dos_kafka`
2. Create an order directly against OrderService (`POST /api/orders`, bypassing GatewayBff's own inventory check) — it should succeed; the Kafka publish fails and is only logged (`OrderApplicationService.CreateOrderAsync` swallows the exception).
3. Start Kafka: `docker start dos_kafka`
4. Create another order — this one publishes normally.
5. Check inventory reflects only the second order's reduction (the first order's Kafka event was never sent, so its stock reduction never happened).

## Clean Up

### Stop Services
Press `Ctrl+C` in each terminal running `dotnet` or `npm start`.

### Stop Docker
```bash
cd docker
docker-compose down
```

### Reset Data
```bash
# Only `postgres_data` is a named volume in this compose file — Redis and Kafka
# have no persistent volume defined, so they reset on every `docker-compose down`
# regardless. Add -v to also wipe Postgres:
docker-compose down -v
docker-compose up -d
```

## Success Criteria

✅ OrderService logs show `Published OrderCreated event for Order ...`
✅ InventoryService logs show `Processing OrderCreatedEvent for Order ...` and `Reduced inventory for Product ...`
✅ Inventory API returns updated quantities
✅ Consumer group lag is 0 (`kafka-consumer-groups --describe --group inventory-service`, or kafka-ui)
✅ No `❌ ERROR` lines in either service's logs
✅ Frontend shows updated inventory after order creation

## Notes

- Product/order/consumer-group ids throughout this guide use `Guid`s for products, `int` for order ids — don't mix these up when adapting curl commands.
- Kafka event publishing is non-blocking: order creation succeeds even if Kafka is down (verified in `OrderApplicationService.CreateOrderAsync`).
- Insufficient-stock is **not** retried by the consumer — it's a committed, logged warning, not a redelivery trigger. Only a technical exception (DB down, etc.) triggers redelivery.
- All log messages are in English (translated from Italian as of 2026-09-24) — if you're grepping and finding nothing, double-check the exact phrasing against the tables above rather than assuming a broken consumer.
