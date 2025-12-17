using CustomerWebsite.Services;

var builder = WebApplication.CreateBuilder(args);

// Aggiungi servizi al container
builder.Services.AddControllersWithViews();

// Configurazione sessioni
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".CustomerWebsite.Session";
});

// Configurazione HttpClient per i servizi API
builder.Services.AddHttpClient();

// Registrazione servizi applicativi
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IShoppingCartService, ShoppingCartService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Configurazione logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configurazione CORS per chiamate AJAX
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configurazione compressione response
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// Configurazione pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Abilita compressione
app.UseResponseCompression();

// Abilita routing
app.UseRouting();

// Abilita sessioni
app.UseSession();

// Abilita CORS
app.UseCors("DefaultPolicy");

app.UseAuthorization();

// Configurazione routing
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

// Gestione errori personalizzati
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

// Middleware per l'handling degli errori globali
app.UseMiddleware<GlobalExceptionMiddleware>();

app.Run();

/// <summary>
/// Middleware per la gestione globale delle eccezioni
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
            _logger.LogError(ex, "Errore non gestito: {Message}", ex.Message);
            
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Si è verificato un errore interno del server.");
            }
        }
    }
}
