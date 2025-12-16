namespace ChatbotService.Models;

public record ChatRequest
{
    public string Message { get; init; } = string.Empty;
    public string? SessionId { get; init; }
    public string? Context { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}

public record ChatResponse
{
    public string Message { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public List<QuickAction> SuggestedActions { get; init; } = new();
    public bool RequiresAuthentication { get; init; }
    public string? RedirectUrl { get; init; }
    public float Confidence { get; init; }
    public string Intent { get; init; } = string.Empty;
}

public record QuickAction
{
    public string Text { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public Dictionary<string, object> Parameters { get; init; } = new();
}

public record UserContext
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public Dictionary<string, object> Properties { get; init; } = new();
}

public record TrainingExample
{
    public string Input { get; init; } = string.Empty;
    public string ExpectedOutput { get; init; } = string.Empty;
    public string Intent { get; init; } = string.Empty;
    public Dictionary<string, string> Entities { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string? UserId { get; init; }
}

public record ChatSession
{
    public string SessionId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime LastActivity { get; init; } = DateTime.UtcNow;
    public Dictionary<string, object> Context { get; init; } = new();
    public bool IsActive { get; init; } = true;
}

public record ChatMessage
{
    public string Id { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public bool IsFromUser { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string Intent { get; init; } = string.Empty;
    public float Confidence { get; init; }
}

public record FeedbackRequest
{
    public string SessionId { get; init; } = string.Empty;
    public string MessageId { get; init; } = string.Empty;
    public bool IsHelpful { get; init; }
    public string? Comments { get; init; }
    public int Rating { get; init; }
}

public record SessionResponse
{
    public string SessionId { get; init; } = string.Empty;
}

public record ChatHistoryResponse
{
    public string SessionId { get; init; } = string.Empty;
    public List<ChatMessage> Messages { get; init; } = new();
}