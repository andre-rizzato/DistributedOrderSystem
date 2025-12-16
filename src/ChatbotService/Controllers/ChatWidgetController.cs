using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

namespace ChatbotService.Controllers;

/// <summary>
/// Controller per servire il widget di chat come risorsa statica
/// Fornisce il file JavaScript del widget e le risorse di configurazione
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
    /// Serve il file JavaScript del widget di chat
    /// Utilizzato sia per integrazione BFF che per progetti esterni
    /// </summary>
    /// <returns>File JavaScript del widget</returns>
    [HttpGet("chat-widget.min.js")]
    [ResponseCache(Duration = 3600)] // Cache per 1 ora
    public IActionResult GetChatWidget()
    {
        try
        {
            var widgetPath = Path.Combine(_environment.WebRootPath, "chat-widget", "dist", "chat-widget.min.js");
            
            if (!System.IO.File.Exists(widgetPath))
            {
                _logger.LogError("File del widget non trovato: {WidgetPath}", widgetPath);
                return NotFound(new { error = "Widget non disponibile" });
            }

            var widgetContent = System.IO.File.ReadAllText(widgetPath);
            
            return Content(widgetContent, "application/javascript");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel servire il widget di chat");
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Serve la pagina demo del widget per test e configurazione
    /// </summary>
    /// <returns>Pagina HTML di demo</returns>
    [HttpGet("demo")]
    [AllowAnonymous]
    public IActionResult GetDemo()
    {
        try
        {
            var demoPath = Path.Combine(_environment.WebRootPath, "chat-widget", "demo.html");
            
            if (!System.IO.File.Exists(demoPath))
            {
                _logger.LogError("File demo non trovato: {DemoPath}", demoPath);
                return NotFound(new { error = "Demo non disponibile" });
            }

            var demoContent = System.IO.File.ReadAllText(demoPath);
            
            return Content(demoContent, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nel servire la demo del widget");
            return StatusCode(500, new { error = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Genera configurazione dinamica per il widget basata sui parametri della richiesta
    /// </summary>
    /// <param name="request">Parametri di configurazione del widget</param>
    /// <returns>Configurazione JavaScript per il widget</returns>
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
                botName = request.BotName ?? "Assistente AI",
                botAvatar = string.IsNullOrEmpty(request.BotAvatar) ? GetDefaultBotAvatar() : request.BotAvatar,
                welcomeMessage = request.WelcomeMessage ?? "Ciao! Come posso aiutarti oggi?",
                placeholderText = request.PlaceholderText ?? "Scrivi un messaggio..."
            };

            var configScript = $@"
// Configurazione generata dinamicamente per il Widget di Chat
window.chatWidgetConfig = {System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions 
            { 
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                WriteIndented = true 
            })};

// Auto-inizializzazione se il widget è già caricato
if (window.DistributedChatWidget && !window.chatWidget) {{
    window.chatWidget = new DistributedChatWidget(window.chatWidgetConfig);
}}
";

            return Content(configScript, "application/javascript");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella generazione della configurazione widget");
            return StatusCode(500, new { error = "Errore nella configurazione" });
        }
    }

    /// <summary>
    /// Endpoint per verificare lo stato del servizio widget
    /// Utilizzato dal widget per testare la connessione
    /// </summary>
    /// <returns>Stato del servizio</returns>
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
    /// Genera snippet di integrazione personalizzato per progetti esterni
    /// </summary>
    /// <param name="request">Parametri per la generazione dello snippet</param>
    /// <returns>Snippet HTML/JavaScript per l'integrazione</returns>
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
<!-- Aggiungi questo snippet prima della chiusura del tag </body> -->
<script src=""{baseUrl}/api/chatwidget/chat-widget.min.js""></script>
<script>
  window.chatWidgetConfig = {{
    useBffRouting: {request.UseBffRouting.ToString().ToLower()},
    {(request.UseBffRouting ? $"bffBaseUrl: '{request.BffBaseUrl ?? "/api/gateway/chat"}'," : $"chatbotServiceUrl: '{request.ChatbotServiceUrl ?? baseUrl + "/api/chat"}',")}
    theme: '{request.Theme ?? "light"}',
    primaryColor: '{request.PrimaryColor ?? "#4299e1"}',
    position: '{request.Position ?? "bottom-right"}',
    autoOpen: {request.AutoOpen.ToString().ToLower()},
    botName: '{request.BotName ?? "Assistente AI"}',
    welcomeMessage: '{request.WelcomeMessage ?? "Ciao! Come posso aiutarti oggi?"}'
  }};
</script>
";

            return Ok(new
            {
                snippet = snippet.Trim(),
                instructions = new[]
                {
                    "1. Copia lo snippet sopra nel tuo file HTML",
                    "2. Posizionalo prima della chiusura del tag </body>",
                    "3. Modifica i parametri di configurazione secondo le tue esigenze",
                    request.UseBffRouting 
                        ? "4. Assicurati che il tuo BFF Gateway sia configurato per instradare le richieste al ChatbotService"
                        : "4. Verifica che l'URL del ChatbotService sia raggiungibile dal tuo frontend"
                },
                configUrl = $"{baseUrl}/api/chatwidget/config",
                demoUrl = $"{baseUrl}/api/chatwidget/demo",
                healthUrl = $"{baseUrl}/api/chatwidget/health"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore nella generazione dello snippet di integrazione");
            return StatusCode(500, new { error = "Errore nella generazione dello snippet" });
        }
    }

    /// <summary>
    /// Fornisce statistiche di utilizzo del widget (per amministratori)
    /// </summary>
    /// <returns>Statistiche di utilizzo</returns>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetWidgetStats()
    {
        try
        {
            // TODO: Implementare raccolta statistiche reali da database/cache
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
            _logger.LogError(ex, "Errore nel recupero delle statistiche widget");
            return StatusCode(500, new { error = "Errore nel recupero delle statistiche" });
        }
    }

    /// <summary>
    /// Genera avatar bot predefinito in SVG
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
/// Modello per la richiesta di configurazione del widget
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
/// Modello per la richiesta di snippet di integrazione
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
    public string? BotName { get; set; } = "Assistente AI";
    public string? WelcomeMessage { get; set; } = "Ciao! Come posso aiutarti oggi?";
}