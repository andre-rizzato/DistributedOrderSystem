using Microsoft.EntityFrameworkCore;
using NotificationService.Configuration;
using NotificationService.Data;
using NotificationService.Services;
using NotificationService.Services.Implementations;
using StackExchange.Redis;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Service configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Database configuration
builder.Services.AddDbContext<NotificationContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis configuration
builder.Services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(connectionString!);
});

// Hangfire configuration for background jobs
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

builder.Services.AddHangfireServer();

// SignalR configuration
builder.Services.AddSignalR();
    // Note: AddStackExchangeRedis extension is not available, Redis can be configured separately

// Settings configuration
builder.Services.Configure<SmsSettings>(builder.Configuration.GetSection("Sms"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<PushNotificationSettings>(builder.Configuration.GetSection("PushNotification"));
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<TemplateSettings>(builder.Configuration.GetSection("Templates"));
builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimit"));

// Notification services registration
if (builder.Environment.IsDevelopment())
{
    // Mock services for development
    builder.Services.AddScoped<ISmsService, MockSmsService>();
    builder.Services.AddScoped<IEmailService, MockEmailService>();
    builder.Services.AddScoped<IPushService, MockPushService>();
}
else
{
    // Real services for production
    builder.Services.AddScoped<ISmsService, TwilioSmsService>();
    builder.Services.AddScoped<IEmailService, MailKitEmailService>();
    builder.Services.AddScoped<IPushService, FirebasePushService>();
}

// Always-active services
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

// Automatic database migration in development.
// Must happen before UseHangfireDashboard: Hangfire opens a connection
// to the database as soon as the dashboard is configured, so the DB
// must already exist at that point.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<NotificationContext>();
    context.Database.EnsureCreated();
}

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

// Map controllers and SignalR hub
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

app.Run();

/// <summary>
/// Authorization filter for the Hangfire Dashboard
/// </summary>
public class HangfireAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // In development, allow free access - always true for now
        return true;
    }
}
