namespace InventoryService.Messaging;

/// <summary>
/// Mock implementation of Kafka consumer for development when Kafka is not available
/// </summary>
public class MockOrderCreatedConsumer : BackgroundService
{
    private readonly ILogger<MockOrderCreatedConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MockOrderCreatedConsumer(ILogger<MockOrderCreatedConsumer> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consumer Kafka Mock inizializzato - in attesa di messaggi simulati");
        
        // Simulate processing orders in development
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Consumer Mock in attesa - Kafka non disponibile, usando implementazione di sviluppo");
            await Task.Delay(30000, stoppingToken); // Check every 30 seconds
        }
        
        _logger.LogInformation("Consumer Kafka Mock arrestato");
    }

    public override void Dispose()
    {
        _logger.LogInformation("Disposing Mock Consumer - nessuna risorsa da rilasciare");
        base.Dispose();
    }
}