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

// ── Infrastructure: Configurazione ──
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));

// ── Infrastructure: Database (EF Core + PostgreSQL) ──
builder.Services.AddDbContext<OrderContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDb")));

// ── Infrastructure: Repository (implementa interfaccia del Domain layer) ──
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// ── Infrastructure: Messaging (Kafka) ──
builder.Services.AddSingleton<IOrderEventProducer, OrderEventProducer>();

// ── Application: Servizio applicativo ──
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

// Configura la pipeline delle richieste HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.MapControllers();

// Assicura che il database sia creato
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderContext>();
    context.Database.EnsureCreated();
}

app.Run();
