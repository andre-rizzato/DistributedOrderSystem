namespace ChatbotService.Services.Interfaces;

using ChatbotService.Models;

public interface IChatbotService
{
    Task<ChatResponse> ProcessMessageAsync(ChatRequest request, UserContext? userContext = null);
    Task<string> GenerateResponseAsync(Intent intent, UserContext? userContext, Dictionary<string, string> parameters);
    Task SaveChatHistoryAsync(string sessionId, string userMessage, string botResponse, string intent, float confidence, string? userId = null);
    Task<List<ChatMessage>> GetChatHistoryAsync(string sessionId, int limit = 10);
    Task<ChatSession> CreateOrGetSessionAsync(string? sessionId, UserContext? userContext);
    Task<ChatHistoryResponse> GetChatHistoryAsync(string sessionId);
    Task<string> CreateSessionAsync();
    Task EndSessionAsync(string sessionId);
    Task<List<string>> GetSupportedIntentsAsync();
    Task StoreFeedbackAsync(FeedbackRequest feedback);
}