using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Configuration;
using OrderService.Domain.Interfaces;
using OrderService.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Presentation ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// ── Infrastructure: Configuration ──
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));

// ── Infrastructure: Database (EF Core + PostgreSQL) ──
builder.Services.AddDbContext<OrderContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDb")));

// ── Infrastructure: Repository (implements the Domain layer's interface) ──
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// ── Infrastructure: Messaging (Kafka) ──
builder.Services.AddSingleton<IOrderEventProducer, OrderEventProducer>();

// Transactional outbox dispatcher: publishes OrderCreatedEvent rows written by
// OrderApplicationService.CreateOrderAsync, with its own retry/poll loop.
builder.Services.AddHostedService<OutboxDispatcherService>();

// Saga participant: applies InventoryService's reservation outcome (Confirmed/Cancelled).
builder.Services.AddHostedService<InventoryReservationResultConsumer>();

// ── Application: Application service ──
builder.Services.AddScoped<IOrderApplicationService, OrderApplicationService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.MapControllers();

// Make sure the database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderContext>();
    context.Database.EnsureCreated();
}

app.Run();
