# Script to rewrite commit messages while preserving dates
# Run this interactively - it will rebase and allow editing

$commits = @(
    @{hash="14f7b03"; msg="Switch catalog response to scalar value"; date="2025-12-17 23:36:05 +0100"}
    @{hash="a9a7c0c"; msg="Fix various bugs and update launch configs"; date="2025-12-17 23:14:58 +0100"}
    @{hash="ad38d83"; msg="Fix notification service bugs"; date="2025-12-17 22:18:10 +0100"}
    @{hash="c9f8812"; msg="Add complete ShopVerse platform documentation

Wrote comprehensive technical docs for CustomerWebsite covering architecture, tech stack, design system, and development guidelines. Includes sections on microservices integration, responsive design, performance optimizations, security measures, monitoring, testing strategy, and future roadmap.

Document ready for team onboarding and reference."; date="2025-12-17 15:38:54 +0100"}
    @{hash="2af3298"; msg="Implement complete ShopVerse e-commerce platform

Built new CustomerWebsite with original branding and modern design. Replaced generic Amazon clone with custom ShopVerse identity using purple-indigo-teal color scheme.

Integrated with microservices (ProductService, OrderService, CartService) using ASP.NET Core MVC and CQRS patterns. Added responsive Bootstrap 5 UI with custom animations, interactive product cards, search functionality, and session-based cart management.

Ready for production deployment."; date="2025-12-17 15:36:20 +0100"}
    @{hash="4bf3296"; msg="Add NotificationService with multi-channel support

Built comprehensive notification system supporting SMS (Twilio), Email (MailKit), Push (Firebase), and real-time in-app notifications (SignalR).

Includes template system with variable substitution, background job processing with Hangfire, audit trail, analytics, rate limiting, and mock services for dev environment.

All endpoints documented and ready to use."; date="2025-12-16 22:09:50 +0100"}
    @{hash="dbe115c"; msg="Add ChatbotService with AI integration and embeddable widget

Integrated Microsoft DialoGPT-small with GPU acceleration support. Built standalone JavaScript chat widget with dual routing (BFF + direct service), customizable themes, and mobile-responsive design.

Includes advanced NLP features, fine-tuning framework, admin dashboard with real-time monitoring, and complete API documentation.

Widget demo page included for testing different configurations."; date="2025-12-16 21:35:19 +0100"}
    @{hash="01b4a4f"; msg="Update launch configuration"; date="2025-12-16 20:14:29 +0100"}
    @{hash="eb1ae46"; msg="Add ChatbotService with ML.NET integration

Built lightweight chatbot microservice with JWT auth, Redis caching, and Swagger docs. Updated VS Code configs to use HTTP-only in dev to avoid cert issues.

Added comprehensive Italian code comments and documentation. Fixed build errors and set up proper port assignments for all services."; date="2025-12-16 20:01:20 +0100"}
    @{hash="2d182d3"; msg="Add startup delay for Kafka consumer

Temporary fix to give Kafka time to initialize before consumer starts polling."; date="2025-12-06 22:56:09 +0100"}
    @{hash="0cf6b80"; msg="Translate code comments to Italian"; date="2025-12-01 21:56:48 +0100"}
    @{hash="ca65f87"; msg="Migrate Kafka to KRaft mode

Removed Zookeeper dependency and migrated to Kafka KRaft mode for simpler deployment. Fixed InventoryService consumer disposal issue and disabled HTTPS in dev environment.

Updated all docs to reflect the new architecture."; date="2025-11-28 21:46:14 +0100"}
    @{hash="3d35eb9"; msg="Fix InventoryService consumer bug"; date="2025-11-28 21:45:14 +0100"}
    @{hash="40674d3"; msg="Add documentation and Kafka implementation"; date="2025-11-27 22:56:41 +0100"}
    @{hash="ea27551"; msg="Add inventory management UI"; date="2025-11-27 22:14:26 +0100"}
    @{hash="c817ea5"; msg="Route product calls through BFF gateway"; date="2025-11-27 22:08:25 +0100"}
    @{hash="6a88a23"; msg="Add BFF gateway with CQRS endpoints"; date="2025-11-26 21:18:08 +0100"}
    @{hash="8186e5d"; msg="Implement InventoryService with Redis and EF Core

Built complete inventory service with caching, database initialization, and Kafka integration for order events."; date="2025-11-25 14:14:49 +0100"}
    @{hash="6faaf63"; msg="Initialize InventoryService project"; date="2025-11-24 22:30:35 +0100"}
    @{hash="3f14592"; msg="Add 404 page and navigation error handling"; date="2025-11-22 17:51:44 +0100"}
    @{hash="a6fee79"; msg="Add code comments"; date="2025-11-19 22:02:50 +0100"}
    @{hash="004e295"; msg="Disable HTTPS redirect in dev to fix frontend connection"; date="2025-11-19 18:53:32 +0100"}
    @{hash="7a2efe9"; msg="Update service configurations"; date="2025-11-19 18:48:00 +0100"}
    @{hash="885f20e"; msg="Update tasks to auto-start Angular app"; date="2025-11-19 18:02:47 +0100"}
    @{hash="f648ad4"; msg="Downgrade to .NET 9 for Linux compatibility"; date="2025-11-19 10:19:21 +0100"}
    @{hash="bd2fca9"; msg="Add Angular frontend with product management"; date="2025-11-18 23:01:40 +0100"}
    @{hash="51c141e"; msg="Implement ProductService with Redis cache and SQL Server"; date="2025-11-18 21:16:11 +0100"}
)

Write-Host "Commit messages prepared. To rewrite, run:"
Write-Host "git rebase -i d7ee8b2"
Write-Host ""
Write-Host "Then replace 'pick' with 'reword' for commits you want to change."
Write-Host "Use the messages from this script when prompted."
