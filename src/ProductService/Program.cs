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

// Redis config
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

// API Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Product Service API", 
        Version = "v1",
        Description = "API for managing products in the distributed order system"
    });
});

// CORS (if needed for frontend)
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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service API v1");
        c.RoutePrefix = string.Empty; // Makes Swagger UI available at root
    });
}

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    context.Database.EnsureCreated();
}

// Comment out HTTPS redirection for development to avoid issues with frontend
// app.UseHttpsRedirection();

// Enable CORS if configured
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}

app.MapControllers();

app.Run();
