namespace ChatbotService.Services;

using ChatbotService.Services.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Background service for initializing AI models on startup
/// Downloads and prepares DialoGPT model for conversational AI
/// </summary>
public class ModelInitializationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModelInitializationService> _logger;

    public ModelInitializationService(
        IServiceProvider serviceProvider,
        ILogger<ModelInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Execute model initialization in background
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("🤖 Starting AI Model Initialization Service...");
            
            // Wait a bit for the application to fully start
            await Task.Delay(3000, stoppingToken);
            
            using var scope = _serviceProvider.CreateScope();
            
            // Get the NLP service and try to download/load the model
            var nlpService = scope.ServiceProvider.GetService<INLPService>();
            
            if (nlpService is AdvancedNLPService advancedNLP)
            {
                _logger.LogInformation("📥 Downloading DialoGPT-small model from Hugging Face...");
                
                var success = await advancedNLP.DownloadAndLoadModelAsync();
                
                if (success)
                {
                    _logger.LogInformation("✅ DialoGPT model loaded successfully! The chatbot is now AI-powered.");
                }
                else
                {
                    _logger.LogWarning("⚠️ DialoGPT model loading failed. Chatbot will use rule-based responses.");
                }
            }
            else
            {
                _logger.LogInformation("ℹ️ Using lightweight NLP service without AI model download.");
            }
            
            _logger.LogInformation("🚀 Model Initialization Service completed!");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Model initialization was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during model initialization");
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔄 Model Initialization Service is starting...");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔴 Model Initialization Service is stopping...");
        await base.StopAsync(cancellationToken);
    }
}