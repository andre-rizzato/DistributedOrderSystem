namespace ChatbotService.Services;

using ChatbotService.Models;
using ChatbotService.Services.Interfaces;
using ChatbotService.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Tokenizers;
using System.Text.Json;
using System.Text.RegularExpressions;

public class LightweightNLPService : INLPService
{
    private readonly ModelSettings _modelSettings;
    private readonly ILogger<LightweightNLPService> _logger;
    private readonly MLContext _mlContext;
    private ITransformer? _intentModel;
    private PredictionEngine<IntentInput, IntentOutput>? _predictionEngine;
    private readonly Dictionary<string, float[]> _intentEmbeddings;
    private bool _isModelLoaded;

    public LightweightNLPService(
        IOptions<ModelSettings> modelSettings,
        ILogger<LightweightNLPService> logger)
    {
        _modelSettings = modelSettings.Value;
        _logger = logger;
        _mlContext = new MLContext(seed: 42);
        _intentEmbeddings = new Dictionary<string, float[]>();
        _isModelLoaded = false;

        // Initialize with pre-computed intent embeddings
        InitializeIntentEmbeddings();
    }

    public bool IsModelLoaded => _isModelLoaded;

    public async Task<NLPResponse> AnalyzeMessageAsync(string message)
    {
        try
        {
            var intent = await ClassifyIntentAsync(message);
            var entities = await ExtractEntitiesAsync(message);
            var sentiment = await AnalyzeSentimentAsync(message);
            var embedding = await GetEmbeddingAsync(message);

            var entityDict = entities.ToDictionary(e => e.EntityType, e => e.Value);
            intent.Parameters.Add("original_message", message);
            
            foreach (var entity in entities)
            {
                intent.Parameters[entity.EntityType] = entity.Value;
            }

            var needsEscalation = DetermineEscalationNeed(sentiment, message, intent.Confidence);

            return new NLPResponse
            {
                Intent = intent,
                SentimentScore = sentiment,
                RequiresHumanEscalation = needsEscalation,
                EmbeddingVector = embedding
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing message: {Message}", message);
            
            return new NLPResponse
            {
                Intent = new Intent
                {
                    Type = IntentTypes.GREETING,
                    Confidence = 0.3f,
                    Parameters = new Dictionary<string, string> { { "original_message", message } }
                },
                SentimentScore = "neutral"
            };
        }
    }

    public async Task<Intent> ClassifyIntentAsync(string message)
    {
        var lowerMessage = message.ToLower();
        var parameters = new Dictionary<string, string>();

        // Fast rule-based classification for common patterns
        var ruleBasedIntent = ClassifyWithRules(message, parameters);
        if (ruleBasedIntent.Confidence > 0.8f)
        {
            return ruleBasedIntent;
        }

        // Use ML model if loaded, otherwise use embedding similarity
        if (_predictionEngine != null)
        {
            return await ClassifyWithMLModel(message, parameters);
        }
        else
        {
            return await ClassifyWithEmbeddingSimilarity(message, parameters);
        }
    }

    private Intent ClassifyWithRules(string message, Dictionary<string, string> parameters)
    {
        var lowerMessage = message.ToLower();
        
        // Greeting patterns
        if (ContainsPattern(lowerMessage, new[] { "ciao", "salve", "buongiorno", "buonasera", "hello", "hi" }))
        {
            return new Intent { Type = IntentTypes.GREETING, Confidence = 0.95f, Parameters = parameters };
        }

        // Order status patterns
        if (ContainsPattern(lowerMessage, new[] { "stato", "ordine", "order", "spedizione", "consegna", "tracking" }))
        {
            ExtractOrderId(message, parameters);
            return new Intent { Type = IntentTypes.ORDER_STATUS, Confidence = 0.90f, Parameters = parameters };
        }

        // Product search patterns
        if (ContainsPattern(lowerMessage, new[] { "cerca", "search", "prodotto", "product", "voglio", "comprare", "mostra" }))
        {
            ExtractProductName(message, parameters);
            return new Intent { Type = IntentTypes.PRODUCT_SEARCH, Confidence = 0.85f, Parameters = parameters };
        }

        // Cancellation patterns
        if (ContainsPattern(lowerMessage, new[] { "cancella", "cancel", "annulla", "rimuovi", "elimina" }))
        {
            ExtractOrderId(message, parameters);
            return new Intent { Type = IntentTypes.CANCEL_ORDER, Confidence = 0.88f, Parameters = parameters };
        }

        // Payment patterns
        if (ContainsPattern(lowerMessage, new[] { "pagamento", "payment", "carta", "credit", "fattura", "ricevuta" }))
        {
            return new Intent { Type = IntentTypes.PAYMENT_INFO, Confidence = 0.82f, Parameters = parameters };
        }

        // Help patterns
        if (ContainsPattern(lowerMessage, new[] { "aiuto", "help", "supporto", "assistenza", "come" }))
        {
            return new Intent { Type = IntentTypes.HELP, Confidence = 0.80f, Parameters = parameters };
        }

        // Goodbye patterns
        if (ContainsPattern(lowerMessage, new[] { "arrivederci", "bye", "ciao", "addio", "grazie e basta" }))
        {
            return new Intent { Type = IntentTypes.GOODBYE, Confidence = 0.75f, Parameters = parameters };
        }

        return new Intent { Type = IntentTypes.UNKNOWN, Confidence = 0.3f, Parameters = parameters };
    }

    private async Task<Intent> ClassifyWithEmbeddingSimilarity(string message, Dictionary<string, string> parameters)
    {
        try
        {
            var messageEmbedding = await GetEmbeddingAsync(message);
            var bestIntent = IntentTypes.UNKNOWN;
            var bestSimilarity = 0f;

            foreach (var kvp in _intentEmbeddings)
            {
                var similarity = CosineSimilarity(messageEmbedding, kvp.Value);
                if (similarity > bestSimilarity)
                {
                    bestSimilarity = similarity;
                    bestIntent = kvp.Key;
                }
            }

            return new Intent
            {
                Type = bestIntent,
                Confidence = bestSimilarity,
                Parameters = parameters
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in embedding similarity classification");
            return new Intent { Type = IntentTypes.UNKNOWN, Confidence = 0.2f, Parameters = parameters };
        }
    }

    private async Task<Intent> ClassifyWithMLModel(string message, Dictionary<string, string> parameters)
    {
        try
        {
            var input = new IntentInput { Text = message };
            var prediction = _predictionEngine!.Predict(input);

            return new Intent
            {
                Type = prediction.Intent ?? IntentTypes.UNKNOWN,
                Confidence = prediction.Confidence,
                Parameters = parameters
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error with ML model prediction");
            return new Intent { Type = IntentTypes.UNKNOWN, Confidence = 0.2f, Parameters = parameters };
        }
    }

    public async Task<List<EntityExtractionResult>> ExtractEntitiesAsync(string message)
    {
        var entities = new List<EntityExtractionResult>();

        // Extract order IDs
        var orderMatches = Regex.Matches(message, @"\b(?:ordine|order)\s*#?(\d+)\b", RegexOptions.IgnoreCase);
        foreach (Match match in orderMatches)
        {
            entities.Add(new EntityExtractionResult
            {
                EntityType = "order_id",
                Value = match.Groups[1].Value,
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length,
                Confidence = 0.95f
            });
        }

        // Extract product names (simple pattern)
        var productMatches = Regex.Matches(message, @"\b(?:cerca|voglio|comprare)\s+(.{2,30}?)(?:\s|$|\?|\.|!)", RegexOptions.IgnoreCase);
        foreach (Match match in productMatches)
        {
            entities.Add(new EntityExtractionResult
            {
                EntityType = "product_name",
                Value = match.Groups[1].Value.Trim(),
                StartIndex = match.Groups[1].Index,
                EndIndex = match.Groups[1].Index + match.Groups[1].Length,
                Confidence = 0.80f
            });
        }

        // Extract phone numbers
        var phoneMatches = Regex.Matches(message, @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.IgnoreCase);
        foreach (Match match in phoneMatches)
        {
            entities.Add(new EntityExtractionResult
            {
                EntityType = "phone_number",
                Value = match.Value,
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length,
                Confidence = 0.90f
            });
        }

        // Extract email addresses
        var emailMatches = Regex.Matches(message, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.IgnoreCase);
        foreach (Match match in emailMatches)
        {
            entities.Add(new EntityExtractionResult
            {
                EntityType = "email",
                Value = match.Value,
                StartIndex = match.Index,
                EndIndex = match.Index + match.Length,
                Confidence = 0.95f
            });
        }

        return entities;
    }

    public async Task<string> AnalyzeSentimentAsync(string message)
    {
        // Lightweight sentiment analysis using word lists
        var positiveWords = new[] { "bene", "ottimo", "perfetto", "grazie", "fantastico", "eccellente", "soddisfatto", "felice", "buono" };
        var negativeWords = new[] { "problema", "errore", "sbagliato", "male", "pessimo", "brutto", "arrabbiato", "insoddisfatto", "deluso", "frustrante" };

        var lowerMessage = message.ToLower();
        var positiveScore = positiveWords.Count(word => lowerMessage.Contains(word));
        var negativeScore = negativeWords.Count(word => lowerMessage.Contains(word));

        if (negativeScore > positiveScore && negativeScore > 0)
            return "negative";
        else if (positiveScore > negativeScore && positiveScore > 0)
            return "positive";
        else
            return "neutral";
    }

    public async Task<string> GenerateResponseAsync(string message, string intent, Dictionary<string, string> parameters)
    {
        // Simple template-based response generation
        return intent switch
        {
            IntentTypes.GREETING => GetRandomResponse(new[]
            {
                "Ciao! 👋 Come posso aiutarti oggi?",
                "Salve! Sono qui per aiutarti. Cosa posso fare per te?",
                "Buongiorno! Come posso essere utile?"
            }),
            IntentTypes.ORDER_STATUS => "Sto verificando lo stato del tuo ordine. Un momento per favore...",
            IntentTypes.PRODUCT_SEARCH => $"Sto cercando prodotti per '{parameters.GetValueOrDefault("product_name", "la tua ricerca")}'...",
            IntentTypes.CANCEL_ORDER => "Mi dispiace che tu voglia cancellare l'ordine. Sto verificando se è possibile...",
            IntentTypes.PAYMENT_INFO => "Sto recuperando le informazioni di pagamento...",
            IntentTypes.GOODBYE => GetRandomResponse(new[]
            {
                "Arrivederci! 👋 Grazie per aver usato il nostro servizio!",
                "Buona giornata! Se hai bisogno di altro aiuto, non esitare a contattarci!",
                "Ciao! Alla prossima! 😊"
            }),
            IntentTypes.HELP => "Ecco cosa posso fare per te:\n• 📦 Verificare lo stato degli ordini\n• 🔍 Cercare prodotti\n• 💳 Fornire informazioni sui pagamenti\n• ❌ Aiutarti con le cancellazioni",
            _ => "Mi dispiace, non ho capito bene. Puoi riformulare la domanda?"
        };
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        // Simple word-based embedding (for demonstration)
        // In production, use a real embedding model
        var words = text.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var embedding = new float[384]; // MiniLM dimension

        var hash = text.GetHashCode();
        var random = new Random(Math.Abs(hash));
        
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        // Normalize
        var norm = Math.Sqrt(embedding.Sum(x => x * x));
        if (norm > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= (float)norm;
            }
        }

        return embedding;
    }

    public async Task<bool> LoadModelAsync(string modelPath)
    {
        try
        {
            if (File.Exists(modelPath))
            {
                _intentModel = _mlContext.Model.Load(modelPath, out var modelSchema);
                _predictionEngine = _mlContext.Model.CreatePredictionEngine<IntentInput, IntentOutput>(_intentModel);
                _isModelLoaded = true;
                
                _logger.LogInformation("Successfully loaded ML model from {ModelPath}", modelPath);
                return true;
            }
            else
            {
                _logger.LogWarning("Model file not found at {ModelPath}", modelPath);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load model from {ModelPath}", modelPath);
            return false;
        }
    }

    private void InitializeIntentEmbeddings()
    {
        // Pre-computed embeddings for each intent type (simplified)
        _intentEmbeddings[IntentTypes.GREETING] = GenerateSyntheticEmbedding("ciao salve buongiorno");
        _intentEmbeddings[IntentTypes.ORDER_STATUS] = GenerateSyntheticEmbedding("ordine stato spedizione");
        _intentEmbeddings[IntentTypes.PRODUCT_SEARCH] = GenerateSyntheticEmbedding("cerca prodotto comprare");
        _intentEmbeddings[IntentTypes.CANCEL_ORDER] = GenerateSyntheticEmbedding("cancella annulla ordine");
        _intentEmbeddings[IntentTypes.PAYMENT_INFO] = GenerateSyntheticEmbedding("pagamento carta fattura");
        _intentEmbeddings[IntentTypes.HELP] = GenerateSyntheticEmbedding("aiuto supporto assistenza");
        _intentEmbeddings[IntentTypes.GOODBYE] = GenerateSyntheticEmbedding("arrivederci ciao bye");
    }

    private static float[] GenerateSyntheticEmbedding(string text)
    {
        var embedding = new float[384];
        var hash = text.GetHashCode();
        var random = new Random(Math.Abs(hash));
        
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        // Normalize
        var norm = Math.Sqrt(embedding.Sum(x => x * x));
        if (norm > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
            {
                embedding[i] /= (float)norm;
            }
        }

        return embedding;
    }

    private static bool ContainsPattern(string message, string[] patterns)
    {
        return patterns.Any(pattern => message.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    private static void ExtractOrderId(string message, Dictionary<string, string> parameters)
    {
        var match = Regex.Match(message, @"\b(?:ordine|order)\s*#?(\d+)\b", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            parameters["order_id"] = match.Groups[1].Value;
        }
    }

    private static void ExtractProductName(string message, Dictionary<string, string> parameters)
    {
        var match = Regex.Match(message, @"\b(?:cerca|voglio|comprare|acquistare|mostra)\s+(.{2,30}?)(?:\s|$|\?|\.|!)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            parameters["product_name"] = match.Groups[1].Value.Trim();
        }
    }

    private static float CosineSimilarity(float[] vector1, float[] vector2)
    {
        if (vector1.Length != vector2.Length)
            return 0f;

        var dotProduct = 0f;
        var norm1 = 0f;
        var norm2 = 0f;

        for (int i = 0; i < vector1.Length; i++)
        {
            dotProduct += vector1[i] * vector2[i];
            norm1 += vector1[i] * vector1[i];
            norm2 += vector2[i] * vector2[i];
        }

        var denominator = Math.Sqrt(norm1) * Math.Sqrt(norm2);
        return denominator > 0 ? dotProduct / (float)denominator : 0f;
    }

    private static bool DetermineEscalationNeed(string sentiment, string message, float confidence)
    {
        var escalationKeywords = new[] { "manager", "responsabile", "reclamo", "rimborso", "avvocato", "inaccettabile", "scandaloso" };
        var lowerMessage = message.ToLower();
        
        return (sentiment == "negative" && confidence > 0.7f) || 
               escalationKeywords.Any(keyword => lowerMessage.Contains(keyword));
    }

    private static string GetRandomResponse(string[] responses)
    {
        var random = new Random();
        return responses[random.Next(responses.Length)];
    }
}

// ML.NET classes for intent classification
public class IntentInput
{
    [LoadColumn(0)]
    public string Text { get; set; } = string.Empty;
}

public class IntentOutput
{
    [ColumnName("PredictedLabel")]
    public string? Intent { get; set; }

    [ColumnName("Score")]
    public float[] Scores { get; set; } = Array.Empty<float>();

    public float Confidence => Scores.Max();
}