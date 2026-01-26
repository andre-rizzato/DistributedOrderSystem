namespace InventoryService.Messaging;

using Confluent.Kafka;
using InventoryService.Services.Interfaces;
using Shared.Messages;
using System.Text.Json;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly string _topic;
    private bool _disposed = false;

    public OrderCreatedConsumer(
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        ILogger<OrderCreatedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _topic = configuration["Kafka:OrderCreatedTopic"] ?? "order-created";
        var groupId = configuration["Kafka:ConsumerGroupId"] ?? "inventory-service";

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        
        _logger.LogInformation(
            "Kafka consumer initialized for topic {Topic} with group {GroupId} at {BootstrapServers}",
            _topic, groupId, bootstrapServers);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topic);
        try
        {
            _logger.LogInformation("Avvio consumer Kafka per topic: {Topic}", _topic);
            // Aggiungi un delay per permettere all'host di avviarsi completamente
            await Task.Delay(2000, stoppingToken);
        
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult?.Message == null)
                        continue;

                    _logger.LogInformation(
                        "Ricevuto messaggio dalla partizione {Partition} all'offset {Offset}",
                        consumeResult.Partition.Value,
                        consumeResult.Offset.Value);

                    await ProcessMessageAsync(consumeResult.Message.Value, stoppingToken);

                    // Conferma offset dopo elaborazione riuscita
                    _consumer.Commit(consumeResult);
                    _consumer.StoreOffset(consumeResult);

                    _logger.LogInformation(
                        "Messaggio elaborato e confermato con successo all'offset {Offset}",
                        consumeResult.Offset.Value);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Errore durante il consumo del messaggio: {Error}", ex.Error.Reason);
                    
                    // Non confermare in caso di errore - il messaggio verrà rielaborato
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante l'elaborazione del messaggio");
                    
                    // Non confermare in caso di errore - il messaggio verrà rielaborato
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumer Kafka arrestato");
        }
        finally
        {
            CloseConsumer();
        }
    }

    private void CloseConsumer()
    {
        if (!_disposed && _consumer != null)
        {
            try
            {
                _consumer.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing Kafka consumer");
            }
        }
    }

    private async Task ProcessMessageAsync(string messageValue, CancellationToken ct)
    {
        var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(messageValue);
        
        if (orderEvent == null)
        {
            _logger.LogWarning("Impossibile deserializzare OrderCreatedEvent");
            return;
        }

        _logger.LogInformation(
            "Elaborazione OrderCreatedEvent per Ordine {OrderId} con {ItemCount} articoli",
            orderEvent.OrderId,
            orderEvent.Items.Count);

        using var scope = _serviceProvider.CreateScope();
        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryWorkerService>();

        // Aggiorna inventario per ogni articolo nell'ordine
        foreach (var item in orderEvent.Items)
        {
            try
            {
                // Riduce inventario della quantità ordinata (delta negativo)
                var success = await inventoryService.AdjustInventoryQuantityAsync(
                    Guid.Parse(item.ProductId),
                    -item.Quantity,
                    ct);

                if (success)
                {
                    _logger.LogInformation(
                        "Ridotto inventario per Prodotto {ProductId} di {Quantity} unità (Ordine {OrderId})",
                        item.ProductId,
                        item.Quantity,
                        orderEvent.OrderId);
                }
                else
                {
                    _logger.LogWarning(
                        "Impossibile ridurre inventario per Prodotto {ProductId} di {Quantity} unità (Ordine {OrderId}) - inventario insufficiente o prodotto non trovato",
                        item.ProductId,
                        item.Quantity,
                        orderEvent.OrderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Errore durante l'aggiornamento dell'inventario per Prodotto {ProductId} (Ordine {OrderId})",
                    item.ProductId,
                    orderEvent.OrderId);
                throw; // Rilancia per evitare commit - il messaggio verrà rielaborato
            }
        }

        _logger.LogInformation(
            "Completati aggiornamenti inventario per Ordine {OrderId}",
            orderEvent.OrderId);
    }

    public override void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            
            try
            {
                _consumer?.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing Kafka consumer during dispose");
            }
            
            try
            {
                _consumer?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing Kafka consumer");
            }
            
            base.Dispose();
        }
    }
}
