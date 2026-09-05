# Distributed Order System - Startup Guide

> Updated to match the current stack: PostgreSQL (not SQL Server), .NET 9 (not .NET 8), and Scalar for interactive API docs (not Swagger UI) on the three services this guide covers. This guide only starts GatewayBff + ProductService + InventoryService + the Angular frontend — see the note at the end for the other five backend services and the new AppHost orchestrator, which this script doesn't touch.

## Prerequisites
- Docker and Docker Compose installed
- **.NET 9 SDK** installed (the new `AppHost` project, if you use it instead of this guide, needs .NET 10 SDK as well)
- Node.js and npm installed
- All services in this guide go through GatewayBff as the entry point for the Angular frontend

## Quick Start

### Automated
```bash
./start-services.sh
```
⚠️ This script's own echoed output still says "SQL Server" and links to `/swagger` — both are stale (see corrections below). The script itself still works for starting the three services + Docker infra + Angular; only its printed messages are outdated.

### Manual Steps

#### 1. Fix Docker (if needed)
If you see iptables errors:
```bash
sudo systemctl restart docker
# OR
docker network prune -f
sudo systemctl restart docker
```

#### 2. Start Infrastructure
```bash
cd docker
docker-compose up -d
```

Wait ~30 seconds for PostgreSQL to initialize (not SQL Server — this repo migrated to PostgreSQL; see `docker/docker-compose.yml`, service `postgres`, container `dos_postgres`).

#### 3. Start Backend Services

**Terminal 1 - GatewayBff:**
```bash
cd src/GatewayBff
dotnet run
```

**Terminal 2 - ProductService:**
```bash
cd src/ProductService
dotnet run
```

**Terminal 3 - InventoryService:**
```bash
cd src/InventoryService
dotnet run
```

These three services don't cover the whole backend — see [Other Services](#other-services-not-covered-by-this-guide) below.

#### 4. Start Frontend

**Terminal 4 - Angular:**
```bash
cd src/frontend/distributed-order-app
npm install  # First time only
npm start
```

## Service URLs

| Service | URL | Description |
|---------|-----|-------------|
| **Frontend** | http://localhost:4200 | Angular UI |
| **GatewayBff** | http://localhost:5189 | API Gateway (MediatR-based BFF) |
| ProductService | http://localhost:5198 | Product microservice |
| InventoryService | http://localhost:5051 | Inventory microservice |
| PostgreSQL | localhost:5432 | Database (container `dos_postgres`) |
| Redis | localhost:6379 | Cache |
| Kafka | localhost:29092 (host) / `kafka:9092` (containers) | Message broker — two listeners, see `docs/KAFKA_INTEGRATION.md` |

## API Endpoints (via GatewayBff)

### Queries (Read)
- `GET /api/queries/catalog` - Get all products with inventory
- `GET /api/queries/catalog/{id}` - Get product by ID with inventory
- `GET /api/queries/orders` - List orders
- `GET /api/queries/orders/{id}` - Get order by ID

### Commands (Write)
- `POST /api/commands/products` - Create product
- `PUT /api/commands/products/{id}` - Update product
- `DELETE /api/commands/products/{id}` - Delete product
- `POST /api/commands/orders` - Create order
- `POST /api/commands/inventory/adjust` - Adjust inventory by a delta
- `POST /api/commands/inventory/set` - Set inventory to an absolute quantity

Cart (`/api/cart/...`) and wishlist (`/api/wishlist/...`) also exist on GatewayBff but proxy to CustomerService (port 5009) with some caveats — see `docs/BFF_ARCHITECTURE.md`.

## Troubleshooting

### Docker Issues
```bash
# Check Docker status
sudo systemctl status docker

# View logs
docker-compose logs

# Clean restart
docker-compose down
docker network prune -f
sudo systemctl restart docker
docker-compose up -d
```

### Backend Service Issues
```bash
# Check if ports are in use
netstat -tulpn | grep -E '5189|5198|5051'

# Build services
dotnet build

# Check service logs in their respective terminals
```

### Frontend Issues
```bash
# Clear and reinstall
rm -rf node_modules package-lock.json
npm install
npm start

# Check if Angular CLI is installed
npm install -g @angular/cli
```

### Database Connection Issues
```bash
# Verify PostgreSQL is running (container is dos_postgres, not dos_sqlserver)
docker ps | grep dos_postgres

# Test connection
docker exec -it dos_postgres psql -U postgres -c "SELECT 1"
```

## Stopping Services

### Stop all Docker containers:
```bash
cd docker
docker-compose down
```

### Stop .NET services:
Press `Ctrl+C` in each terminal running a .NET service

### Stop Angular:
Press `Ctrl+C` in the terminal running npm start

## Architecture Flow

```
┌──────────────────┐
│  Angular Frontend│
│   (Port 4200)    │
└────────┬─────────┘
         │ All HTTP Requests
         ↓
┌────────────────────┐
│    GatewayBff      │ ← MediatR-based BFF
│   (Port 5189)      │    - Aggregates data
│                    │    - Routes commands
└────────┬───────────┘    - Single entry point
         │
         ├─────→ ProductService (Port 5198)
         │       └─→ PostgreSQL + Redis Cache
         │
         └─────→ InventoryService (Port 5051)
                 └─→ PostgreSQL + Redis Cache
                     (also consumes OrderCreatedEvent from Kafka)
```

## Testing the System

1. Open browser to http://localhost:4200
2. Create a new product
3. View the product list (shows inventory quantity)
4. Edit/Delete products
5. Create an order → GatewayBff calls OrderService, which publishes to Kafka; InventoryService picks up the event asynchronously and decrements stock (allow a second or two for this to show up)

## Development Tips

- Use **Scalar** for interactive API docs (this solution uses `Scalar.AspNetCore`, not Swagger UI, on these services):
  - GatewayBff: http://localhost:5189/scalar/v1
  - ProductService: http://localhost:5198/scalar/v1
  - InventoryService: http://localhost:5051/scalar/v1

- Frontend uses CQRS pattern via BFF:
  - Queries → `/api/queries/*`
  - Commands → `/api/commands/*`

- Check browser console for frontend logs
- Check terminal output for backend logs

## Other Services Not Covered by This Guide

This guide only starts GatewayBff, ProductService, InventoryService, and the Angular frontend. The solution also has:

- **PaymentService** (port 5034) — unimplemented stub, nothing to start meaningfully yet.
- **NotificationService** (port 5246) — `dotnet run --project src/NotificationService`; needs PostgreSQL + Redis running.
- **UserService** (port 5010) — `dotnet run --project src/UserService`; needs PostgreSQL running.
- **CustomerService** (port 5009) — `dotnet run --project src/CustomerService`; needs Redis running. Required if you want cart/wishlist to work through GatewayBff.
- **ChatbotService** (port 5055) — `dotnet run --project src/ChatbotService`; needs PostgreSQL + Redis running.
- **CustomerWebsite** (port 5100) — the second, independent storefront under `src/frontend/customer-facing e-commerce/CustomerWebsite`; see `docs/BFF_ARCHITECTURE.md` for its (currently misconfigured) service URLs before relying on it.
- **AppHost** (new, .NET Aspire) — an alternative to manually running GatewayBff/ProductService/InventoryService/OrderService in separate terminals: `dotnet run --project src/AppHost` (needs .NET 10 SDK), after starting infra via Docker Compose. Doesn't cover the other five services listed above either.

For the full picture of every service, see `../COMPREHENSIVE_SOLUTION_ARCHITECTURE_EN.md` at the repository root.
