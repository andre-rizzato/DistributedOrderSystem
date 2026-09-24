using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace ChatbotService.Controllers;

/// <summary>
/// Controller for serving the chat widget as a static resource
/// Provides the widget's JavaScript file and configuration resources
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatWidgetController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ChatWidgetController> _logger;

    public ChatWidgetController(IWebHostEnvironment environment, ILogger<ChatWidgetController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Serves the chat widget's JavaScript file
    /// Used both for BFF integration and for external projects
    /// </summary>
    /// <returns>Widget JavaScript file</returns>
    [HttpGet("chat-widget.min.js")]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour
    public IActionResult GetChatWidget()
    {
        try
        {
            var widgetPath = Path.Combine(_environment.WebRootPath, "chat-widget", "dist", "chat-widget.min.js");

            if (!System.IO.File.Exists(widgetPath))
            {
                _logger.LogError("Widget file not found: {WidgetPath}", widgetPath);
                return NotFound(new { error = "Widget unavailable" });
            }

            var widgetContent = System.IO.File.ReadAllText(widgetPath);

            return Content(widgetContent, "application/javascript");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving the chat widget");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Serves the widget demo page for testing and configuration
    /// </summary>
    /// <returns>Demo HTML page</returns>
    [HttpGet("demo")]
    [AllowAnonymous]
    public IActionResult GetDemo()
    {
        try
        {
            var demoPath = Path.Combine(_environment.WebRootPath, "chat-widget", "demo.html");

            if (!System.IO.File.Exists(demoPath))
            {
                _logger.LogError("Demo file not found: {DemoPath}", demoPath);
                return NotFound(new { error = "Demo unavailable" });
            }

            var demoContent = System.IO.File.ReadAllText(demoPath);

            return Content(demoContent, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving the widget demo");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Generates dynamic configuration for the widget based on the request parameters
    /// </summary>
    /// <param name="request">Widget configuration parameters</param>
    /// <returns>JavaScript configuration for the widget</returns>
    [HttpGet("config")]
    [AllowAnonymous]
    public IActionResult GetWidgetConfig([FromQuery] WidgetConfigRequest request)
    {
        try
        {
            var config = new
            {
                useBffRouting = request.UseBffRouting ?? true,
                bffBaseUrl = request.BffBaseUrl ?? "/api/gateway/chat",
                chatbotServiceUrl = request.ChatbotServiceUrl ?? Request.Scheme + "://" + Request.Host + "/api/chat",
                theme = request.Theme ?? "light",
                primaryColor = request.PrimaryColor ?? "#4299e1",
                secondaryColor = request.SecondaryColor ?? "#f7fafc",
                borderRadius = request.BorderRadius ?? "12px",
                position = request.Position ?? "bottom-right",
                autoOpen = request.AutoOpen ?? false,
                showTypingIndicator = request.ShowTypingIndicator ?? true,
                enableSoundNotifications = request.EnableSoundNotifications ?? false,
                maxMessages = request.MaxMessages ?? 100,
                botName = request.BotName ?? "AI Assistant",
                botAvatar = string.IsNullOrEmpty(request.BotAvatar) ? GetDefaultBotAvatar() : request.BotAvatar,
                welcomeMessage = request.WelcomeMessage ?? "Hi! How can I help you today?",
                placeholderText = request.PlaceholderText ?? "Type a message..."
            };

            var configScript = $@"
// Dynamically generated configuration for the Chat Widget
window.chatWidgetConfig = {System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true
            })};

// Auto-initialize if the widget is already loaded
if (window.DistributedChatWidget && !window.chatWidget) {{
    window.chatWidget = new DistributedChatWidget(window.chatWidgetConfig);
}}
";

            return Content(configScript, "application/javascript");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating the widget configuration");
            return StatusCode(500, new { error = "Error generating the configuration" });
        }
    }

    /// <summary>
    /// Endpoint to check the widget service's status
    /// Used by the widget to test the connection
    /// </summary>
    /// <returns>Service status</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult GetWidgetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            service = "ChatWidget",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            capabilities = new[]
            {
                "dual-routing", 
                "real-time-chat", 
                "ai-integration", 
                "customizable-themes",
                "responsive-design",
                "multi-language"
            }
        });
    }

    /// <summary>
    /// Generates a customized integration snippet for external projects
    /// </summary>
    /// <param name="request">Parameters for generating the snippet</param>
    /// <returns>HTML/JavaScript integration snippet</returns>
    [HttpPost("integration-snippet")]
    [AllowAnonymous]
    public IActionResult GenerateIntegrationSnippet([FromBody] IntegrationSnippetRequest request)
    {
        try
        {
            var baseUrl = request.UseBffRouting
                ? request.BaseUrl ?? Request.Scheme + "://" + Request.Host
                : request.ChatbotServiceUrl ?? Request.Scheme + "://" + Request.Host;

            var snippet = $@"
<!-- Chat Widget Integration -->
<!-- Add this snippet before the closing </body> tag -->
<script src=""{baseUrl}/api/chatwidget/chat-widget.min.js""></script>
<script>
  window.chatWidgetConfig = {{
    useBffRouting: {request.UseBffRouting.ToString().ToLower()},
    {(request.UseBffRouting ? $"bffBaseUrl: '{request.BffBaseUrl ?? "/api/gateway/chat"}'," : $"chatbotServiceUrl: '{request.ChatbotServiceUrl ?? baseUrl + "/api/chat"}',")}
    theme: '{request.Theme ?? "light"}',
    primaryColor: '{request.PrimaryColor ?? "#4299e1"}',
    position: '{request.Position ?? "bottom-right"}',
    autoOpen: {request.AutoOpen.ToString().ToLower()},
    botName: '{request.BotName ?? "AI Assistant"}',
    welcomeMessage: '{request.WelcomeMessage ?? "Hi! How can I help you today?"}'
  }};
</script>
";

            return Ok(new
            {
                snippet = snippet.Trim(),
                instructions = new[]
                {
                    "1. Copy the snippet above into your HTML file",
                    "2. Place it before the closing </body> tag",
                    "3. Adjust the configuration parameters to fit your needs",
                    request.UseBffRouting
                        ? "4. Make sure your BFF Gateway is configured to route requests to ChatbotService"
                        : "4. Verify that the ChatbotService URL is reachable from your frontend"
                },
                configUrl = $"{baseUrl}/api/chatwidget/config",
                demoUrl = $"{baseUrl}/api/chatwidget/demo",
                healthUrl = $"{baseUrl}/api/chatwidget/health"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating the integration snippet");
            return StatusCode(500, new { error = "Error generating the snippet" });
        }
    }

    /// <summary>
    /// Provides widget usage statistics (for administrators)
    /// </summary>
    /// <returns>Usage statistics</returns>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetWidgetStats()
    {
        try
        {
            // TODO: Implement real statistics collection from the database/cache
            var stats = new
            {
                totalDownloads = Random.Shared.Next(1000, 5000),
                activeInstances = Random.Shared.Next(50, 200),
                averageSessionDuration = TimeSpan.FromMinutes(Random.Shared.Next(3, 15)).ToString(),
                popularThemes = new[] { "light", "dark", "auto" },
                topPositions = new[] { "bottom-right", "bottom-left" },
                integrationMethods = new
                {
                    bffRouting = Random.Shared.Next(60, 80),
                    directService = Random.Shared.Next(20, 40)
                },
                lastWeekActivity = Enumerable.Range(0, 7)
                    .Select(i => new
                    {
                        date = DateTime.Today.AddDays(-i).ToString("yyyy-MM-dd"),
                        sessions = Random.Shared.Next(10, 100)
                    })
                    .ToArray()
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving widget statistics");
            return StatusCode(500, new { error = "Error retrieving statistics" });
        }
    }

    /// <summary>
    /// Generates a default bot avatar as SVG
    /// </summary>
    private static string GetDefaultBotAvatar()
    {
        var svg = @"<svg width='40' height='40' viewBox='0 0 40 40' fill='none' xmlns='http://www.w3.org/2000/svg'>
                      <circle cx='20' cy='20' r='20' fill='#4299e1'/>
                      <path d='M12 12H16V16H12V12ZM20 12H24V16H20V12ZM16 20H24V22H16V20Z' fill='white'/>
                    </svg>";
        
        return "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));
    }
}

/// <summary>
/// Model for the widget configuration request
/// </summary>
public class WidgetConfigRequest
{
    public bool? UseBffRouting { get; set; }
    public string? BffBaseUrl { get; set; }
    public string? ChatbotServiceUrl { get; set; }
    public string? Theme { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? BorderRadius { get; set; }
    public string? Position { get; set; }
    public bool? AutoOpen { get; set; }
    public bool? ShowTypingIndicator { get; set; }
    public bool? EnableSoundNotifications { get; set; }
    public int? MaxMessages { get; set; }
    public string? BotName { get; set; }
    public string? BotAvatar { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? PlaceholderText { get; set; }
}

/// <summary>
/// Model for the integration snippet request
/// </summary>
public class IntegrationSnippetRequest
{
    [Required]
    public bool UseBffRouting { get; set; }
    
    public string? BaseUrl { get; set; }
    public string? BffBaseUrl { get; set; }
    public string? ChatbotServiceUrl { get; set; }
    public string? Theme { get; set; } = "light";
    public string? PrimaryColor { get; set; } = "#4299e1";
    public string? Position { get; set; } = "bottom-right";
    public bool AutoOpen { get; set; } = false;
    public string? BotName { get; set; } = "AI Assistant";
    public string? WelcomeMessage { get; set; } = "Hi! How can I help you today?";
}