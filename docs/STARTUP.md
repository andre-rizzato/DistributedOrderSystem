# Distributed Order System - Startup Guide

## Prerequisites
- Docker and Docker Compose installed
- .NET 8 SDK installed
- Node.js and npm installed
- All services use the GatewayBff as mediator

## Quick Start

### Automated (Recommended)
```bash
./start-services.sh
```

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

Wait ~30 seconds for SQL Server to initialize.

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
| **GatewayBff** | http://localhost:5189 | API Gateway (Mediator) |
| ProductService | http://localhost:5198 | Product microservice |
| InventoryService | http://localhost:5051 | Inventory microservice |
| SQL Server | localhost:1433 | Database |
| Redis | localhost:6379 | Cache |

## API Endpoints (via GatewayBff)

### Queries (Read)
- `GET /api/queries/catalog` - Get all products with inventory
- `GET /api/queries/catalog/{id}` - Get product by ID with inventory

### Commands (Write)
- `POST /api/commands/products` - Create product
- `PUT /api/commands/products/{id}` - Update product
- `DELETE /api/commands/products/{id}` - Delete product
- `POST /api/commands/orders` - Create order

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
# Verify SQL Server is running
docker ps | grep sqlserver

# Test connection
docker exec -it dos_sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P YourStrong_Password123 -C -Q "SELECT 1"
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
│    GatewayBff      │ ← MEDIATOR/BFF Pattern
│   (Port 5189)      │    - Aggregates data
│                    │    - Routes commands
└────────┬───────────┘    - Single entry point
         │
         ├─────→ ProductService (Port 5198)
         │       └─→ SQL Server + Redis Cache
         │
         └─────→ InventoryService (Port 5051)
                 └─→ SQL Server + Redis Cache
```

## Testing the System

1. Open browser to http://localhost:4200
2. Create a new product
3. View the product list (shows inventory quantity)
4. Edit/Delete products
5. All operations go through GatewayBff!

## Development Tips

- Use Swagger UI for API testing:
  - GatewayBff: http://localhost:5189/swagger
  - ProductService: http://localhost:5198/swagger
  - InventoryService: http://localhost:5051/swagger

- Frontend uses CQRS pattern via BFF:
  - Queries → `/api/queries/*`
  - Commands → `/api/commands/*`

- Check browser console for frontend logs
- Check terminal output for backend logs
