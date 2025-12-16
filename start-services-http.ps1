# Script per avviare tutti i microservizi con configurazione HTTP
# Risolve i problemi di certificato HTTPS in sviluppo

Write-Host "🚀 Avvio di tutti i microservizi con configurazione HTTP..." -ForegroundColor Green

# Imposta variabili di ambiente per tutti i processi
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_HTTPS_PORT = ""

# Array con servizi e porte
$services = @(
    @{Name="GatewayBff"; Port=5000; Path="src/GatewayBff"},
    @{Name="OrderService"; Port=5001; Path="src/OrderService"},
    @{Name="InventoryService"; Port=5002; Path="src/InventoryService"},
    @{Name="NotificationService"; Port=5003; Path="src/NotificationService"},
    @{Name="PaymentService"; Port=5004; Path="src/PaymentService"},
    @{Name="🤖 ChatbotService"; Port=5055; Path="src/ChatbotService"},
    @{Name="ProductService"; Port=5006; Path="src/ProductService"}
)

# Arresta eventuali processi esistenti
Write-Host "🛑 Arresto servizi esistenti..." -ForegroundColor Yellow
Get-Process | Where-Object {$_.ProcessName -eq "dotnet"} | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Avvia ogni servizio
foreach ($service in $services) {
    Write-Host "🔧 Avvio $($service.Name) su porta $($service.Port)..." -ForegroundColor Cyan
    
    $env:ASPNETCORE_URLS = "http://localhost:$($service.Port)"
    
    # Avvia il servizio in un processo separato
    Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "$($service.Path)" -WorkingDirectory $PWD -WindowStyle Minimized
    
    Start-Sleep -Seconds 1
}

Write-Host ""
Write-Host "✅ Tutti i servizi sono stati avviati!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Endpoints disponibili:" -ForegroundColor White
Write-Host "  🌐 Gateway BFF:        http://localhost:5000" -ForegroundColor Gray
Write-Host "  📦 OrderService:       http://localhost:5001/swagger" -ForegroundColor Gray  
Write-Host "  📦 InventoryService:   http://localhost:5002/swagger" -ForegroundColor Gray
Write-Host "  📢 NotificationService: http://localhost:5003/swagger" -ForegroundColor Gray
Write-Host "  💳 PaymentService:     http://localhost:5004/swagger" -ForegroundColor Gray
Write-Host "  🤖 ChatbotService:     http://localhost:5055/swagger" -ForegroundColor Gray
Write-Host "  🛍️ ProductService:      http://localhost:5006/swagger" -ForegroundColor Gray
Write-Host ""
Write-Host "⚡ Per aprire Swagger del ChatbotService:" -ForegroundColor Yellow
Write-Host "   http://localhost:5055/swagger" -ForegroundColor White