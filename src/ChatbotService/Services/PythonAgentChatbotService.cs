namespace ChatbotService.Services;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ChatbotService.Data;
using ChatbotService.Models;
using ChatbotService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using ChatEntity = ChatbotService.Data.Entities.ChatMessage;
using SessionEntity = ChatbotService.Data.Entities.ChatSession;

/// <summary>
/// Real implementation of IChatbotService - HTTP bridge to AgentService
/// (Python, LangGraph). Replaces the previous registration of this
/// interface, which was commented out in Program.cs because there was
/// never a real implementation behind ChatController.
/// </summary>
public class PythonAgentChatbotService : IChatbotService
{
    private readonly HttpClient _httpClient;
    private readonly ChatContext _db;
    private readonly ILogger<PythonAgentChatbotService> _logger;

    private static readonly List<string> SupportedIntents = new()
    {
        "create_order", "cancel_order", "update_order", "get_info", "general_question",
    };

    public PythonAgentChatbotService(HttpClient httpClient, ChatContext db, ILogger<PythonAgentChatbotService> logger)
    {
        _httpClient = httpClient;
        _db = db;
        _logger = logger;
    }

    public async Task<ChatResponse> ProcessMessageAsync(ChatRequest request, UserContext? userContext = null)
    {
        var session = await CreateOrGetSessionAsync(request.SessionId, userContext);

        AgentResponseDto? agentResponse;
        try
        {
            var httpResponse = await _httpClient.PostAsJsonAsync(
                "/agent/message",
                new AgentRequestDto { Message = request.Message, SessionId = session.SessionId });
            httpResponse.EnsureSuccessStatusCode();
            agentResponse = await httpResponse.Content.ReadFromJsonAsync<AgentResponseDto>();
        }
        catch (Exception ex)
        {
            // If the external call fails, don't make up a response -
            // return an explicit error, never a fake "success".
            _logger.LogError(ex, "Failed to call AgentService for session {SessionId}", session.SessionId);
            return new ChatResponse
            {
                Message = "Sorry, the assistant is currently unavailable. Please try again shortly.",
                SessionId = session.SessionId,
                Intent = "error",
                Confidence = 0,
            };
        }

        if (agentResponse is null)
        {
            _logger.LogError("AgentService returned an empty body for session {SessionId}", session.SessionId);
            return new ChatResponse
            {
                Message = "Sorry, I couldn't process your message right now.",
                SessionId = session.SessionId,
                Intent = "error",
                Confidence = 0,
            };
        }

        await SaveChatHistoryAsync(
            session.SessionId, request.Message, agentResponse.Reply,
            agentResponse.Intent ?? "unknown", agentResponse.Confidence ?? 0, userContext?.UserId);

        return new ChatResponse
        {
            Message = agentResponse.Reply,
            SessionId = session.SessionId,
            Intent = agentResponse.Intent ?? "unknown",
            Confidence = agentResponse.Confidence ?? 0,
        };
    }

    public Task<string> GenerateResponseAsync(Intent intent, UserContext? userContext, Dictionary<string, string> parameters)
    {
        // Outside the main path (ChatController only calls ProcessMessageAsync) -
        // deterministic implementation with no external call, just to satisfy the interface.
        return Task.FromResult($"Detected intent: {intent.Type} (confidence {intent.Confidence:P0}).");
    }

    public async Task SaveChatHistoryAsync(string sessionId, string userMessage, string botResponse, string intent, float confidence, string? userId = null)
    {
        _db.ChatMessages.Add(new ChatEntity
        {
            SessionId = sessionId,
            UserId = userId,
            UserMessage = userMessage,
            BotResponse = botResponse,
            Intent = intent,
            Confidence = confidence,
            CreatedAt = DateTime.UtcNow,
        });

        var session = await _db.ChatSessions.FindAsync(sessionId);
        if (session is not null)
        {
            session.LastInteractionAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<ChatMessage>> GetChatHistoryAsync(string sessionId, int limit = 10)
    {
        var messages = await _db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatMessage
            {
                Id = m.Id.ToString(),
                SessionId = m.SessionId,
                Message = m.UserMessage,
                IsFromUser = true,
                Timestamp = m.CreatedAt,
                Intent = m.Intent,
                Confidence = m.Confidence,
            })
            .ToList();
    }

    public async Task<ChatSession> CreateOrGetSessionAsync(string? sessionId, UserContext? userContext)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var existingSession = await _db.ChatSessions.FindAsync(sessionId);
            if (existingSession is not null)
            {
                return MapToModel(existingSession);
            }
        }

        var newSession = new SessionEntity
        {
            Id = sessionId ?? Guid.NewGuid().ToString(),
            UserId = userContext?.UserId,
            UserEmail = userContext?.Email,
            CreatedAt = DateTime.UtcNow,
            LastInteractionAt = DateTime.UtcNow,
            IsAuthenticated = userContext is not null,
        };

        _db.ChatSessions.Add(newSession);
        await _db.SaveChangesAsync();
        return MapToModel(newSession);
    }

    public async Task<ChatHistoryResponse> GetChatHistoryAsync(string sessionId)
    {
        var messages = await GetChatHistoryAsync(sessionId, limit: 50);
        return new ChatHistoryResponse { SessionId = sessionId, Messages = messages };
    }

    public async Task<string> CreateSessionAsync()
    {
        var session = await CreateOrGetSessionAsync(null, null);
        return session.SessionId;
    }

    public async Task EndSessionAsync(string sessionId)
    {
        var session = await _db.ChatSessions.FindAsync(sessionId);
        if (session is null)
        {
            return;
        }

        session.Status = "Ended";
        await _db.SaveChangesAsync();
    }

    public Task<List<string>> GetSupportedIntentsAsync() => Task.FromResult(SupportedIntents);

    public Task StoreFeedbackAsync(FeedbackRequest feedback)
    {
        // There's no feedback table in ChatContext yet - log it for now
        // instead of creating a new migration outside the scope of this increment.
        _logger.LogInformation(
            "Feedback received - session {SessionId}, message {MessageId}, helpful={IsHelpful}, rating={Rating}",
            feedback.SessionId, feedback.MessageId, feedback.IsHelpful, feedback.Rating);
        return Task.CompletedTask;
    }

    private static ChatSession MapToModel(SessionEntity entity) => new()
    {
        SessionId = entity.Id,
        UserId = entity.UserId ?? string.Empty,
        CreatedAt = entity.CreatedAt,
        LastActivity = entity.LastInteractionAt,
        IsActive = entity.Status == "Active",
    };

    private record AgentRequestDto
    {
        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;

        [JsonPropertyName("session_id")]
        public string SessionId { get; init; } = string.Empty;
    }

    private record AgentResponseDto
    {
        [JsonPropertyName("reply")]
        public string Reply { get; init; } = string.Empty;

        [JsonPropertyName("intent")]
        public string? Intent { get; init; }

        [JsonPropertyName("confidence")]
        public float? Confidence { get; init; }

        [JsonPropertyName("order_id")]
        public string? OrderId { get; init; }
    }
}
