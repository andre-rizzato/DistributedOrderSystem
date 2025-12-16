namespace ChatbotService.Configuration;

public class ModelSettings
{
    public string BaseModelPath { get; set; } = "./Models/Downloaded";
    public string FineTunedModelPath { get; set; } = "./Models/FineTuned/chatbot_model.onnx";
    public string EmbeddingModelName { get; set; } = "sentence-transformers/all-MiniLM-L6-v2";
    public int MaxTokens { get; set; } = 150;
    public float Temperature { get; set; } = 0.7f;
    public float TopP { get; set; } = 0.9f;
    public string IntentClassifierModel { get; set; } = "microsoft/DialoGPT-small";
    public string EntityRecognitionModel { get; set; } = "dbmdz/bert-large-cased-finetuned-conll03-english";
}

public class ServiceUrlsSettings
{
    public string OrderService { get; set; } = "http://localhost:5003";
    public string ProductService { get; set; } = "http://localhost:5198";
    public string InventoryService { get; set; } = "http://localhost:5051";
    public string PaymentService { get; set; } = "http://localhost:5034";
    public string GatewayBff { get; set; } = "http://localhost:5189";
}

public class AuthSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public int TokenExpirationMinutes { get; set; } = 60;
}

public class FineTuningSettings
{
    public string TrainingDataPath { get; set; } = "./Data/TrainingData";
    public int MaxTrainingIterations { get; set; } = 1000;
    public float LearningRate { get; set; } = 0.001f;
    public int BatchSize { get; set; } = 16;
    public float ValidationSplit { get; set; } = 0.2f;
    public bool EnableEarlyStopping { get; set; } = true;
    public int EarlyStoppingPatience { get; set; } = 5;
}