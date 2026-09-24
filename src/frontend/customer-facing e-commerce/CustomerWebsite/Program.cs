using CustomerWebsite.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Session configuration
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".CustomerWebsite.Session";
});

// HttpClient configuration for API services
builder.Services.AddHttpClient();

// Application service registration
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// CORS configuration for AJAX calls
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Response compression configuration
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// HTTP pipeline configuration
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Enable compression
app.UseResponseCompression();

// Enable routing
app.UseRouting();

// Enable sessions
app.UseSession();

// Enable CORS
app.UseCors("DefaultPolicy");

app.UseAuthorization();

// Route configuration
app.MapControllerRoute(
    name: "productDetails",
    pattern: "Product/{id:guid}",
    defaults: new { controller = "Product", action = "Details" });

app.MapControllerRoute(
    name: "categoryProducts",
    pattern: "Category/{categoryName}",
    defaults: new { controller = "Home", action = "Category" });

app.MapControllerRoute(
    name: "search",
    pattern: "Search",
    defaults: new { controller = "Home", action = "Search" });

app.MapControllerRoute(
    name: "cart",
    pattern: "Cart",
    defaults: new { controller = "Cart", action = "Index" });

app.MapControllerRoute(
    name: "checkout",
    pattern: "Checkout",
    defaults: new { controller = "Checkout", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapStaticAssets();

// Custom error handling
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

// Middleware for global error handling
app.UseMiddleware<GlobalExceptionMiddleware>();

app.Run();

/// <summary>
/// Middleware for global exception handling
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error: {Message}", ex.Message);

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("An internal server error occurred.");
            }
        }
    }
}
