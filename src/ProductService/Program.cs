using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using ProductService.Cache.Interfaces;
using ProductService.Cache;
using ProductService.Configuration;
using ProductService.Data;
using ProductService.Repositories.Interfaces;
using ProductService.Repositories;
using ProductService.Services.Interfaces;
using ProductService.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// EF Core + PostgreSQL
builder.Services.AddDbContext<ProductDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("ProductDb")
             ?? "Host=localhost;Port=5432;Database=ProductDb;Username=postgres;Password=YourStrong_Password123;";
    options.UseNpgsql(cs);
});

// Configurazione Redis
builder.Services.Configure<RedisSettings>(
    builder.Configuration.GetSection("Redis"));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var config = builder.Configuration.GetSection("Redis:ConnectionString").Value
                 ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(config);
});

// Dependency Injection
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddSingleton<IProductCache, RedisProductCache>();
builder.Services.AddScoped<IProductService, ProductWorkerServices>();

// Servizi API
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi("v1");

// CORS (se necessario per il frontend)
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

// Configura la pipeline delle richieste HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Assicura che il database sia creato
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    context.Database.EnsureCreated();
}

// Commenta il reindirizzamento HTTPS per lo sviluppo per evitare problemi con il frontend
// app.UseHttpsRedirection();

// Abilita CORS se configurato
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}

app.MapControllers();

app.Run();
