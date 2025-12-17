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

// EF Core + SQL Server
builder.Services.AddDbContext<ProductDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("ProductDb")
             ?? "Server=localhost,1433;Database=ProductDb;User Id=sa;Password=YourStrong_Password123;TrustServerCertificate=True;";
    options.UseSqlServer(cs);
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
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

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
