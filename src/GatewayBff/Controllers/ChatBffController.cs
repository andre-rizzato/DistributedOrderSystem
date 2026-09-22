namespace GatewayBff.Controllers;

using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// BFF proxy for the ChatbotService chat widget.
///
/// Why this exists: the widget served by ChatbotService (wwwroot/chat-widget/
/// dist/chat-widget.min.js) defaults to "BFF routing" mode — it calls
/// GatewayBff instead of talking to ChatbotService directly from the
/// browser. That's the same pattern already used for the cart and wishlist
/// (see CartBffController / WishlistBffController): the frontend only ever
/// needs to know GatewayBff's address, never the addresses of every
/// downstream microservice. Before this controller was added, GatewayBff had
/// no route at "api/gateway/chat" at all, so BFF-mode chat requests failed
/// with a 404 no matter what was wrong (or right) on the ChatbotService side.
///
/// This controller doesn't contain any chat logic itself — it's a thin
/// forwarder. Unlike CartBffController (which uses MediatR commands/queries
/// because it also has to talk to Redis/other services), this one just
/// relays the request body to ChatbotService and relays the response back
/// unchanged, so a plain HttpClient call is enough.
/// </summary>
[ApiController]
[Route("api/gateway/chat")]
[Produces("application/json")]
public class ChatBffController : ControllerBase
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<ChatBffController> _logger;

    public ChatBffController(IHttpClientFactory clients, ILogger<ChatBffController> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    /// <summary>
    /// GET api/gateway/chat/health
    ///
    /// The widget calls "{baseUrl}/health" once on page load to decide whether
    /// to show the bot status as "Online" or "Offline" (see connectToService()
    /// in chat-widget.min.js). We just forward that check to ChatbotService's
    /// own health endpoint and relay the status code and body as-is.
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        var client = _clients.CreateClient("ChatbotService");
        var response = await client.GetAsync("/api/chatwidget/health", ct);

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode);
        }

        return Content(await response.Content.ReadAsStringAsync(ct), "application/json");
    }

    /// <summary>
    /// POST api/gateway/chat/message
    ///
    /// The widget calls "{baseUrl}/message" every time the user sends a chat
    /// message (see sendToBackend() in chat-widget.min.js), with a JSON body
    /// like { message, sessionId, userId }. We don't need to know or validate
    /// that shape here — ChatbotService's ChatController already does that
    /// via its own ChatRequest model — so we accept the body as a generic
    /// `object` and pass it straight through. This keeps the BFF from having
    /// to be updated every time the chat request/response contract changes.
    /// </summary>
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] object request, CancellationToken ct)
    {
        var client = _clients.CreateClient("ChatbotService");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("/api/chat/message", request, ct);
        }
        catch (Exception ex)
        {
            // If ChatbotService is down or unreachable, tell the widget clearly
            // instead of letting the exception bubble up as a generic 500 —
            // "502 Bad Gateway" is the conventional HTTP status for "the
            // upstream server this proxy depends on didn't respond".
            _logger.LogError(ex, "Failed to reach ChatbotService");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "Chat service unavailable" });
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        return Content(body, "application/json", System.Text.Encoding.UTF8);
    }
}
