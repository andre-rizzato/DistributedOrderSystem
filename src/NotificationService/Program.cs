using Microsoft.EntityFrameworkCore;
using NotificationService.Configuration;
using NotificationService.Data;
using NotificationService.Services;
using NotificationService.Services.Implementations;
using StackExchange.Redis;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configurazione servizi
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configurazione database
builder.Services.AddDbContext<NotificationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurazione Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(connectionString!);
});

// Configurazione Hangfire per job in background
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

builder.Services.AddHangfireServer();

// Configurazione SignalR
builder.Services.AddSignalR();
    // Note: AddStackExchangeRedis extension is not available, Redis can be configured separately

// Configurazione settings
builder.Services.Configure<SmsSettings>(builder.Configuration.GetSection("Sms"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<PushNotificationSettings>(builder.Configuration.GetSection("PushNotification"));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<TemplateSettings>(builder.Configuration.GetSection("Templates"));
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimit"));

// Registrazione servizi notifica
if (builder.Environment.IsDevelopment())
{
    // Mock services per sviluppo
    builder.Services.AddScoped<ISmsService, MockSmsService>();
    builder.Services.AddScoped<IEmailService, MockEmailService>();
    builder.Services.AddScoped<IPushService, MockPushService>();
}
else
{
    // Servizi reali per produzione
    builder.Services.AddScoped<ISmsService, TwilioSmsService>();
    builder.Services.AddScoped<IEmailService, MailKitEmailService>();
    builder.Services.AddScoped<IPushService, FirebasePushService>();
}

// Servizi sempre attivi
builder.Services.AddScoped<IInAppNotificationService, SignalRInAppNotificationService>();
builder.Services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
builder.Services.AddScoped<INotificationService, NotificationService.Services.Implementations.NotificationService>();

// Health checks
builder.Services.AddHealthChecks();
    // .AddRedis(builder.Configuration.GetConnectionString("Redis")!);
    // Note: AddDbContext is not available for health checks, use AddDbContextCheck instead

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    // if (!builder.Environment.IsDevelopment())
    // {
    //     logging.AddApplicationInsights();
    // }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();

// Middleware Hangfire dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map controllers e hub SignalR
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

// Migrazione database automatica in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<NotificationContext>();
    context.Database.EnsureCreated();
}

app.Run();

/// <summary>
/// Filtro autorizzazione per Hangfire Dashboard
/// </summary>
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // In development, consenti accesso libero - sempre true per ora
        return true;
    }
}
