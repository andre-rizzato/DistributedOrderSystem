namespace ChatbotService.Services.Interfaces;

using ChatbotService.Models;

public interface INLPService
{
    Task<NLPResponse> AnalyzeMessageAsync(string message);
    Task<Intent> ClassifyIntentAsync(string message);
    Task<List<EntityExtractionResult>> ExtractEntitiesAsync(string message);
    Task<string> AnalyzeSentimentAsync(string message);
    Task<string> GenerateResponseAsync(string message, string intent, Dictionary<string, string> parameters);
    Task<float[]> GetEmbeddingAsync(string text);
    Task<bool> LoadModelAsync(string modelPath);
    bool IsModelLoaded { get; }
}