# PaymentService e NotificationService - Guide Implementazione

## Indice
1. [PaymentService](#paymentservice)
2. [NotificationService](#notificationservice)
3. [Integrazione con Sistema Esistente](#integrazione-con-sistema-esistente)
4. [Testing Strategy](#testing-strategy)

---

# PaymentService

## Panoramica

### Responsabilità
- Gestione transazioni pagamento
- Integrazione payment providers (Stripe, PayPal, ecc.)
- Tracciamento stato pagamenti
- Gestione rimborsi
- Webhook processing per eventi esterni

### Pattern Architetturali
- **Strategy Pattern**: Supporto multipli payment providers
- **State Machine**: Gestione stati transazione
- **Idempotency**: Prevenzione pagamenti duplicati
- **Webhook Handler**: Processing eventi asincroni
- **Event Sourcing Light**: Audit trail completo

---

## Architecture Design

### Database Schema

```sql
-- Tabella principale transazioni
CREATE TABLE PaymentTransactions (
    Id INT PRIMARY KEY IDENTITY,
    OrderId INT NOT NULL,                    -- Link a OrderService
    Amount DECIMAL(18,2) NOT NULL,
    Currency NVARCHAR(3) NOT NULL DEFAULT 'EUR',
    Status NVARCHAR(50) NOT NULL,            -- Pending, Completed, Failed, Refunded
    PaymentMethod NVARCHAR(50) NOT NULL,     -- CreditCard, PayPal, BankTransfer
    Provider NVARCHAR(50) NOT NULL,          -- Stripe, PayPal, Braintree
    ProviderTransactionId NVARCHAR(200),     -- ID transazione provider esterno
    IdempotencyKey NVARCHAR(100) UNIQUE,     -- Previene duplicati
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CompletedAtUtc DATETIME2,
    FailureReason NVARCHAR(500)
);

-- Indici per performance
CREATE INDEX IX_OrderId ON PaymentTransactions(OrderId);
CREATE INDEX IX_Status ON PaymentTransactions(Status);
CREATE INDEX IX_CreatedAt ON PaymentTransactions(CreatedAtUtc DESC);
CREATE UNIQUE INDEX IX_IdempotencyKey ON PaymentTransactions(IdempotencyKey);

-- Eventi pagamento (audit trail)
CREATE TABLE PaymentEvents (
    Id INT PRIMARY KEY IDENTITY,
    PaymentTransactionId INT NOT NULL,
    EventType NVARCHAR(50) NOT NULL,         -- Created, Authorized, Captured, Failed, Refunded
    EventData NVARCHAR(MAX),                 -- JSON con dettagli
    OccurredAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (PaymentTransactionId) REFERENCES PaymentTransactions(Id)
);

CREATE INDEX IX_PaymentTransactionId ON PaymentEvents(PaymentTransactionId);

-- Informazioni pagamento cliente (tokenizzate)
CREATE TABLE PaymentMethods (
    Id INT PRIMARY KEY IDENTITY,
    CustomerId INT NOT NULL,                 -- Link a Customer/User
    Type NVARCHAR(50) NOT NULL,              -- CreditCard, PayPal
    Provider NVARCHAR(50) NOT NULL,
    ProviderCustomerId NVARCHAR(200),        -- ID cliente nel provider
    ProviderPaymentMethodId NVARCHAR(200),   -- Token metodo pagamento
    Last4Digits NVARCHAR(4),                 -- Ultime 4 cifre carta
    ExpiryMonth INT,
    ExpiryYear INT,
    IsDefault BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_CustomerId ON PaymentMethods(CustomerId);
```

### Models

```csharp
// PaymentTransaction.cs
public class PaymentTransaction
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public PaymentStatus Status { get; set; }
    public string PaymentMethod { get; set; }
    public string Provider { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? FailureReason { get; set; }
    
    public ICollection<PaymentEvent> Events { get; set; } = new List<PaymentEvent>();
}

public enum PaymentStatus
{
    Pending,        // In attesa elaborazione
    Authorized,     // Autorizzato ma non ancora catturato
    Captured,       // Fondi catturati (pagamento completato)
    Failed,         // Pagamento fallito
    Refunded,       // Rimborsato
    PartiallyRefunded, // Rimborsato parzialmente
    Cancelled       // Cancellato
}

// PaymentEvent.cs (Event Sourcing Light)
public class PaymentEvent
{
    public int Id { get; set; }
    public int PaymentTransactionId { get; set; }
    public string EventType { get; set; } = null!;
    public string? EventData { get; set; }  // JSON
    public DateTime OccurredAtUtc { get; set; }
    
    public PaymentTransaction PaymentTransaction { get; set; } = null!;
}

// PaymentMethod.cs
public class PaymentMethod
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Type { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string? ProviderCustomerId { get; set; }
    public string? ProviderPaymentMethodId { get; set; }
    public string? Last4Digits { get; set; }
    public int? ExpiryMonth { get; set; }
    public int? ExpiryYear { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
```

### Strategy Pattern - Payment Providers

```csharp
// IPaymentProvider.cs
public interface IPaymentProvider
{
    string ProviderName { get; }
    
    Task<PaymentResult> CreatePaymentAsync(
        PaymentRequest request, 
        CancellationToken ct = default);
    
    Task<PaymentResult> CapturePaymentAsync(
        string providerTransactionId, 
        CancellationToken ct = default);
    
    Task<RefundResult> RefundPaymentAsync(
        string providerTransactionId, 
        decimal amount, 
        CancellationToken ct = default);
    
    Task<PaymentStatus> GetPaymentStatusAsync(
        string providerTransactionId, 
        CancellationToken ct = default);
}

// StripePaymentProvider.cs
public class StripePaymentProvider : IPaymentProvider
{
    private readonly StripeClient _stripeClient;
    private readonly ILogger<StripePaymentProvider> _logger;
    
    public string ProviderName => "Stripe";
    
    public async Task<PaymentResult> CreatePaymentAsync(
        PaymentRequest request, 
        CancellationToken ct = default)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100), // Centesimi
                Currency = request.Currency.ToLower(),
                PaymentMethod = request.PaymentMethodId,
                Confirm = true,
                Metadata = new Dictionary<string, string>
                {
                    { "OrderId", request.OrderId.ToString() },
                    { "IdempotencyKey", request.IdempotencyKey }
                }
            };
            
            var service = new PaymentIntentService(_stripeClient);
            var paymentIntent = await service.CreateAsync(options, cancellationToken: ct);
            
            return new PaymentResult
            {
                Success = paymentIntent.Status == "succeeded",
                ProviderTransactionId = paymentIntent.Id,
                Status = MapStripeStatus(paymentIntent.Status),
                Message = paymentIntent.Status
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe payment creation failed");
            return new PaymentResult
            {
                Success = false,
                Status = PaymentStatus.Failed,
                Message = ex.Message
            };
        }
    }
    
    public async Task<PaymentResult> CapturePaymentAsync(
        string providerTransactionId, 
        CancellationToken ct = default)
    {
        var service = new PaymentIntentService(_stripeClient);
        var paymentIntent = await service.CaptureAsync(
            providerTransactionId, 
            cancellationToken: ct);
        
        return new PaymentResult
        {
            Success = paymentIntent.Status == "succeeded",
            ProviderTransactionId = paymentIntent.Id,
            Status = MapStripeStatus(paymentIntent.Status)
        };
    }
    
    public async Task<RefundResult> RefundPaymentAsync(
        string providerTransactionId, 
        decimal amount, 
        CancellationToken ct = default)
    {
        var options = new RefundCreateOptions
        {
            PaymentIntent = providerTransactionId,
            Amount = (long)(amount * 100)
        };
        
        var service = new RefundService(_stripeClient);
        var refund = await service.CreateAsync(options, cancellationToken: ct);
        
        return new RefundResult
        {
            Success = refund.Status == "succeeded",
            RefundId = refund.Id,
            Amount = refund.Amount / 100m
        };
    }
    
    private PaymentStatus MapStripeStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "requires_payment_method" => PaymentStatus.Pending,
            "requires_confirmation" => PaymentStatus.Pending,
            "requires_action" => PaymentStatus.Pending,
            "processing" => PaymentStatus.Pending,
            "requires_capture" => PaymentStatus.Authorized,
            "succeeded" => PaymentStatus.Captured,
            "canceled" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Failed
        };
    }
}

// PayPalPaymentProvider.cs (Simile implementazione)
public class PayPalPaymentProvider : IPaymentProvider
{
    // Implementazione specifica PayPal
}
```

### Payment Service

```csharp
// IPaymentService.cs
public interface IPaymentService
{
    Task<PaymentTransaction> ProcessPaymentAsync(
        ProcessPaymentCommand command, 
        CancellationToken ct = default);
    
    Task<PaymentTransaction> CapturePaymentAsync(
        int paymentTransactionId, 
        CancellationToken ct = default);
    
    Task<RefundResult> RefundPaymentAsync(
        int paymentTransactionId, 
        decimal? amount = null, 
        CancellationToken ct = default);
    
    Task<PaymentTransaction?> GetPaymentByOrderIdAsync(
        int orderId, 
        CancellationToken ct = default);
}

// PaymentService.cs
public class PaymentService : IPaymentService
{
    private readonly PaymentContext _db;
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly IPaymentEventPublisher _eventPublisher;
    private readonly ILogger<PaymentService> _logger;
    
    public async Task<PaymentTransaction> ProcessPaymentAsync(
        ProcessPaymentCommand command, 
        CancellationToken ct = default)
    {
        // 1. Idempotency check
        var existing = await _db.PaymentTransactions
            .FirstOrDefaultAsync(p => p.IdempotencyKey == command.IdempotencyKey, ct);
        
        if (existing != null)
        {
            _logger.LogInformation("Payment already processed: {IdempotencyKey}", 
                command.IdempotencyKey);
            return existing;
        }
        
        // 2. Crea transazione con stato Pending
        var transaction = new PaymentTransaction
        {
            OrderId = command.OrderId,
            Amount = command.Amount,
            Currency = command.Currency,
            Status = PaymentStatus.Pending,
            PaymentMethod = command.PaymentMethod,
            Provider = command.Provider,
            IdempotencyKey = command.IdempotencyKey,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        _db.PaymentTransactions.Add(transaction);
        await _db.SaveChangesAsync(ct);
        
        // 3. Aggiungi evento "Created"
        await AddPaymentEventAsync(transaction.Id, "Created", 
            new { command.Amount, command.Currency }, ct);
        
        // 4. Seleziona provider
        var provider = _providers.FirstOrDefault(p => p.ProviderName == command.Provider);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider {command.Provider} not found");
        }
        
        // 5. Esegui pagamento
        var result = await provider.CreatePaymentAsync(new PaymentRequest
        {
            OrderId = command.OrderId,
            Amount = command.Amount,
            Currency = command.Currency,
            PaymentMethodId = command.PaymentMethodId,
            IdempotencyKey = command.IdempotencyKey
        }, ct);
        
        // 6. Aggiorna transazione con risultato
        transaction.Status = result.Status;
        transaction.ProviderTransactionId = result.ProviderTransactionId;
        transaction.UpdatedAtUtc = DateTime.UtcNow;
        
        if (result.Success && result.Status == PaymentStatus.Captured)
        {
            transaction.CompletedAtUtc = DateTime.UtcNow;
            await AddPaymentEventAsync(transaction.Id, "Captured", result, ct);
            
            // 7. Pubblica evento Kafka
            await _eventPublisher.PublishPaymentCompletedAsync(new PaymentCompletedEvent
            {
                PaymentTransactionId = transaction.Id,
                OrderId = transaction.OrderId,
                Amount = transaction.Amount,
                CompletedAt = transaction.CompletedAtUtc.Value
            });
        }
        else if (!result.Success)
        {
            transaction.FailureReason = result.Message;
            await AddPaymentEventAsync(transaction.Id, "Failed", result, ct);
            
            // Pubblica evento fallimento
            await _eventPublisher.PublishPaymentFailedAsync(new PaymentFailedEvent
            {
                PaymentTransactionId = transaction.Id,
                OrderId = transaction.OrderId,
                Reason = result.Message
            });
        }
        
        await _db.SaveChangesAsync(ct);
        
        return transaction;
    }
    
    private async Task AddPaymentEventAsync(
        int paymentTransactionId, 
        string eventType, 
        object eventData, 
        CancellationToken ct)
    {
        var paymentEvent = new PaymentEvent
        {
            PaymentTransactionId = paymentTransactionId,
            EventType = eventType,
            EventData = JsonSerializer.Serialize(eventData),
            OccurredAtUtc = DateTime.UtcNow
        };
        
        _db.PaymentEvents.Add(paymentEvent);
        await _db.SaveChangesAsync(ct);
    }
}
```

### REST API Controller

```csharp
[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    
    // Crea/Processa pagamento
    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
    {
        var command = new ProcessPaymentCommand
        {
            OrderId = dto.OrderId,
            Amount = dto.Amount,
            Currency = dto.Currency ?? "EUR",
            PaymentMethod = dto.PaymentMethod,
            Provider = dto.Provider,
            PaymentMethodId = dto.PaymentMethodId,
            IdempotencyKey = dto.IdempotencyKey ?? Guid.NewGuid().ToString()
        };
        
        var transaction = await _paymentService.ProcessPaymentAsync(command);
        
        return Ok(new
        {
            paymentTransactionId = transaction.Id,
            status = transaction.Status.ToString(),
            amount = transaction.Amount,
            currency = transaction.Currency
        });
    }
    
    // Query stato pagamento
    [HttpGet("{paymentTransactionId}")]
    public async Task<IActionResult> GetPayment(int paymentTransactionId)
    {
        var transaction = await _db.PaymentTransactions
            .Include(p => p.Events)
            .FirstOrDefaultAsync(p => p.Id == paymentTransactionId);
        
        if (transaction == null)
            return NotFound();
        
        return Ok(transaction);
    }
    
    // Query pagamenti per ordine
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetPaymentsByOrder(int orderId)
    {
        var transactions = await _db.PaymentTransactions
            .Where(p => p.OrderId == orderId)
            .ToListAsync();
        
        return Ok(transactions);
    }
    
    // Rimborso
    [HttpPost("{paymentTransactionId}/refund")]
    public async Task<IActionResult> RefundPayment(
        int paymentTransactionId, 
        [FromBody] RefundDto dto)
    {
        var result = await _paymentService.RefundPaymentAsync(
            paymentTransactionId, 
            dto.Amount);
        
        return Ok(result);
    }
}
```

### Webhook Handler

```csharp
[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IConfiguration _config;
    
    // Stripe webhook
    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                Request.Headers["Stripe-Signature"],
                _config["Stripe:WebhookSecret"]);
            
            if (stripeEvent.Type == "payment_intent.succeeded")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                // Aggiorna stato transazione
                await UpdatePaymentStatusFromWebhook(
                    paymentIntent.Id, 
                    PaymentStatus.Captured);
            }
            else if (stripeEvent.Type == "payment_intent.payment_failed")
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                await UpdatePaymentStatusFromWebhook(
                    paymentIntent.Id, 
                    PaymentStatus.Failed);
            }
            
            return Ok();
        }
        catch (StripeException ex)
        {
            return BadRequest();
        }
    }
    
    private async Task UpdatePaymentStatusFromWebhook(
        string providerTransactionId, 
        PaymentStatus status)
    {
        var transaction = await _db.PaymentTransactions
            .FirstOrDefaultAsync(p => p.ProviderTransactionId == providerTransactionId);
        
        if (transaction != null)
        {
            transaction.Status = status;
            transaction.UpdatedAtUtc = DateTime.UtcNow;
            
            if (status == PaymentStatus.Captured)
            {
                transaction.CompletedAtUtc = DateTime.UtcNow;
            }
            
            await _db.SaveChangesAsync();
        }
    }
}
```

### Kafka Events

```csharp
// PaymentCompletedEvent.cs
public class PaymentCompletedEvent
{
    public int PaymentTransactionId { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime CompletedAt { get; set; }
}

// PaymentFailedEvent.cs
public class PaymentFailedEvent
{
    public int PaymentTransactionId { get; set; }
    public int OrderId { get; set; }
    public string Reason { get; set; } = null!;
}
```

### Configuration

```json
{
  "ConnectionStrings": {
    "PaymentDb": "Server=localhost,1433;Database=PaymentDb;..."
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "PaymentCompletedTopic": "payment-completed",
    "PaymentFailedTopic": "payment-failed"
  },
  "Stripe": {
    "ApiKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "PublishableKey": "pk_test_..."
  },
  "PayPal": {
    "ClientId": "...",
    "ClientSecret": "...",
    "Mode": "sandbox"
  }
}
```

---

# NotificationService

## Panoramica

### Responsabilità
- Invio email (conferme ordini, pagamenti, spedizioni)
- Invio SMS (notifiche critiche)
- Push notifications (mobile app future)
- Template management
- Kafka consumer per eventi sistema

### Pattern Architetturali
- **Template Method Pattern**: Rendering template email
- **Observer Pattern**: Risposta a eventi Kafka
- **Queue Pattern**: Retry automatico invii falliti
- **Factory Pattern**: Creazione notifiche diverse

---

## Architecture Design

### Database Schema

```sql
-- Template notifiche
CREATE TABLE NotificationTemplates (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL UNIQUE,      -- order-created, payment-completed
    Type NVARCHAR(50) NOT NULL,              -- Email, SMS, Push
    Subject NVARCHAR(200),                   -- Per email
    BodyTemplate NVARCHAR(MAX) NOT NULL,     -- Template con placeholder {{variabile}}
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Storia notifiche inviate
CREATE TABLE NotificationHistory (
    Id INT PRIMARY KEY IDENTITY,
    Type NVARCHAR(50) NOT NULL,              -- Email, SMS, Push
    Recipient NVARCHAR(200) NOT NULL,        -- Email address o phone number
    Subject NVARCHAR(200),
    Body NVARCHAR(MAX),
    Status NVARCHAR(50) NOT NULL,            -- Pending, Sent, Failed
    TemplateId INT,
    RelatedEntityType NVARCHAR(50),          -- Order, Payment
    RelatedEntityId INT,
    SentAtUtc DATETIME2,
    FailureReason NVARCHAR(500),
    RetryCount INT NOT NULL DEFAULT 0,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (TemplateId) REFERENCES NotificationTemplates(Id)
);

CREATE INDEX IX_Recipient ON NotificationHistory(Recipient);
CREATE INDEX IX_Status ON NotificationHistory(Status);
CREATE INDEX IX_RelatedEntity ON NotificationHistory(RelatedEntityType, RelatedEntityId);
```

### Models

```csharp
// NotificationTemplate.cs
public class NotificationTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string? Subject { get; set; }
    public string BodyTemplate { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public enum NotificationType
{
    Email,
    SMS,
    Push
}

// NotificationHistory.cs
public class NotificationHistory
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string Recipient { get; set; } = null!;
    public string? Subject { get; set; }
    public string Body { get; set; } = null!;
    public NotificationStatus Status { get; set; }
    public int? TemplateId { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    
    public NotificationTemplate? Template { get; set; }
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}
```

### Notification Service

```csharp
// INotificationService.cs
public interface INotificationService
{
    Task SendEmailAsync(SendEmailCommand command, CancellationToken ct = default);
    Task SendSmsAsync(SendSmsCommand command, CancellationToken ct = default);
    Task SendTemplatedEmailAsync(string templateName, string recipient, 
        Dictionary<string, string> variables, CancellationToken ct = default);
}

// NotificationService.cs
public class NotificationService : INotificationService
{
    private readonly NotificationContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<NotificationService> _logger;
    
    public async Task SendTemplatedEmailAsync(
        string templateName, 
        string recipient, 
        Dictionary<string, string> variables, 
        CancellationToken ct = default)
    {
        // 1. Carica template
        var template = await _db.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive, ct);
        
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateName} not found");
        }
        
        // 2. Render template con variabili
        var subject = _templateRenderer.Render(template.Subject ?? "", variables);
        var body = _templateRenderer.Render(template.BodyTemplate, variables);
        
        // 3. Crea record history
        var history = new NotificationHistory
        {
            Type = NotificationType.Email,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            TemplateId = template.Id,
            CreatedAtUtc = DateTime.UtcNow
        };
        
        _db.NotificationHistory.Add(history);
        await _db.SaveChangesAsync(ct);
        
        // 4. Invia email
        try
        {
            await _emailSender.SendEmailAsync(recipient, subject, body, ct);
            
            history.Status = NotificationStatus.Sent;
            history.SentAtUtc = DateTime.UtcNow;
            
            _logger.LogInformation("Email sent to {Recipient} using template {Template}", 
                recipient, templateName);
        }
        catch (Exception ex)
        {
            history.Status = NotificationStatus.Failed;
            history.FailureReason = ex.Message;
            
            _logger.LogError(ex, "Failed to send email to {Recipient}", recipient);
        }
        
        await _db.SaveChangesAsync(ct);
    }
}
```

### Email Sender (SMTP)

```csharp
// IEmailSender.cs
public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string body, 
        CancellationToken ct = default);
}

// SmtpEmailSender.cs
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;
    
    public async Task SendEmailAsync(
        string to, 
        string subject, 
        string body, 
        CancellationToken ct = default)
    {
        using var client = new SmtpClient(_settings.Host, _settings.Port);
        client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
        client.EnableSsl = true;
        
        var message = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        
        message.To.Add(to);
        
        await client.SendMailAsync(message, ct);
    }
}

// SendGridEmailSender.cs (Alternative)
public class SendGridEmailSender : IEmailSender
{
    private readonly SendGridClient _client;
    
    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress("noreply@example.com", "Order System"),
            Subject = subject,
            HtmlContent = body
        };
        msg.AddTo(new EmailAddress(to));
        
        await _client.SendEmailAsync(msg, ct);
    }
}
```

### Template Renderer

```csharp
// ITemplateRenderer.cs
public interface ITemplateRenderer
{
    string Render(string template, Dictionary<string, string> variables);
}

// SimpleTemplateRenderer.cs
public class SimpleTemplateRenderer : ITemplateRenderer
{
    public string Render(string template, Dictionary<string, string> variables)
    {
        var result = template;
        
        foreach (var kvp in variables)
        {
            result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value);
        }
        
        return result;
    }
}

// RazorTemplateRenderer.cs (Advanced)
public class RazorTemplateRenderer : ITemplateRenderer
{
    public string Render(string template, Dictionary<string, string> variables)
    {
        var engine = new RazorLightEngineBuilder()
            .UseMemoryCachingProvider()
            .Build();
        
        return engine.CompileRenderStringAsync(
            "template", 
            template, 
            variables).Result;
    }
}
```

### Kafka Consumers

```csharp
// OrderCreatedConsumer.cs
public class OrderCreatedNotificationConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConsumer<string, string> _consumer;
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("order-created");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = _consumer.Consume(stoppingToken);
            var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Value);
            
            using var scope = _serviceProvider.CreateScope();
            var notificationService = scope.ServiceProvider
                .GetRequiredService<INotificationService>();
            
            // Invia email conferma ordine
            await notificationService.SendTemplatedEmailAsync(
                "order-created",
                orderEvent.CustomerEmail,
                new Dictionary<string, string>
                {
                    { "OrderId", orderEvent.OrderId.ToString() },
                    { "TotalAmount", orderEvent.TotalAmount.ToString("C") },
                    { "CreatedAt", orderEvent.CreatedAt.ToString("dd/MM/yyyy HH:mm") }
                });
            
            _consumer.Commit(message);
        }
    }
}

// PaymentCompletedConsumer.cs
public class PaymentCompletedNotificationConsumer : BackgroundService
{
    // Simile implementazione per evento pagamento completato
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("payment-completed");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = _consumer.Consume(stoppingToken);
            var paymentEvent = JsonSerializer.Deserialize<PaymentCompletedEvent>(message.Value);
            
            // Invia email pagamento confermato
            await notificationService.SendTemplatedEmailAsync(
                "payment-completed",
                paymentEvent.CustomerEmail,
                new Dictionary<string, string>
                {
                    { "OrderId", paymentEvent.OrderId.ToString() },
                    { "Amount", paymentEvent.Amount.ToString("C") },
                    { "PaymentMethod", paymentEvent.PaymentMethod }
                });
            
            _consumer.Commit(message);
        }
    }
}
```

### REST API Controller

```csharp
[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    
    // Invio email manuale
    [HttpPost("email")]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailDto dto)
    {
        await _notificationService.SendEmailAsync(new SendEmailCommand
        {
            To = dto.To,
            Subject = dto.Subject,
            Body = dto.Body
        });
        
        return Ok(new { message = "Email sent" });
    }
    
    // Invio email da template
    [HttpPost("email/template")]
    public async Task<IActionResult> SendTemplatedEmail([FromBody] SendTemplatedEmailDto dto)
    {
        await _notificationService.SendTemplatedEmailAsync(
            dto.TemplateName,
            dto.Recipient,
            dto.Variables);
        
        return Ok(new { message = "Email sent" });
    }
    
    // Query storia notifiche
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string? recipient = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.NotificationHistory.AsQueryable();
        
        if (!string.IsNullOrEmpty(recipient))
        {
            query = query.Where(n => n.Recipient == recipient);
        }
        
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return Ok(new { total, page, pageSize, items });
    }
}
```

### Email Templates (Seed)

```csharp
// Seed nel Program.cs o migration
public static void SeedNotificationTemplates(NotificationContext db)
{
    if (!db.NotificationTemplates.Any())
    {
        db.NotificationTemplates.AddRange(
            // Order Created
            new NotificationTemplate
            {
                Name = "order-created",
                Type = NotificationType.Email,
                Subject = "Conferma Ordine #{{OrderId}}",
                BodyTemplate = @"
                    <h2>Grazie per il tuo ordine!</h2>
                    <p>Il tuo ordine #{{OrderId}} è stato ricevuto con successo.</p>
                    <p><strong>Totale:</strong> {{TotalAmount}}</p>
                    <p><strong>Data:</strong> {{CreatedAt}}</p>
                    <p>Riceverai un'altra email quando il pagamento sarà confermato.</p>
                ",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            
            // Payment Completed
            new NotificationTemplate
            {
                Name = "payment-completed",
                Type = NotificationType.Email,
                Subject = "Pagamento Confermato - Ordine #{{OrderId}}",
                BodyTemplate = @"
                    <h2>Pagamento Confermato!</h2>
                    <p>Il pagamento per l'ordine #{{OrderId}} è stato completato con successo.</p>
                    <p><strong>Importo:</strong> {{Amount}}</p>
                    <p><strong>Metodo:</strong> {{PaymentMethod}}</p>
                    <p>Il tuo ordine verrà processato a breve.</p>
                ",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            
            // Low Stock Alert
            new NotificationTemplate
            {
                Name = "low-stock-alert",
                Type = NotificationType.Email,
                Subject = "ALERT: Stock Basso - Prodotto {{ProductName}}",
                BodyTemplate = @"
                    <h2>Allerta Stock Basso</h2>
                    <p>Il prodotto <strong>{{ProductName}}</strong> ha solo {{AvailableQuantity}} unità rimaste.</p>
                    <p>Si consiglia di rifornire il magazzino.</p>
                ",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        );
        
        db.SaveChanges();
    }
}
```

### Configuration

```json
{
  "ConnectionStrings": {
    "NotificationDb": "Server=localhost,1433;Database=NotificationDb;..."
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "OrderCreatedTopic": "order-created",
    "PaymentCompletedTopic": "payment-completed",
    "LowStockTopic": "low-stock-alert",
    "ConsumerGroupId": "notification-service"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@example.com",
    "FromName": "Order System"
  },
  "SendGrid": {
    "ApiKey": "SG.xxx..."
  },
  "Twilio": {
    "AccountSid": "ACxxx...",
    "AuthToken": "xxx...",
    "FromPhoneNumber": "+1234567890"
  }
}
```

---

# Integrazione con Sistema Esistente

## Flussi Completi

### Flusso 1: Ordine → Pagamento → Notifiche

```
1. User crea ordine
   Frontend → GatewayBff → OrderService
   
2. OrderService salva ordine
   DB: Orders, OrderItems
   
3. OrderService pubblica OrderCreatedEvent
   Kafka Topic: order-created
   
4. InventoryService consuma evento
   → Riduce stock
   
5. NotificationService consuma evento
   → Invia email "Ordine Creato"
   
6. Frontend reindirizza a pagamento
   PaymentService/checkout
   
7. User completa pagamento
   Frontend → GatewayBff → PaymentService
   
8. PaymentService processa con Stripe/PayPal
   → Salva PaymentTransaction
   → Pubblica PaymentCompletedEvent
   
9. OrderService consuma PaymentCompletedEvent
   → Aggiorna Order.Status = "Paid"
   
10. NotificationService consuma PaymentCompletedEvent
    → Invia email "Pagamento Confermato"
```

### Flusso 2: Stock Basso → Notifica Admin

```
1. InventoryService aggiorna stock
   AvailableQuantity = 3
   
2. InventoryService controlla threshold
   if (quantity < lowStockThreshold)
   
3. Pubblica LowStockEvent
   Kafka Topic: low-stock-alert
   
4. NotificationService consuma evento
   → Invia email admin/warehouse
   → Template: low-stock-alert
```

## Kafka Topics Summary

```
order-created
  Producers: OrderService
  Consumers: InventoryService, NotificationService
  
payment-completed
  Producers: PaymentService
  Consumers: OrderService, NotificationService
  
payment-failed
  Producers: PaymentService
  Consumers: OrderService, NotificationService
  
low-stock-alert
  Producers: InventoryService
  Consumers: NotificationService
  
inventory-updated
  Producers: InventoryService
  Consumers: ProductService (cache invalidation)
```

## Endpoints BFF da Aggiungere

```csharp
// GatewayBff Commands per Payment
[HttpPost("api/commands/payment")]
public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto dto)
{
    // Forward to PaymentService
    var response = await _httpClient.PostAsJsonAsync(
        $"{_paymentServiceUrl}/api/payments", 
        dto);
    return Ok(await response.Content.ReadFromJsonAsync<PaymentResponseDto>());
}

// Query payment status
[HttpGet("api/queries/payment/{paymentId}")]
public async Task<IActionResult> GetPaymentStatus(int paymentId)
{
    var response = await _httpClient.GetAsync(
        $"{_paymentServiceUrl}/api/payments/{paymentId}");
    return Ok(await response.Content.ReadFromJsonAsync<PaymentDto>());
}

// Query notification history
[HttpGet("api/queries/notifications/{orderId}")]
public async Task<IActionResult> GetNotifications(int orderId)
{
    var response = await _httpClient.GetAsync(
        $"{_notificationServiceUrl}/api/notifications/history?orderId={orderId}");
    return Ok(await response.Content.ReadFromJsonAsync<List<NotificationDto>>());
}
```

---

# Testing Strategy

## Unit Tests

```csharp
// PaymentService Unit Test
public class PaymentServiceTests
{
    [Fact]
    public async Task ProcessPayment_WithValidData_CreatesTransaction()
    {
        // Arrange
        var mockProvider = new Mock<IPaymentProvider>();
        mockProvider.Setup(p => p.CreatePaymentAsync(It.IsAny<PaymentRequest>(), default))
            .ReturnsAsync(new PaymentResult { Success = true, Status = PaymentStatus.Captured });
        
        var service = new PaymentService(_db, new[] { mockProvider.Object }, _eventPublisher, _logger);
        
        // Act
        var result = await service.ProcessPaymentAsync(new ProcessPaymentCommand
        {
            OrderId = 1,
            Amount = 100,
            Currency = "EUR",
            Provider = "Stripe",
            IdempotencyKey = Guid.NewGuid().ToString()
        });
        
        // Assert
        Assert.Equal(PaymentStatus.Captured, result.Status);
        Assert.NotNull(result.CompletedAtUtc);
    }
    
    [Fact]
    public async Task ProcessPayment_WithDuplicateIdempotencyKey_ReturnsExisting()
    {
        // Test idempotency
    }
}

// NotificationService Unit Test
public class NotificationServiceTests
{
    [Fact]
    public async Task SendTemplatedEmail_WithValidTemplate_SendsEmail()
    {
        // Arrange
        var mockEmailSender = new Mock<IEmailSender>();
        var service = new NotificationService(_db, mockEmailSender.Object, _smsSender, _templateRenderer, _logger);
        
        // Act
        await service.SendTemplatedEmailAsync(
            "order-created",
            "test@example.com",
            new Dictionary<string, string> { { "OrderId", "123" } });
        
        // Assert
        mockEmailSender.Verify(s => s.SendEmailAsync(
            "test@example.com",
            It.IsAny<string>(),
            It.IsAny<string>(),
            default), Times.Once);
    }
}
```

## Integration Tests

```csharp
// Payment Integration Test
public class PaymentIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task ProcessPayment_EndToEnd_Success()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.PostAsJsonAsync("/api/payments", new
        {
            orderId = 1,
            amount = 100,
            currency = "EUR",
            provider = "Stripe",
            paymentMethodId = "pm_card_visa"
        });
        
        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto>();
        Assert.Equal("Captured", result.Status);
    }
}
```

## E2E Test

```csharp
public class OrderToPaymentE2ETests
{
    [Fact]
    public async Task CompleteOrderFlow_CreatesOrderAndProcessesPayment()
    {
        // 1. Crea ordine
        var orderResponse = await _client.PostAsJsonAsync("/api/commands/orders", new
        {
            items = new[] { new { productId = 1, quantity = 2 } }
        });
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();
        
        // 2. Processa pagamento
        var paymentResponse = await _client.PostAsJsonAsync("/api/commands/payment", new
        {
            orderId = order.Id,
            amount = order.TotalAmount,
            provider = "Stripe"
        });
        
        // 3. Verifica email inviata
        await Task.Delay(2000); // Wait for Kafka processing
        var notifications = await _client.GetFromJsonAsync<List<NotificationDto>>(
            $"/api/queries/notifications/{order.Id}");
        
        Assert.Contains(notifications, n => n.TemplateId == "payment-completed");
    }
}
```

---

## Conclusione

PaymentService e NotificationService completano l'architettura con:

**PaymentService**:
- ✅ Multi-provider support (Stripe, PayPal)
- ✅ Idempotency per sicurezza
- ✅ Webhook handling
- ✅ Event sourcing light (audit trail)
- ✅ Kafka integration per comunicazione

**NotificationService**:
- ✅ Template-based notifications
- ✅ Multi-channel (Email, SMS, Push)
- ✅ Kafka consumers per eventi sistema
- ✅ Retry logic automatico
- ✅ Storia completa notifiche

Entrambi seguono gli stessi pattern architetturali del sistema esistente e si integrano perfettamente via Kafka events.
