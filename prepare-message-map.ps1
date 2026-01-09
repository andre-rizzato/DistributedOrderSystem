# Better approach: Use git rebase interactive with prepare-commit-msg hook

# First, let's create a commit message mapping
$messageMap = @"
Change to scalar=Switch catalog response to scalar value
bug fixes, launch options, etc=Fix various bugs and update launch configs
Bug fixes on notification services=Fix notification service bugs
docs: Aggiunta documentazione completa ShopVerse Platform=Add complete ShopVerse platform documentation

Wrote comprehensive technical docs for CustomerWebsite covering architecture, tech stack, design system, and development guidelines. Includes sections on microservices integration, responsive design, performance optimizations, security measures, monitoring, testing strategy, and future roadmap.

Document ready for team onboarding and reference.
feat: Implementazione completa ShopVerse - Piattaforma e-commerce moderna=Implement complete ShopVerse e-commerce platform

Built new CustomerWebsite with original branding and modern design. Replaced generic Amazon clone with custom ShopVerse identity using purple-indigo-teal color scheme.

Integrated with microservices (ProductService, OrderService, CartService) using ASP.NET Core MVC and CQRS patterns. Added responsive Bootstrap 5 UI with custom animations, interactive product cards, search functionality, and session-based cart management.

Ready for production deployment.
feat: Implement comprehensive NotificationService with multi-channel support=Add NotificationService with multi-channel support

Built comprehensive notification system supporting SMS (Twilio), Email (MailKit), Push (Firebase), and real-time in-app notifications (SignalR).

Includes template system with variable substitution, background job processing with Hangfire, audit trail, analytics, rate limiting, and mock services for dev environment.

All endpoints documented and ready to use.
🤖 Implement comprehensive ChatbotService with AI integration and embeddable chat widget=Add ChatbotService with AI integration and embeddable widget

Integrated Microsoft DialoGPT-small with GPU acceleration support. Built standalone JavaScript chat widget with dual routing (BFF + direct service), customizable themes, and mobile-responsive design.

Includes advanced NLP features, fine-tuning framework, admin dashboard with real-time monitoring, and complete API documentation.

Widget demo page included for testing different configurations.
adjustment launch=Update launch configuration
🤖 feat: Add ChatbotService with comprehensive Italian documentation=Add ChatbotService with ML.NET integration

Built lightweight chatbot microservice with JWT auth, Redis caching, and Swagger docs. Updated VS Code configs to use HTTP-only in dev to avoid cert issues.

Added comprehensive Italian code comments and documentation. Fixed build errors and set up proper port assignments for all services.
Delay for lazzy orderCreatedConsumer (temporary solution)=Add startup delay for Kafka consumer

Temporary fix to give Kafka time to initialize before consumer starts polling.
comments in italiano=Translate code comments to Italian
Migrate Kafka to KRaft mode and fix service issues=Migrate Kafka to KRaft mode

Removed Zookeeper dependency and migrated to Kafka KRaft mode for simpler deployment. Fixed InventoryService consumer disposal issue and disabled HTTPS in dev environment.

Updated all docs to reflect the new architecture.
Bugfix InventoryService=Fix InventoryService consumer bug
documentation, comments and kafka implementation=Add documentation and Kafka implementation
Added inventory FrontEnd=Add inventory management UI
changed call to pass through BFF=Route product calls through BFF gateway
Add Gateway BFF with CQRS endpoints=Add BFF gateway with CQRS endpoints
Implement InventoryService with Redis caching, Entity Framework, and database initialization=Implement InventoryService with Redis and EF Core

Built complete inventory service with caching, database initialization, and Kafka integration for order events.
Initiating InventoryService=Initialize InventoryService project
Add navigation error handling and 404 page component=Add 404 page and navigation error handling
comments=Add code comments
Comment out HTTPS redirection for development to avoid issues with frontend=Disable HTTPS redirect in dev to fix frontend connection
configs change=Update service configurations
Changed taks and launch to start also angular app=Update tasks to auto-start Angular app
downgrade .net version for compatibility with linux - net10 not available=Downgrade to .NET 9 for Linux compatibility
Add Angular frontend application with product management=Add Angular frontend with product management
Implement ProductService API with Redis cache and SQL Server=Implement ProductService with Redis cache and SQL Server
"@

Set-Content -Path "message-map.txt" -Value $messageMap -Encoding UTF8

Write-Host "Message map created. Now using git-filter-repo method..." -ForegroundColor Yellow
Write-Host ""
Write-Host "Note: This requires git-filter-repo to be installed." -ForegroundColor Cyan
Write-Host "Install with: pip install git-filter-repo" -ForegroundColor Cyan
Write-Host ""
Write-Host "Or we can use manual interactive rebase." -ForegroundColor Yellow
