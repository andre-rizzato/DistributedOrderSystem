using Scalar.AspNetCore;
using MediatR;
using GatewayBff.Clients;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Each downstream microservice gets a typed HttpClient (an interface + implementation
// under Clients/, e.g. IOrderServiceClient), pointed at the URL configured in
// appsettings.Development.json -> ServiceUrls. Handlers/controllers depend on the
// interface instead of IHttpClientFactory, so routes, payload shapes, and response
// parsing for a given downstream service live in exactly one place.
var urls = builder.Configuration.GetSection("ServiceUrls");
builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>(c => c.BaseAddress = new Uri(urls["ProductService"]!));
builder.Services.AddHttpClient<IInventoryServiceClient, InventoryServiceClient>(c => c.BaseAddress = new Uri(urls["InventoryService"]!));
builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>(c => c.BaseAddress = new Uri(urls["OrderService"]!));
builder.Services.AddHttpClient<ICustomerServiceClient, CustomerServiceClient>(c => c.BaseAddress = new Uri(urls["CustomerService"]!));
// Used by ChatBffController to forward chat-widget requests to ChatbotService.
builder.Services.AddHttpClient<IChatbotServiceClient, ChatbotServiceClient>(c => c.BaseAddress = new Uri(urls["ChatbotService"]!));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// CORS configuration to allow requests from the frontend
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

// Disable HTTPS redirection in development to allow HTTP calls from the frontend
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();
