#!/bin/bash

# Script to fix Docker iptables issue
# Run with: sudo ./fix-docker.sh

echo "🔧 Fixing Docker iptables issue..."
echo ""

# Stop Docker
echo "1. Stopping Docker..."
systemctl stop docker

# Clean iptables
echo "2. Cleaning iptables rules..."
iptables -t filter -F DOCKER || true
iptables -t filter -F DOCKER-ISOLATION-STAGE-1 || true
iptables -t filter -F DOCKER-ISOLATION-STAGE-2 || true
iptables -t filter -X DOCKER-ISOLATION-STAGE-2 || true
iptables -t nat -F || true
iptables -t nat -X || true

# Recreate the missing chain
echo "3. Creating missing iptables chain..."
iptables -t filter -N DOCKER-ISOLATION-STAGE-2 || true

# Start Docker
echo "4. Starting Docker..."
systemctl start docker

# Wait a bit
sleep 3

# Check status
echo "5. Checking Docker status..."
systemctl status docker --no-pager -l

echo ""
echo "✅ Docker fix complete!"
echo ""
echo "Now you can run:"
echo "  cd docker && docker-compose up -d"
