# PowerShell script to rewrite commit messages while preserving all dates
# This creates a new branch with rewritten history

$commits = @{
    "14f7b03" = "Switch catalog response to scalar value"
    "a9a7c0c" = "Fix various bugs and update launch configs"
    "ad38d83" = "Fix notification service bugs"
    "c9f8812" = @"
Add complete ShopVerse platform documentation

Wrote comprehensive technical docs for CustomerWebsite covering architecture, tech stack, design system, and development guidelines. Includes sections on microservices integration, responsive design, performance optimizations, security measures, monitoring, testing strategy, and future roadmap.

Document ready for team onboarding and reference.
"@
    "2af3298" = @"
Implement complete ShopVerse e-commerce platform

Built new CustomerWebsite with original branding and modern design. Replaced generic Amazon clone with custom ShopVerse identity using purple-indigo-teal color scheme.

Integrated with microservices (ProductService, OrderService, CartService) using ASP.NET Core MVC and CQRS patterns. Added responsive Bootstrap 5 UI with custom animations, interactive product cards, search functionality, and session-based cart management.

Ready for production deployment.
"@
    "4bf3296" = @"
Add NotificationService with multi-channel support

Built comprehensive notification system supporting SMS (Twilio), Email (MailKit), Push (Firebase), and real-time in-app notifications (SignalR).

Includes template system with variable substitution, background job processing with Hangfire, audit trail, analytics, rate limiting, and mock services for dev environment.

All endpoints documented and ready to use.
"@
    "dbe115c" = @"
Add ChatbotService with AI integration and embeddable widget

Integrated Microsoft DialoGPT-small with GPU acceleration support. Built standalone JavaScript chat widget with dual routing (BFF + direct service), customizable themes, and mobile-responsive design.

Includes advanced NLP features, fine-tuning framework, admin dashboard with real-time monitoring, and complete API documentation.

Widget demo page included for testing different configurations.
"@
    "01b4a4f" = "Update launch configuration"
    "eb1ae46" = @"
Add ChatbotService with ML.NET integration

Built lightweight chatbot microservice with JWT auth, Redis caching, and Swagger docs. Updated VS Code configs to use HTTP-only in dev to avoid cert issues.

Added comprehensive Italian code comments and documentation. Fixed build errors and set up proper port assignments for all services.
"@
    "2d182d3" = @"
Add startup delay for Kafka consumer

Temporary fix to give Kafka time to initialize before consumer starts polling.
"@
    "0cf6b80" = "Translate code comments to Italian"
    "ca65f87" = @"
Migrate Kafka to KRaft mode

Removed Zookeeper dependency and migrated to Kafka KRaft mode for simpler deployment. Fixed InventoryService consumer disposal issue and disabled HTTPS in dev environment.

Updated all docs to reflect the new architecture.
"@
    "3d35eb9" = "Fix InventoryService consumer bug"
    "40674d3" = "Add documentation and Kafka implementation"
    "ea27551" = "Add inventory management UI"
    "c817ea5" = "Route product calls through BFF gateway"
    "6a88a23" = "Add BFF gateway with CQRS endpoints"
    "8186e5d" = @"
Implement InventoryService with Redis and EF Core

Built complete inventory service with caching, database initialization, and Kafka integration for order events.
"@
    "6faaf63" = "Initialize InventoryService project"
    "3f14592" = "Add 404 page and navigation error handling"
    "a6fee79" = "Add code comments"
    "004e295" = "Disable HTTPS redirect in dev to fix frontend connection"
    "7a2efe9" = "Update service configurations"
    "885f20e" = "Update tasks to auto-start Angular app"
    "f648ad4" = "Downgrade to .NET 9 for Linux compatibility"
    "bd2fca9" = "Add Angular frontend with product management"
    "51c141e" = "Implement ProductService with Redis cache and SQL Server"
}

Write-Host "Preparing to rewrite commits..." -ForegroundColor Yellow
Write-Host ""

# Get current branch
$currentBranch = git rev-parse --abbrev-ref HEAD
Write-Host "Current branch: $currentBranch" -ForegroundColor Cyan

# Create backup branch
$backupBranch = "${currentBranch}-backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
git branch $backupBranch
Write-Host "Created backup branch: $backupBranch" -ForegroundColor Green
Write-Host ""

# Create message map file for filter-branch
$mapFile = Join-Path $PSScriptRoot "commit-message-map.txt"
$commits.GetEnumerator() | ForEach-Object {
    $hash = $_.Key
    $msg = $_.Value
    Add-Content -Path $mapFile -Value "$hash|$msg"
}

Write-Host "Starting commit rewrite..." -ForegroundColor Yellow
Write-Host "This will preserve all original commit dates." -ForegroundColor Yellow
Write-Host ""

# Use environment variable to pass the map file path
$env:COMMIT_MAP_FILE = $mapFile

$filterScript = @'
$mapFile = $env:COMMIT_MAP_FILE
$map = @{}
Get-Content $mapFile | ForEach-Object {
    $parts = $_ -split '\|', 2
    if ($parts.Length -eq 2) {
        $map[$parts[0]] = $parts[1]
    }
}

$commitHash = git rev-parse --short HEAD
$currentMsg = git log -1 --format=%B

foreach ($hash in $map.Keys) {
    if ($commitHash -eq $hash) {
        Write-Output $map[$hash]
        exit 0
    }
}

Write-Output $currentMsg
'@

$filterScriptFile = Join-Path $PSScriptRoot "filter-msg.ps1"
Set-Content -Path $filterScriptFile -Value $filterScript

# Run filter-branch
git filter-branch -f --msg-filter "powershell -ExecutionPolicy Bypass -File `"$filterScriptFile`"" d7ee8b2..HEAD

# Cleanup
Remove-Item $mapFile -ErrorAction SilentlyContinue
Remove-Item $filterScriptFile -ErrorAction SilentlyContinue
Remove-Item env:COMMIT_MAP_FILE -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Commit messages rewritten successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Original commits backed up in branch: $backupBranch" -ForegroundColor Cyan
Write-Host ""
Write-Host "To push the changes to remote, run:" -ForegroundColor Yellow
Write-Host "  git push --force-with-lease origin $currentBranch" -ForegroundColor White
Write-Host ""
Write-Host "To restore original commits if needed:" -ForegroundColor Yellow
Write-Host "  git reset --hard $backupBranch" -ForegroundColor White
