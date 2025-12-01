using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Services;
using OrderService.Messaging;
using OrderService.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Aggiungi servizi al contenitore
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurazione
builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));

// Database
builder.Services.AddDbContext<OrderContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderDb")));

// Servizi
builder.Services.AddScoped<IOrderService, OrderWorkerService>();

// Produttore Kafka
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

// Configura la pipeline delle richieste HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
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
