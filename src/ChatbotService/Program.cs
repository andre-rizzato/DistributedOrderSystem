/* ================================================================
 * CHATBOT SERVICE - AI CHATBOT MICROSERVICE
 * ================================================================
 *
 * This is the main entry point for the chatbot service, which
 * provides artificial intelligence features for customer support
 * and sales assistance.
 *
 * Main features:
 * - Intelligent chat with intent classification
 * - JWT authentication
 * - AI model fine-tuning
 * - Integration with other microservices
 * - Admin dashboard
 *
 * Author: Distributed Order System
 * Date: December 2025
 * Version: 1.0.0
 */

// Entity Framework imports (database)
using Microsoft.EntityFrameworkCore;
// JWT (JSON Web Token) authentication
using Microsoft.AspNetCore.Authentication.JwtBearer;
// Security token validation
using Microsoft.IdentityModel.Tokens;
// Text encoding
using System.Text;
// Scalar API documentation
using Scalar.AspNetCore;
// Chatbot database context
using ChatbotService.Data;
// Chatbot services
using ChatbotService.Services;
// Service interfaces
using ChatbotService.Services.Interfaces;
// System configuration
using ChatbotService.Configuration;
// Redis for session management
using StackExchange.Redis;

// Create the web application builder with command-line arguments
var builder = WebApplication.CreateBuilder(args);

/* ================================================================
 * DATABASE CONFIGURATION
 * ================================================================ */

// Database context configuration for Entity Framework
// Uses PostgreSQL as the primary database to store:
// - User chat sessions
// - Message history
// - AI training data
// - Fine-tuning jobs
builder.Services.AddDbContext<ChatContext>(options =>
    // Connect to PostgreSQL using the "ChatbotDb" connection string
    options.UseNpgsql(builder.Configuration.GetConnectionString("ChatbotDb")));

/* ================================================================
 * REDIS CONFIGURATION
 * ================================================================ */

// Redis configuration for chat session management
// Redis is used as a distributed cache for:
// - Temporary storage of user sessions
// - Caching frequent chatbot responses
// - Tracking the state of ongoing conversations
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    // Connect to Redis, falling back to localhost port 6379 if not configured
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

/* ================================================================
 * CONFIGURATION SECTIONS
 * ================================================================ */

// AI model settings
// Includes model paths, temperature parameters, max tokens
builder.Services.Configure<ModelSettings>(builder.Configuration.GetSection("ModelSettings"));

// External microservice URL settings
// Defines the endpoints for OrderService, ProductService, PaymentService, etc.
builder.Services.Configure<ServiceUrlsSettings>(builder.Configuration.GetSection("ServiceUrls"));

// JWT authentication settings
// Includes secret key, issuer, audience, token lifetime
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("Auth"));

// Fine-tuning settings
// Includes training parameters, learning rate, batch size, epochs
builder.Services.Configure<FineTuningSettings>(builder.Configuration.GetSection("FineTuning"));

/* ================================================================
 * HTTP CLIENTS FOR SERVICE INTEGRATION
 * ================================================================ */

// HTTP client configuration for integrating with other microservices
// This client is used to communicate with:
// - OrderService (order management)
// - ProductService (product catalog)
// - PaymentService (payments)
// - InventoryService (inventory)
builder.Services.AddHttpClient<IServiceIntegration, ServiceIntegrationService>(client =>
{
    // 30-second timeout for HTTP calls to external services
    client.Timeout = TimeSpan.FromSeconds(30);
});

/* ================================================================
 * CORE SERVICE REGISTRATION
 * ================================================================ */

// Main chatbot service - HTTP bridge to AgentService (Python,
// LangGraph). Replaces the old registration that was left commented
// out (it never had a real implementation behind it).
builder.Services.AddHttpClient<IChatbotService, PythonAgentChatbotService>(client =>
{
    var agentServiceUrl = builder.Configuration["AgentService:BaseUrl"] ?? "http://localhost:8100";
    client.BaseAddress = new Uri(agentServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Advanced NLP (Natural Language Processing) service using DialoGPT
// Provides intent classification using real conversational AI
// Uses Microsoft DialoGPT-small to generate natural responses
builder.Services.AddScoped<INLPService, AdvancedNLPService>();
builder.Services.AddHttpClient<AdvancedNLPService>();

// Authentication service to handle user login and registration
// Generates and validates JWT tokens for secure API access
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Fine-tuning service for custom AI model training
builder.Services.AddScoped<IFineTuningService, FineTuningService>();

// Background service that initializes AI models
// Automatically downloads DialoGPT when the service starts
builder.Services.AddHostedService<ModelInitializationService>();

/* ================================================================
 * JWT AUTHENTICATION CONFIGURATION
 * ================================================================ */

// Read authentication settings from the configuration file
// Falls back to default settings if not found, to avoid errors
var authSettings = builder.Configuration.GetSection("Auth").Get<AuthSettings>() ?? new AuthSettings();

// JWT Bearer authentication service configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // JWT token validation parameters
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Validate the token issuer
            ValidateIssuer = true,
            // Validate the token audience
            ValidateAudience = true,
            // Validate the token lifetime (expiration)
            ValidateLifetime = true,
            // Validate the token signing key
            ValidateIssuerSigningKey = true,
            // Valid issuer (who issues the tokens)
            ValidIssuer = authSettings.Issuer,
            // Valid audience (who the tokens are intended for)
            ValidAudience = authSettings.Audience,
            // Secret key used to validate the token signature
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(authSettings.SecretKey))
        };
    });

// Authorization configuration with role-based policies
builder.Services.AddAuthorization(options =>
{
    // "Admin" policy requiring the administrator role
    // Users must have the "Admin" role to access protected endpoints
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

/* ================================================================
 * API SERVICES CONFIGURATION
 * ================================================================ */

// Register MVC controllers to handle HTTP API requests
// Includes automatic JSON serialization and model validation
builder.Services.AddControllers();

// Service for exploring API endpoints (required for Scalar)
builder.Services.AddEndpointsApiExplorer();

// OpenAPI configuration for interactive API documentation
builder.Services.AddOpenApi();

/* ================================================================
 * CORS CONFIGURATION (Cross-Origin Resource Sharing)
 * ================================================================ */

// CORS to allow requests from different origins
// Required so the frontend can communicate with the API
builder.Services.AddCors(options =>
{
    // Default CORS policy specifying which origins are allowed
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                // Angular frontend in development (HTTP)
                "http://localhost:4200",
                // Angular frontend in development (HTTPS)
                "https://localhost:4200",
                // API Gateway/BFF in development (HTTP)
                "http://localhost:5189",
                // API Gateway/BFF in development (HTTPS)
                "https://localhost:7189"
            )
              // Allow all HTTP methods (GET, POST, PUT, DELETE, etc.)
              .AllowAnyMethod()
              // Allow all request headers
              .AllowAnyHeader()
              // Allow sending credentials (cookies, authorization tokens)
              .AllowCredentials();
    });
});

/* ================================================================
 * LOGGING CONFIGURATION
 * ================================================================ */

// Logging system configuration for diagnostics and debugging
builder.Logging.ClearProviders(); // Remove all default logging providers
builder.Logging.AddConsole();     // Add console logging for development
builder.Logging.AddDebug();       // Add debug logging for Visual Studio

/* ================================================================
 * APPLICATION BUILD
 * ================================================================ */

// Build the web application with all the configurations defined above
var app = builder.Build();

/* ================================================================
 * DATABASE INITIALIZATION
 * ================================================================ */

// Initialize the database in a separate scope
// This ensures resources are released correctly
using (var scope = app.Services.CreateScope())
{
    try
    {
        // Get the database context from the DI container
        var context = scope.ServiceProvider.GetRequiredService<ChatContext>();

        // Automatically create the database if it doesn't exist
        // Production will use more sophisticated migrations
        await context.Database.EnsureCreatedAsync();

        // Logger for recording initialization events
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Database initialized successfully");
    }
    catch (Exception ex)
    {
        // Error handling during initialization
        var errorLogger = app.Services.GetRequiredService<ILogger<Program>>();
        errorLogger.LogError(ex, "An error occurred while initializing the database or loading the models");
    }
}

/* ================================================================
 * DEVELOPMENT ENVIRONMENT CONFIGURATION
 * ================================================================ */

// Development-specific configuration
if (app.Environment.IsDevelopment())
{
    // Enable Scalar for modern API documentation
    app.MapOpenApi();
    app.MapScalarApiReference();
}

/* ================================================================
 * MIDDLEWARE PIPELINE
 * ================================================================ */

// Middleware to serve static files (widget JavaScript, CSS, images)
// Allows serving the chat widget and its associated resources
app.UseStaticFiles();

// Automatic HTTP-to-HTTPS redirection for security
app.UseHttpsRedirection();

// Apply the CORS policy configured above
app.UseCors();

// Authentication middleware - must precede authorization
// Reads and validates JWT tokens from requests
app.UseAuthentication();

// Authorization middleware - checks user permissions
// Verifies whether the authenticated user can access the requested resource
app.UseAuthorization();

// Health check endpoint
app.MapGet("/health", () => new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Version = "1.0.0",
    Service = "ChatbotService"
});

// Redirect from root to the admin dashboard for easy access in development
// Developers can navigate directly to localhost:port to reach the admin panel
app.MapGet("/", () => Results.Redirect("/api/admin"));

// Automatically map all controllers in the assembly
// Includes ChatController and SimpleAdminController
app.MapControllers();

/* ================================================================
 * APPLICATION STARTUP
 * ================================================================ */

// Logger for service startup messages
var appLogger = app.Services.GetRequiredService<ILogger<Program>>();

// Startup log with emoji to make logs easier to read
appLogger.LogInformation("🤖 ChatbotService started successfully");

// Admin dashboard URL, with a link specific to the environment
appLogger.LogInformation("📊 Admin Dashboard: {AdminUrl}",
    app.Environment.IsDevelopment() ? "http://localhost:5055/api/admin" : "/api/admin");

// Chat demo URL for quick testing
appLogger.LogInformation("🧪 Chat Demo: {DemoUrl}",
    app.Environment.IsDevelopment() ? "http://localhost:5055/api/chat/demo" : "/api/chat/demo");

// Widget demo URL for integration
appLogger.LogInformation("🎨 Widget Demo: {WidgetDemoUrl}",
    app.Environment.IsDevelopment() ? "http://localhost:5055/api/chatwidget/demo" : "/api/chatwidget/demo");

// Widget JavaScript URL for integration
appLogger.LogInformation("📦 Widget Script: {WidgetScriptUrl}",
    app.Environment.IsDevelopment() ? "http://localhost:5055/api/chatwidget/chat-widget.min.js" : "/api/chatwidget/chat-widget.min.js");

// Swagger API documentation URL
appLogger.LogInformation("📘 API Documentation: {SwaggerUrl}",
    app.Environment.IsDevelopment() ? "http://localhost:5055/swagger" : "/swagger");

// Asynchronously start the application
// Keeps the service running until a shutdown is requested
await app.RunAsync();

/* ================================================================
 * END OF PROGRAM
 * ================================================================
 *
 * ChatbotService is now fully configured and running.
 *
 * Available features:
 * ✅ Chat API with intent classification
 * ✅ JWT authentication with authorization
 * ✅ Admin dashboard
 * ✅ Integrated JavaScript chat widget
 * ✅ Dual routing (BFF and direct service)
 * ✅ Integration with other microservices
 * ✅ Health checks for monitoring
 * ✅ Complete Swagger documentation
 * ✅ Redis cache for performance
 * ✅ Structured logging for diagnostics
 *
 * For debugging:
 * - Use F5 in VS Code with the "🤖 Debug ChatbotService" configuration
 * - Or run: dotnet run --project src/ChatbotService
 * - For watch mode: dotnet watch --project src/ChatbotService
 *
 * Main endpoints:
 * - Chat API: POST /api/chat/message
 * - Authentication: POST /api/auth/login
 * - Admin Panel: GET /api/admin
 * - Health Check: GET /health
 * - Swagger UI: GET /swagger
 * - Widget Script: GET /api/chatwidget/chat-widget.min.js
 * - Widget Demo: GET /api/chatwidget/demo
 * - Widget Config: GET /api/chatwidget/config
 *
 * Widget integration:
 *
 * 1. BFF mode (for DistributedOrderSystem):
 * <script src="/api/chatwidget/chat-widget.min.js"></script>
 * <script>
 *   window.chatWidgetConfig = {
 *     useBffRouting: true,
 *     bffBaseUrl: '/api/gateway/chat',
 *     theme: 'light'
 *   };
 * </script>
 *
 * 2. Direct mode (for external projects):
 * <script src="https://your-chatbot-service.com/api/chatwidget/chat-widget.min.js"></script>
 * <script>
 *   window.chatWidgetConfig = {
 *     useBffRouting: false,
 *     chatbotServiceUrl: 'https://your-chatbot-service.com/api/chat',
 *     theme: 'dark'
 *   };
 * </script>
 *
 * Note: Some advanced services (the full ChatbotService and FineTuningService)
 * are temporarily commented out to keep the build clean during
 * development. They can be enabled gradually as the remaining
 * dependencies are implemented.
 */
