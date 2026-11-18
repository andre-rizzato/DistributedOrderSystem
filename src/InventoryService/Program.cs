using Scalar.AspNetCore;
using InventoryService.Cache;
using InventoryService.Cache.Interfaces;
using InventoryService.Configuration;
using InventoryService.Data;
using InventoryService.Services;
using InventoryService.Services.Interfaces;
using InventoryService.Messaging;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// EF Core + PostgreSQL
var inventoryDbConnectionString = builder.Configuration.GetConnectionString("InventoryDb");
if (string.IsNullOrWhiteSpace(inventoryDbConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:InventoryDb is not configured. Set it with " +
        "'dotnet user-secrets set \"ConnectionStrings:InventoryDb\" \"<value>\"' " +
        "or the ConnectionStrings__InventoryDb environment variable.");
}

builder.Services.AddDbContext<InventoryContext>(options => options.UseNpgsql(inventoryDbConnectionString));

// Redis
builder.Services.Configure<RedisSettings>(
    builder.Configuration.GetSection("Redis"));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var conn = builder.Configuration.GetSection("Redis:ConnectionString").Value
               ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(conn);
});

// Register IDatabase from IConnectionMultiplexer
builder.Services.AddSingleton<IDatabase>(provider =>
{
    var connectionMultiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
    return connectionMultiplexer.GetDatabase();
});

// Configuration
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));

// Dependency Injection
builder.Services.AddSingleton<IInventoryCache, RedisInventoryCache>();
builder.Services.AddScoped<IInventoryWorkerService, InventoryWorkerService>();

// Kafka Producer (publishes the saga's compensating/confirming result back to OrderService)
builder.Services.AddSingleton<IInventoryEventProducer, InventoryEventProducer>();

// Kafka Consumer (Background Service)
builder.Services.AddHostedService<OrderCreatedConsumer>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Make sure the database is created and up to date
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InventoryContext>();
    context.Database.EnsureCreated();
}

// Configure Scalar middleware (development only)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Disable HTTPS redirection in development to allow HTTP calls between services

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();

