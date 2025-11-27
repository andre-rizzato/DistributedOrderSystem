using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Services;
using OrderService.Messaging;
using OrderService.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuration
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));

// Database
builder.Services.AddDbContext<OrderContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderDb")));

// Services
builder.Services.AddScoped<IOrderService, OrderWorkerService>();

// Kafka Producer
builder.Services.AddSingleton<IOrderEventProducer, OrderEventProducer>();

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
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderContext>();
    context.Database.EnsureCreated();
}

app.Run();
