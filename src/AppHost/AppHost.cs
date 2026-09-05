var builder = DistributedApplication.CreateBuilder(args);

// Postgres, Redis, and Kafka are NOT managed here — they keep running via
// `docker compose -f docker/docker-compose.yml up -d postgres redis kafka`,
// exactly as they do outside Aspire. Each project's own appsettings.Development.json
// already points at localhost for all three, so no extra wiring is needed.

var productService = builder.AddProject<Projects.ProductService>("productservice", launchProfileName: "http");

var inventoryService = builder.AddProject<Projects.InventoryService>("inventoryservice", launchProfileName: "http");

var orderService = builder.AddProject<Projects.OrderService>("orderservice", launchProfileName: "http");

builder.AddProject<Projects.GatewayBff>("gatewaybff", launchProfileName: "http")
    .WaitFor(productService)
    .WaitFor(inventoryService)
    .WaitFor(orderService)
    .WithReference(productService)
    .WithReference(inventoryService)
    .WithReference(orderService);

builder.Build().Run();
