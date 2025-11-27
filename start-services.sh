#!/bin/bash

# Distributed Order System - Service Startup Script
# This script starts all services in the correct order

echo "🚀 Starting Distributed Order System..."
echo ""

# Get the workspace root directory
WORKSPACE_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$WORKSPACE_ROOT"

echo "📦 Step 1: Starting Docker infrastructure (SQL Server, Redis, Kafka)..."
cd docker
docker-compose up -d
if [ $? -ne 0 ]; then
    echo "❌ Failed to start Docker services. Please check Docker is running."
    exit 1
fi
echo "✅ Docker infrastructure started"
echo ""

# Wait for SQL Server to be ready
echo "⏳ Waiting for SQL Server to be ready (30 seconds)..."
sleep 30

echo "🔧 Step 2: Starting Backend Services..."
echo ""

# Start GatewayBff
echo "  → Starting GatewayBff on port 5189..."
cd "$WORKSPACE_ROOT/src/GatewayBff"
gnome-terminal --tab --title="GatewayBff" -- bash -c "dotnet run; exec bash" 2>/dev/null || \
xterm -T "GatewayBff" -e "dotnet run" 2>/dev/null || \
(dotnet run &)
sleep 3

# Start ProductService
echo "  → Starting ProductService on port 5198..."
cd "$WORKSPACE_ROOT/src/ProductService"
gnome-terminal --tab --title="ProductService" -- bash -c "dotnet run; exec bash" 2>/dev/null || \
xterm -T "ProductService" -e "dotnet run" 2>/dev/null || \
(dotnet run &)
sleep 3

# Start InventoryService
echo "  → Starting InventoryService on port 5051..."
cd "$WORKSPACE_ROOT/src/InventoryService"
gnome-terminal --tab --title="InventoryService" -- bash -c "dotnet run; exec bash" 2>/dev/null || \
xterm -T "InventoryService" -e "dotnet run" 2>/dev/null || \
(dotnet run &)
sleep 3

echo "✅ Backend services started"
echo ""

echo "🌐 Step 3: Starting Angular Frontend..."
cd "$WORKSPACE_ROOT/frontend/distributed-order-app"

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    echo "  → Installing npm dependencies..."
    npm install
fi

echo "  → Starting Angular dev server on port 4200..."
gnome-terminal --tab --title="Angular Frontend" -- bash -c "npm start; exec bash" 2>/dev/null || \
xterm -T "Angular Frontend" -e "npm start" 2>/dev/null || \
npm start &

echo ""
echo "✅ All services starting!"
echo ""
echo "🎉 Distributed Order System is running:"
echo "   • Frontend:         http://localhost:4200"
echo "   • GatewayBff:       http://localhost:5189/swagger"
echo "   • ProductService:   http://localhost:5198/swagger"
echo "   • InventoryService: http://localhost:5051/swagger"
echo ""
echo "📝 Note: Services may take a few seconds to be fully ready"
echo "🛑 To stop: Press Ctrl+C in each terminal or run: docker-compose down"
