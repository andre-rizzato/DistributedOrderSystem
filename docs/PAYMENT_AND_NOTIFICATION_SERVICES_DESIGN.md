# PaymentService and NotificationService - Implementation Guide

> ⚠️ **Verified against the real code.** This document contains two very different stories:
> - **PaymentService**: everything that follows is **pure design, never implemented**. `src/PaymentService/Program.cs` is still the default `dotnet new webapi` template (just the `/weatherforecast` endpoint) — there are no `Controllers/`, `Models/`, `Services/`, or `Data/` folders. `appsettings.Development.json` has Kafka settings scaffolded (`OrderCreatedTopic`, `PaymentProcessedTopic`, consumer group `payment-service`) but no consumer reads them. Treat the entire PaymentService section as a spec for future development, not as real state.
> - **NotificationService**: the service **is implemented**, but substantially differently from what's described below. See the "⚠️ Reality check" note at the start of the NotificationService section for the details of the divergences, and refer to `docs/NOTIFICATION_SERVICE_DOCUMENTATION.md` for the real state of the service.

## Table of Contents
1. [PaymentService](#paymentservice)
2. [NotificationService](#notificationservice)
3. [Integration with the Existing System](#integration-with-the-existing-system)
4. [Testing Strategy](#testing-strategy)

---

# PaymentService

> 📄 **Status: 100% not implemented.** No schema, model, provider, controller, or Kafka event described in this section exists in the code. `PaymentService/Program.cs` only exposes the default template. Everything that follows should be read as "what to build," not "what exists."

## Overview

### Responsibilities
- Payment transaction management
- Payment provider integration (Stripe, PayPal, etc.)
- Payment status tracking
- Refund handling
- Webhook processing for external events

### Architectural Patterns
- **Strategy Pattern**: support for multiple payment providers
- **State Machine**: transaction status management
- **Idempotency**: duplicate payment prevention
- **Webhook Handler**: asynchronous event processing
- **Event Sourcing Light**: full audit trail

---

## Architecture Design

### Database Schema

```sql
-- Main transactions table
CREATE TABLE PaymentTransactions (
    Id INT PRIMARY KEY IDENTITY,
    OrderId INT NOT NULL,                    -- Link to OrderService
    Amount DECIMAL(18,2) NOT NULL,
    Currency NVARCHAR(3) NOT NULL DEFAULT 'EUR',
    Status NVARCHAR(50) NOT NULL,            -- Pending, Completed, Failed, Refunded
    PaymentMethod NVARCHAR(50) NOT NULL,     -- CreditCard, PayPal, BankTransfer
    Provider NVARCHAR(50) NOT NULL,          -- Stripe, PayPal, Braintree
    ProviderTransactionId NVARCHAR(200),     -- External provider transaction ID
    IdempotencyKey NVARCHAR(100) UNIQUE,     -- Prevents duplicates
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CompletedAtUtc DATETIME2,
    FailureReason NVARCHAR(500)
);

-- Indexes for performance
CREATE INDEX IX_OrderId ON PaymentTransactions(OrderId);
CREATE INDEX IX_Status ON PaymentTransactions(Status);
CREATE INDEX IX_CreatedAt ON PaymentTransactions(CreatedAtUtc DESC);
CREATE UNIQUE INDEX IX_IdempotencyKey ON PaymentTransactions(IdempotencyKey);

-- Payment events (audit trail)
CREATE TABLE PaymentEvents (
    Id INT PRIMARY KEY IDENTITY,
    PaymentTransactionId INT NOT NULL,
    EventType NVARCHAR(50) NOT NULL,         -- Created, Authorized, Captured, Failed, Refunded
    EventData NVARCHAR(MAX),                 -- JSON with details
    OccurredAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    FOREIGN KEY (PaymentTransactionId) REFERENCES PaymentTransactions(Id)
);

CREATE INDEX IX_PaymentTransactionId ON PaymentEvents(PaymentTransactionId);

-- Customer payment information (tokenized)
CREATE TABLE PaymentMethods (
    Id INT PRIMARY KEY IDENTITY,
    CustomerId INT NOT NULL,                 -- Link to Customer/User
    Type NVARCHAR(50) NOT NULL,              -- CreditCard, PayPal
    Provider NVARCHAR(50) NOT NULL,
    ProviderCustomerId NVARCHAR(200),        -- Customer ID at the provider
    ProviderPaymentMethodId NVARCHAR(200),   -- Payment method token
    Last4Digits NVARCHAR(4),                 -- Last 4 card digits
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
    Pending,        // Awaiting processing
    Authorized,     // Authorized but not yet captured
    Captured,       // Funds captured (payment completed)
    Failed,         // Payment failed
    Refunded,       // Refunded
    PartiallyRefunded, // Partially refunded
    Cancelled       // Cancelled
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
                Amount = (long)(request.Amount * 100), // Cents
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

// PayPalPaymentProvider.cs (similar implementation)
public class PayPalPaymentProvider : IPaymentProvider
{
    // PayPal-specific implementation
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
        
        // 2. Create transaction with Pending status
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
        
        // 3. Add "Created" event
        await AddPaymentEventAsync(transaction.Id, "Created", 
            new { command.Amount, command.Currency }, ct);
        
        // 4. Select provider
        var provider = _providers.FirstOrDefault(p => p.ProviderName == command.Provider);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider {command.Provider} not found");
        }
        
        // 5. Execute payment
        var result = await provider.CreatePaymentAsync(new PaymentRequest
        {
            OrderId = command.OrderId,
            Amount = command.Amount,
            Currency = command.Currency,
            PaymentMethodId = command.PaymentMethodId,
            IdempotencyKey = command.IdempotencyKey
        }, ct);
        
        // 6. Update transaction with result
        transaction.Status = result.Status;
        transaction.ProviderTransactionId = result.ProviderTransactionId;
        transaction.UpdatedAtUtc = DateTime.UtcNow;
        
        if (result.Success && result.Status == PaymentStatus.Captured)
        {
            transaction.CompletedAtUtc = DateTime.UtcNow;
            await AddPaymentEventAsync(transaction.Id, "Captured", result, ct);
            
            // 7. Publish Kafka event
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
            
            // Publish failure event
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
    
    // Create/process payment
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
    
    // Query payment status
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
    
    // Query payments by order
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetPaymentsByOrder(int orderId)
    {
        var transactions = await _db.PaymentTransactions
            .Where(p => p.OrderId == orderId)
            .ToListAsync();
        
        return Ok(transactions);
    }
    
    // Refund
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
                // Update transaction status
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

> ⚠️ **Reality check — the service is implemented, but differently from this design.** Comparing this document against `src/NotificationService/` (`Program.cs`, `Data/NotificationContext.cs`, `Models/NotificationModels.cs`, `Services/Implementations/NotificationTemplateService.cs`, `Controllers/NotificationController.cs`), the main divergences are:
>
> 1. **No Kafka consumer exists at all** — `NotificationService.csproj` doesn't even reference `Confluent.Kafka`. The entire "Kafka Consumers" section below (`OrderCreatedNotificationConsumer`, `PaymentCompletedNotificationConsumer`) and the "Kafka consumer for system events" line remain unrealized design. The real service uses **Hangfire** (background jobs, `/hangfire` dashboard, PostgreSQL storage) and **SignalR** (`NotificationHub` at `/hubs/notifications` for real-time in-app notifications) — not Kafka — as its asynchronous processing mechanism.
> 2. **Different data schema**: the real model (`NotificationContext`) has tables `Notifications`, `NotificationTemplates`, `NotificationPreferences`, `NotificationLogs` — not `PaymentTransactions`/`NotificationHistory` as in the SQL schema below. The real `Notification` includes fields like `Priority` (`Low/Normal/High/Critical`), `ReferenceId`/`ReferenceType`, `ScheduledAt`, `ExternalId` that don't appear in this design.
> 3. **Different placeholder syntax**: the real templates (seeded in `NotificationContext.SeedTemplates`) use `{variable}` (single brace, substitution via regex in `NotificationTemplateService.ExtractVariables`/`ReplaceVariables`), not `{{variable}}` as in the `SimpleTemplateRenderer`/`RazorTemplateRenderer` examples below.
> 4. **Different template names**: the real ones are `order_confirmation`, `order_shipped`, `payment_reminder`, `welcome_user`, `system_maintenance` (with `NotificationType.Email/SMS/Push/InApp`) — not `order-created`/`payment-completed`/`low-stock-alert` as proposed here.
> 5. **Different real route**: the main endpoint is `POST /api/notification/send-template` (`NotificationController`, base route `api/[controller]` → singular "notification"), not `POST /api/notifications/email` as below.
> 6. **Real channels**: mock implementations (`MockSmsService`/`MockEmailService`/`MockPushService`) in Development, `TwilioSmsService`/`MailKitEmailService`/`FirebasePushService` elsewhere — so MailKit instead of direct SMTP/SendGrid as proposed here.
>
> For the real, verified state of the service, see **`docs/NOTIFICATION_SERVICE_DOCUMENTATION.md`**. The content below remains useful as historical context on some design choices (why template-based, why multi-channel), but it does not describe the code as it exists today.

## Overview

### Responsibilities (as originally designed — see note above for real state)
- Sending emails (order confirmations, payments, shipping)
- Sending SMS (critical notifications)
- Push notifications (future mobile app)
- Template management
- ~~Kafka consumer for system events~~ — not implemented; the real service uses Hangfire/SignalR, not Kafka

### Architectural Patterns (as originally designed)
- **Template Method Pattern**: email template rendering
- ~~**Observer Pattern**: responding to Kafka events~~ — not implemented (no Kafka consumer in the real service)
- **Queue Pattern**: automatic retry of failed sends
- **Factory Pattern**: creation of different notification types

---

## Architecture Design

### Database Schema

```sql
-- Notification templates
CREATE TABLE NotificationTemplates (
    Id INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) NOT NULL UNIQUE,      -- order-created, payment-completed
    Type NVARCHAR(50) NOT NULL,              -- Email, SMS, Push
    Subject NVARCHAR(200),                   -- For email
    BodyTemplate NVARCHAR(MAX) NOT NULL,     -- Template with {{variable}} placeholders
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAtUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- History of sent notifications
CREATE TABLE NotificationHistory (
    Id INT PRIMARY KEY IDENTITY,
    Type NVARCHAR(50) NOT NULL,              -- Email, SMS, Push
    Recipient NVARCHAR(200) NOT NULL,        -- Email address or phone number
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
        // 1. Load template
        var template = await _db.NotificationTemplates
            .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive, ct);
        
        if (template == null)
        {
            throw new InvalidOperationException($"Template {templateName} not found");
        }
        
        // 2. Render template with variables
        var subject = _templateRenderer.Render(template.Subject ?? "", variables);
        var body = _templateRenderer.Render(template.BodyTemplate, variables);
        
        // 3. Create history record
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
        
        // 4. Send email
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
            
            // Send order confirmation email
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
    // Similar implementation for the payment-completed event
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("payment-completed");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var message = _consumer.Consume(stoppingToken);
            var paymentEvent = JsonSerializer.Deserialize<PaymentCompletedEvent>(message.Value);
            
            // Send payment confirmed email
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
    
    // Manual email send
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
    
    // Send email from template
    [HttpPost("email/template")]
    public async Task<IActionResult> SendTemplatedEmail([FromBody] SendTemplatedEmailDto dto)
    {
        await _notificationService.SendTemplatedEmailAsync(
            dto.TemplateName,
            dto.Recipient,
            dto.Variables);
        
        return Ok(new { message = "Email sent" });
    }
    
    // Query notification history
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
// Seed in Program.cs or a migration
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
                Subject = "Order Confirmation #{{OrderId}}",
                BodyTemplate = @"
                    <h2>Thank you for your order!</h2>
                    <p>Your order #{{OrderId}} has been received successfully.</p>
                    <p><strong>Total:</strong> {{TotalAmount}}</p>
                    <p><strong>Date:</strong> {{CreatedAt}}</p>
                    <p>You'll receive another email once the payment is confirmed.</p>
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
                Subject = "Payment Confirmed - Order #{{OrderId}}",
                BodyTemplate = @"
                    <h2>Payment Confirmed!</h2>
                    <p>Payment for order #{{OrderId}} has been completed successfully.</p>
                    <p><strong>Amount:</strong> {{Amount}}</p>
                    <p><strong>Method:</strong> {{PaymentMethod}}</p>
                    <p>Your order will be processed shortly.</p>
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
                Subject = "ALERT: Low Stock - Product {{ProductName}}",
                BodyTemplate = @"
                    <h2>Low Stock Alert</h2>
                    <p>Product <strong>{{ProductName}}</strong> has only {{AvailableQuantity}} units left.</p>
                    <p>Restocking is recommended.</p>
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

# Integration with the Existing System

## Complete Flows

### Flow 1: Order → Payment → Notifications

```
1. User creates an order
   Frontend → GatewayBff → OrderService
   
2. OrderService saves the order
   DB: Orders, OrderItems
   
3. OrderService publishes OrderCreatedEvent
   Kafka Topic: order-created
   
4. InventoryService consumes the event
   → Reduces stock
   
5. NotificationService consumes the event
   → Sends "Order Created" email
   
6. Frontend redirects to payment
   PaymentService/checkout
   
7. User completes payment
   Frontend → GatewayBff → PaymentService
   
8. PaymentService processes via Stripe/PayPal
   → Saves PaymentTransaction
   → Publishes PaymentCompletedEvent
   
9. OrderService consumes PaymentCompletedEvent
   → Updates Order.Status = "Paid"
   
10. NotificationService consumes PaymentCompletedEvent
    → Sends "Payment Confirmed" email
```

### Flow 2: Low Stock → Admin Notification

```
1. InventoryService updates stock
   AvailableQuantity = 3
   
2. InventoryService checks threshold
   if (quantity < lowStockThreshold)
   
3. Publishes LowStockEvent
   Kafka Topic: low-stock-alert
   
4. NotificationService consumes the event
   → Sends email to admin/warehouse
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

## BFF Endpoints to Add

```csharp
// GatewayBff Commands for Payment
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
        // 1. Create order
        var orderResponse = await _client.PostAsJsonAsync("/api/commands/orders", new
        {
            items = new[] { new { productId = 1, quantity = 2 } }
        });
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();
        
        // 2. Process payment
        var paymentResponse = await _client.PostAsJsonAsync("/api/commands/payment", new
        {
            orderId = order.Id,
            amount = order.TotalAmount,
            provider = "Stripe"
        });
        
        // 3. Verify email sent
        await Task.Delay(2000); // Wait for Kafka processing
        var notifications = await _client.GetFromJsonAsync<List<NotificationDto>>(
            $"/api/queries/notifications/{order.Id}");
        
        Assert.Contains(notifications, n => n.TemplateId == "payment-completed");
    }
}
```

---

## Conclusion

**PaymentService** — 📄 everything above is unimplemented design:
- ⬜ Multi-provider support (Stripe, PayPal) — to be built
- ⬜ Idempotency for safety — to be built
- ⬜ Webhook handling — to be built
- ⬜ Event sourcing light (audit trail) — to be built
- ⬜ Kafka integration for communication — settings scaffolded in `appsettings`, no producer/consumer

**NotificationService** — ✅ implemented, but with a design different from the above:
- ✅ Template-based notifications — real, but with `{variable}` syntax and different template names (see "Reality check" note)
- ✅ Multi-channel (Email, SMS, Push, In-App via SignalR)
- ⬜ Kafka consumers for system events — **not present**; the real service uses Hangfire + SignalR, not Kafka
- ➖ Retry logic — present as a field (`RetryCount`) on the `Notification` model, but check `docs/NOTIFICATION_SERVICE_DOCUMENTATION.md` for how much of the automatic retry logic is actually wired up
- ✅ Notification history — real `Notifications`/`NotificationLogs` table, different schema from what's proposed here

This document remains valid as a **spec for PaymentService** and as **historical design context** for NotificationService — not as a description of the code's current state. For NotificationService's real state, use `docs/NOTIFICATION_SERVICE_DOCUMENTATION.md`.
