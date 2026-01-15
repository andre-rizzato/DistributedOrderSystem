# Quick Testing Guide - Kafka Integration

## Prerequisites
```bash
# Start Docker infrastructure (SQL Server, Redis, Kafka in KRaft mode)
cd docker
docker-compose up -d

# Verify Kafka is running
docker ps | grep kafka
docker logs dos_kafka | tail -20

# Note: Kafka now runs in KRaft mode (no Zookeeper needed)
```

## Start All Services

### Terminal 1: OrderService (Port 5003)
```bash
cd src/OrderService
dotnet run
```
**Watch for**: "Kafka producer initialized for topic order-created"

### Terminal 2: InventoryService (Port 5051)
```bash
cd src/InventoryService
dotnet run
```
**Watch for**: 
- "Kafka consumer initialized for topic order-created"
- "Subscribed to Kafka topic: order-created"

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

### 1. Create Products (via Frontend or API)
```bash
# Via API
curl -X POST http://localhost:5189/api/commands/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Product",
    "description": "Test Description",
    "price": 29.99
  }'
```

### 2. Initialize Inventory
```bash
curl -X POST http://localhost:5051/api/inventory/seed \
  -H "Content-Type: application/json" \
  -d '[
    {"productId": 1, "quantity": 100},
    {"productId": 2, "quantity": 50},
    {"productId": 3, "quantity": 75}
  ]'
```

### 3. Check Initial Inventory
```bash
# Check product 1 inventory
curl http://localhost:5051/api/inventory/1

# Expected response:
# {"productId":1,"availableQuantity":100,"reservedQuantity":0}
```

### 4. Create Order (via Frontend)
1. Open browser: http://localhost:4200
2. Navigate to "Create Order"
3. Add products to cart (e.g., Product 1, Quantity: 5)
4. Click "Crea Ordine"

**OR via API**:
```bash
curl -X POST http://localhost:5189/api/commands/orders \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {"productId": 1, "quantity": 5}
    ]
  }'
```

### 5. Verify Kafka Event Published

**OrderService logs should show**:
```
Published OrderCreated event for Order 1 to partition 0 at offset 0
```

### 6. Verify Inventory Updated

**InventoryService logs should show**:
```
Received message from partition 0 at offset 0
Processing OrderCreatedEvent for Order 1 with 1 items
Reduced inventory for Product 1 by 5 units (Order 1)
Successfully processed and committed message at offset 0
```

**Check inventory**:
```bash
curl http://localhost:5051/api/inventory/1

# Expected response:
# {"productId":1,"availableQuantity":95,"reservedQuantity":0}
# (100 - 5 = 95)
```

### 7. View Order
```bash
# Get all orders
curl http://localhost:5189/api/queries/orders

# Get specific order
curl http://localhost:5189/api/queries/orders/1
```

## Kafka Debugging Commands

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

### Check Consumer Group Status
```bash
docker exec -it dos_kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --describe \
  --group inventory-service
```
**Look for**: LAG column (should be 0 if consumer is caught up)

### View Consumer Group Offsets
```bash
docker exec -it dos_kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group inventory-service \
  --describe \
  --offsets
```

## Expected Log Output

### OrderService (when order created)
```
info: OrderService.Controllers.OrderCommandsController[0]
      Order 1 created with 1 items
info: OrderService.Messaging.OrderEventProducer[0]
      Published OrderCreated event for Order 1 to partition 0 at offset 0
```

### InventoryService (when consuming event)
```
info: InventoryService.Messaging.OrderCreatedConsumer[0]
      Received message from partition 0 at offset 0
info: InventoryService.Messaging.OrderCreatedConsumer[0]
      Processing OrderCreatedEvent for Order 1 with 1 items
info: InventoryService.Messaging.OrderCreatedConsumer[0]
      Reduced inventory for Product 1 by 5 units (Order 1)
info: InventoryService.Messaging.OrderCreatedConsumer[0]
      Completed inventory updates for Order 1
info: InventoryService.Messaging.OrderCreatedConsumer[0]
      Successfully processed and committed message at offset 0
```

## Troubleshooting

### Problem: Consumer not receiving messages
```bash
# Check if consumer is subscribed
docker logs dos_inventoryservice | grep "Subscribed"

# Check consumer group
docker exec -it dos_kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --list

# Check if topic exists
docker exec -it dos_kafka kafka-topics \
  --bootstrap-server localhost:9092 \
  --list
```

### Problem: Inventory not updating
```bash
# Check InventoryService logs
docker logs dos_inventoryservice | grep "OrderCreatedEvent"

# Check if products exist
curl http://localhost:5051/api/inventory/1

# Verify database
docker exec -it dos_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P YourStrong_Password123 -C \
  -Q "SELECT * FROM InventoryDb.dbo.InventoryItems"
```

### Problem: Kafka not starting
```bash
# Check if Kafka container is running
docker ps | grep kafka

# View Kafka logs for errors
docker logs dos_kafka

# Restart Kafka (KRaft mode)
cd docker
docker-compose restart kafka

# If issues persist, recreate Kafka
docker-compose down kafka
docker-compose up -d kafka
```

## Test Scenarios

### Scenario 1: Single Order, Single Item
1. Inventory: Product 1 = 100
2. Create order: Product 1, Qty 5
3. Expected: Inventory = 95

### Scenario 2: Single Order, Multiple Items
1. Inventory: Product 1 = 100, Product 2 = 50
2. Create order: Product 1 Qty 5, Product 2 Qty 10
3. Expected: Product 1 = 95, Product 2 = 40

### Scenario 3: Multiple Orders
1. Inventory: Product 1 = 100
2. Create order 1: Product 1, Qty 5
3. Create order 2: Product 1, Qty 3
4. Expected: Inventory = 92 (100 - 5 - 3)

### Scenario 4: Insufficient Inventory (should still create order)
1. Inventory: Product 1 = 2
2. Create order: Product 1, Qty 10
3. Expected: Order created, but inventory warning in logs
4. Inventory remains at 2 (no negative values)

### Scenario 5: Kafka Failure Recovery
1. Stop Kafka: `docker stop dos_kafka`
2. Create order (should succeed, event publishing fails)
3. Start Kafka: `docker start dos_kafka`
4. Create another order (should publish event)
5. Check inventory updates

## Clean Up

### Stop Services
```bash
# Press Ctrl+C in each terminal running dotnet services

# Stop frontend
# Press Ctrl+C in terminal running npm start
```

### Stop Docker
```bash
cd docker
docker-compose down
```

### Reset Data (if needed)
```bash
# Remove volumes (deletes all data)
docker-compose down -v

# Restart fresh
docker-compose up -d
```

## Performance Monitoring

### View Kafka Metrics
```bash
# Topic statistics
docker exec -it dos_kafka kafka-run-class kafka.tools.GetOffsetShell \
  --broker-list localhost:9092 \
  --topic order-created

# Consumer lag
docker exec -it dos_kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --describe \
  --group inventory-service
```

### Monitor Service Performance
```bash
# Watch OrderService logs
tail -f src/OrderService/bin/Debug/net9.0/logs/orderservice.log

# Watch InventoryService logs
tail -f src/InventoryService/bin/Debug/net9.0/logs/inventoryservice.log
```

## Success Criteria

✅ OrderService logs show "Published OrderCreated event"  
✅ InventoryService logs show "Processing OrderCreatedEvent"  
✅ InventoryService logs show "Reduced inventory for Product"  
✅ Inventory API returns updated quantities  
✅ Consumer group lag is 0  
✅ No errors in any service logs  
✅ Frontend shows updated inventory after order creation  

## Notes

- First order might take longer (topic creation)
- Consumer processes messages in order within partition
- If consumer restarts, it resumes from last committed offset
- Event publishing is non-blocking (order creation succeeds even if Kafka is down)
- Inventory updates are idempotent (can safely retry)
