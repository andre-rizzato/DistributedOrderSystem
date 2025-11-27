using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

var urls = builder.Configuration.GetSection("ServiceUrls");
builder.Services.AddHttpClient("ProductService", c => c.BaseAddress = new Uri(urls["ProductService"]!));
builder.Services.AddHttpClient("InventoryService", c => c.BaseAddress = new Uri(urls["InventoryService"]!));
builder.Services.AddHttpClient("OrderService", c => c.BaseAddress = new Uri(urls["OrderService"]!));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// CORS configuration to allow frontend requests
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
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");

// Disable HTTPS redirection in development to allow frontend HTTP calls
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();
