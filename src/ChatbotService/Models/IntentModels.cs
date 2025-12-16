namespace ChatbotService.Models;

public record Intent
{
    public string Type { get; init; } = string.Empty;
    public float Confidence { get; init; }
    public Dictionary<string, string> Parameters { get; init; } = new();
    public List<string> Entities { get; init; } = new();
}

public record NLPResponse
{
    public Intent Intent { get; init; } = new();
    public string SentimentScore { get; init; } = "neutral";
    public bool RequiresHumanEscalation { get; init; }
    public float[] EmbeddingVector { get; init; } = Array.Empty<float>();
}

// Supported intents
public static class IntentTypes
{
    public const string ORDER_STATUS = "order_status";
    public const string PAYMENT_INFO = "payment_info";
    public const string CANCEL_ORDER = "cancel_order";
    public const string PRODUCT_SEARCH = "product_search";
    public const string INVENTORY_CHECK = "inventory_check";
    public const string COMPLAINT = "complaint";
    public const string CREATE_ORDER = "create_order";
    public const string HUMAN_AGENT = "human_agent";
    public const string GREETING = "greeting";
    public const string GOODBYE = "goodbye";
    public const string HELP = "help";
    public const string UNKNOWN = "unknown";
}

public record EntityExtractionResult
{
    public string EntityType { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public int StartIndex { get; init; }
    public int EndIndex { get; init; }
    public float Confidence { get; init; }
}