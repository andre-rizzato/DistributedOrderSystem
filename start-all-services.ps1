# Script para iniciar todos os microserviços
Write-Host "Starting all microservices..." -ForegroundColor Green

# Array com os serviços e suas portas
$services = @(
    @{Name="ProductService"; Port=5198; Path="src/ProductService"},
    @{Name="GatewayBff"; Port=5189; Path="src/GatewayBff"},
    @{Name="OrderService"; Port=5003; Path="src/OrderService"},
    @{Name="InventoryService"; Port=5051; Path="src/InventoryService"},
    @{Name="PaymentService"; Port=5034; Path="src/PaymentService"},
    @{Name="NotificationService"; Port=5246; Path="src/NotificationService"},
    @{Name="ChatbotService"; Port=5055; Path="src/ChatbotService"}
)

# Função para verificar se uma porta está em uso
function Test-Port {
    param($Port)
    $connection = Test-NetConnection -ComputerName localhost -Port $Port -WarningAction SilentlyContinue -InformationLevel Quiet
    return $connection
}

# Iniciar cada serviço
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

Write-Host "`n============================================" -ForegroundColor Green
Write-Host "All services started!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host "`nService URLs:" -ForegroundColor Cyan
Write-Host "  • GatewayBff:         http://localhost:5189/swagger" -ForegroundColor White
Write-Host "  • ProductService:     http://localhost:5198/swagger" -ForegroundColor White
Write-Host "  • OrderService:       http://localhost:5003/swagger" -ForegroundColor White
Write-Host "  • InventoryService:   http://localhost:5051/swagger" -ForegroundColor White
Write-Host "  • PaymentService:     http://localhost:5034/swagger" -ForegroundColor White
Write-Host "  • NotificationService: http://localhost:5246/swagger" -ForegroundColor White
Write-Host "  • ChatbotService:     http://localhost:5055/swagger" -ForegroundColor White
Write-Host "`nFrontend will connect to: http://localhost:5189" -ForegroundColor Yellow
Write-Host "`nPress any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
