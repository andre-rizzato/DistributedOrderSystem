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
builder.Services.AddDbContext<InventoryContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("InventoryDb")
             ?? "Host=localhost;Port=5432;Database=InventoryDb;Username=postgres;Password=YourStrong_Password123;";
    options.UseNpgsql(cs);
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

// Registra IDatabase da IConnectionMultiplexer
builder.Services.AddSingleton<IDatabase>(provider =>
{
    var connectionMultiplexer = provider.GetRequiredService<IConnectionMultiplexer>();
    return connectionMultiplexer.GetDatabase();
});

// Configurazione
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));

// Dependency Injection
builder.Services.AddSingleton<IInventoryCache, RedisInventoryCache>();
builder.Services.AddScoped<IInventoryWorkerService, InventoryWorkerService>();

// Consumer Kafka (Servizio in Background)
builder.Services.AddHostedService<OrderCreatedConsumer>();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Assicura che il database sia creato e aggiornato
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InventoryContext>();
    context.Database.EnsureCreated();
}

// Configura middleware Scalar (solo in sviluppo)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Disabilita il reindirizzamento HTTPS in sviluppo per consentire chiamate HTTP tra servizi

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();

