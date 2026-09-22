using Scalar.AspNetCore;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Each downstream microservice gets a named HttpClient, pointed at the URL
// configured in appsettings.Development.json -> ServiceUrls. Controllers ask
// IHttpClientFactory for a client by this same name (see ChatBffController).
var urls = builder.Configuration.GetSection("ServiceUrls");
builder.Services.AddHttpClient("ProductService", c => c.BaseAddress = new Uri(urls["ProductService"]!));
builder.Services.AddHttpClient("InventoryService", c => c.BaseAddress = new Uri(urls["InventoryService"]!));
builder.Services.AddHttpClient("OrderService", c => c.BaseAddress = new Uri(urls["OrderService"]!));
// Used by ChatBffController to forward chat-widget requests to ChatbotService.
builder.Services.AddHttpClient("ChatbotService", c => c.BaseAddress = new Uri(urls["ChatbotService"]!));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configurazione CORS per consentire le richieste del frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
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

app.UseCors("AllowFrontend");

// Disabilita il reindirizzamento HTTPS in sviluppo per consentire chiamate HTTP del frontend
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();
