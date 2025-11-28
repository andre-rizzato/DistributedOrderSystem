using InventoryService.Cache;
using InventoryService.Cache.Interfaces;
using InventoryService.Configuration;
using InventoryService.Data;
using InventoryService.Services;
using InventoryService.Services.Interfaces;
using InventoryService.Messaging;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// EF Core + SQL Server
builder.Services.AddDbContext<InventoryContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("InventoryDb")
             ?? "Server=localhost,1433;Database=InventoryDb;User Id=sa;Password=YourStrong_Password123;TrustServerCertificate=True;";
    options.UseSqlServer(cs);
});

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

// Kafka Consumer (Background Service)
builder.Services.AddHostedService<OrderCreatedConsumer>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Inventory Service API", 
        Version = "v1",
        Description = "API for managing inventory items"
    });
});

var app = builder.Build();

// Ensure database is created and updated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InventoryContext>();
    context.Database.EnsureCreated();
}

// Configure Swagger middleware (only in development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Service API V1");
    });
}

// Disable HTTPS redirection in development to allow inter-service HTTP calls
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();

