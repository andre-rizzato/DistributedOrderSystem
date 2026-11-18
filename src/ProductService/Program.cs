using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using MediatR;
using FluentValidation;
using ProductService.Application.Common.Behaviors;
using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Interfaces;
using ProductService.Infrastructure.Cache;
using ProductService.Infrastructure.Configuration;
using ProductService.Infrastructure.Data;
using ProductService.Infrastructure.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure: Database (EF Core + PostgreSQL) ──
var productDbConnectionString = builder.Configuration.GetConnectionString("ProductDb");
if (string.IsNullOrWhiteSpace(productDbConnectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:ProductDb is not configured. Set it with " +
        "'dotnet user-secrets set \"ConnectionStrings:ProductDb\" \"<value>\"' " +
        "or the ConnectionStrings__ProductDb environment variable.");
}

builder.Services.AddDbContext<ProductDbContext>(options => options.UseNpgsql(productDbConnectionString));

// ── Infrastructure: Redis ──
builder.Services.Configure<RedisSettings>(
    builder.Configuration.GetSection("Redis"));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var config = builder.Configuration.GetSection("Redis:ConnectionString").Value
                 ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(config);
});

// ── Infrastructure → Domain: Repository (Dependency Inversion) ──
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// ── Infrastructure → Application: Cache (Dependency Inversion) ──
builder.Services.AddSingleton<IProductCache, RedisProductCache>();

// ── Application: MediatR CQRS + Pipeline Behaviors ──
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// ── Presentation ──
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v1");

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Make sure the database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    context.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}

app.MapControllers();
app.Run();
