# Script to start all microservices and open their Swagger pages + frontend apps
Write-Host "Starting all microservices and opening browsers..." -ForegroundColor Green

# Array with services and their ports
$services = @(
    @{Name="ProductService"; Port=5198; Path="src/ProductService"; HasSwagger=$true},
    @{Name="GatewayBff"; Port=5189; Path="src/GatewayBff"; HasSwagger=$true},
    @{Name="OrderService"; Port=5003; Path="src/OrderService"; HasSwagger=$true},
    @{Name="InventoryService"; Port=5051; Path="src/InventoryService"; HasSwagger=$true},
    @{Name="PaymentService"; Port=5034; Path="src/PaymentService"; HasSwagger=$true},
    @{Name="NotificationService"; Port=5246; Path="src/NotificationService"; HasSwagger=$true},
    @{Name="ChatbotService"; Port=5055; Path="src/ChatbotService"; HasSwagger=$true}
)

# Frontend apps
$frontendApps = @(
    @{Name="Angular Frontend"; Port=4200; Path="frontend/distributed-order-app"; Command="npm start"; Url="http://localhost:4200"},
    @{Name="Customer Website"; Port=5100; Path="frontend/customer-facing e-commerce/CustomerWebsite"; Command="dotnet run"; Url="http://localhost:5100"}
)

# Function to check if a port is in use
function Test-Port {
    param($Port)
    $connection = Test-NetConnection -ComputerName localhost -Port $Port -WarningAction SilentlyContinue -InformationLevel Quiet
    return $connection
}

# Start each microservice
foreach ($service in $services) {
    Write-Host "`nChecking $($service.Name) on port $($service.Port)..." -ForegroundColor Cyan
    
    if (Test-Port -Port $service.Port) {
        Write-Host "  ✓ $($service.Name) is already running on port $($service.Port)" -ForegroundColor Yellow
    } else {
        Write-Host "  → Starting $($service.Name)..." -ForegroundColor White
        $fullPath = Join-Path $PSScriptRoot $service.Path
        
        Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd '$fullPath'; Write-Host 'Starting $($service.Name)...' -ForegroundColor Green; dotnet run" -WindowStyle Minimized
        
        Start-Sleep -Seconds 2
        Write-Host "  ✓ $($service.Name) started" -ForegroundColor Green
    }
}

# Start frontend apps
foreach ($app in $frontendApps) {
    Write-Host "`nChecking $($app.Name) on port $($app.Port)..." -ForegroundColor Cyan
    
    if (Test-Port -Port $app.Port) {
        Write-Host "  ✓ $($app.Name) is already running on port $($app.Port)" -ForegroundColor Yellow
    } else {
        Write-Host "  → Starting $($app.Name)..." -ForegroundColor White
        $fullPath = Join-Path $PSScriptRoot $app.Path
        
        Start-Process pwsh -ArgumentList "-NoExit", "-Command", "cd '$fullPath'; Write-Host 'Starting $($app.Name)...' -ForegroundColor Green; $($app.Command)" -WindowStyle Minimized
        
        Start-Sleep -Seconds 3
        Write-Host "  ✓ $($app.Name) started" -ForegroundColor Green
    }
}

Write-Host "`n============================================" -ForegroundColor Green
Write-Host "All services started! Opening browsers..." -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green

# Wait a bit for services to be ready
Write-Host "`nWaiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Open all Swagger pages
Write-Host "`nOpening Swagger pages..." -ForegroundColor Cyan
foreach ($service in $services) {
    if ($service.HasSwagger) {
        $url = "http://localhost:$($service.Port)/swagger"
        Write-Host "  → Opening $($service.Name): $url" -ForegroundColor White
        Start-Process $url
        Start-Sleep -Milliseconds 500
    }
}

# Open frontend apps
Write-Host "`nOpening frontend applications..." -ForegroundColor Cyan
foreach ($app in $frontendApps) {
    Write-Host "  → Opening $($app.Name): $($app.Url)" -ForegroundColor White
    Start-Process $app.Url
    Start-Sleep -Milliseconds 500
}

Write-Host "`n============================================" -ForegroundColor Green
Write-Host "All applications are running and opened!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green

Write-Host "`nMicroservices URLs:" -ForegroundColor Cyan
foreach ($service in $services) {
    Write-Host "  • $($service.Name): http://localhost:$($service.Port)/swagger" -ForegroundColor White
}

Write-Host "`nFrontend URLs:" -ForegroundColor Cyan
foreach ($app in $frontendApps) {
    Write-Host "  • $($app.Name): $($app.Url)" -ForegroundColor White
}

Write-Host "`nDocker Services:" -ForegroundColor Cyan
Write-Host "  • Kafka UI:  http://localhost:8080" -ForegroundColor White
Write-Host "  • Redis:     localhost:6379" -ForegroundColor White
Write-Host "  • SQL Server: localhost:1433" -ForegroundColor White

Write-Host "`n" -ForegroundColor Gray
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
