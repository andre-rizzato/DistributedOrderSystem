using Microsoft.Extensions.Options;
using NotificationService.Configuration;
using NotificationService.Models;
using NotificationService.Services;
using System.Text.RegularExpressions;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace NotificationService.Services.Implementations;

/// <summary>
/// Implementazione del servizio SMS usando Twilio
/// </summary>
public class TwilioSmsService : ISmsService
{
    private readonly SmsSettings _settings;
    private readonly ILogger<TwilioSmsService> _logger;
    
    public TwilioSmsService(IOptions<SmsSettings> settings, ILogger<TwilioSmsService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        // Inizializza Twilio
        TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
    }

    /// <inheritdoc/>
    public async Task<NotificationResponse> SendSmsAsync(SendSmsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Valida numero di telefono
            if (!ValidatePhoneNumber(request.PhoneNumber))
            {
                _logger.LogWarning("Numero di telefono non valido: {PhoneNumber}", request.PhoneNumber);
                return NotificationResponse.Error("Numero di telefono non valido");
            }

            // Crea messaggio Twilio
            var message = await MessageResource.CreateAsync(
                body: request.Message,
                from: new PhoneNumber(_settings.FromPhoneNumber),
                to: new PhoneNumber(request.PhoneNumber)
            );

            _logger.LogInformation("SMS inviato con successo. SID: {MessageSid}", message.Sid);

            return new NotificationResponse
            {
                Success = true,
                MessageId = message.Sid,
                ExternalId = message.Sid,
                Status = "sent"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio SMS a {PhoneNumber}", request.PhoneNumber);
            return NotificationResponse.Error($"Errore invio SMS: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<BulkNotificationResponse> SendBulkSmsAsync(List<SendSmsRequest> requests, CancellationToken cancellationToken = default)
    {
        var response = new BulkNotificationResponse
        {
            TotalRequests = requests.Count
        };

        var tasks = requests.Select(async request =>
        {
            var result = await SendSmsAsync(request, cancellationToken);
            if (result.Success)
            {
                response.SuccessCount++;
                response.Results.Add(new BulkNotificationResult
                {
                    Recipient = request.PhoneNumber,
                    Success = true,
                    MessageId = result.MessageId,
                    ExternalId = result.ExternalId
                });
            }
            else
            {
                response.FailureCount++;
                response.Results.Add(new BulkNotificationResult
                {
                    Recipient = request.PhoneNumber,
                    Success = false,
                    Error = result.Error
                });
            }
        });

        await Task.WhenAll(tasks);
        
        response.Success = response.SuccessCount > 0;
        return response;
    }

    /// <inheritdoc/>
    public async Task<NotificationStatusResponse> GetSmsStatusAsync(string externalId, CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await MessageResource.FetchAsync(pathSid: externalId);
            
            return new NotificationStatusResponse
            {
                Success = true,
                Status = MapTwilioStatus(message.Status),
                ExternalId = message.Sid,
                UpdatedAt = message.DateUpdated ?? DateTime.UtcNow,
                ErrorMessage = message.ErrorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero stato SMS {ExternalId}", externalId);
            return new NotificationStatusResponse
            {
                Success = false,
                Error = $"Errore recupero stato: {ex.Message}"
            };
        }
    }

    /// <inheritdoc/>
    public bool ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        // Regex per validare numeri di telefono internazionali
        var phoneRegex = new Regex(@"^\+[1-9]\d{6,14}$");
        return phoneRegex.IsMatch(phoneNumber);
    }

    /// <summary>
    /// Mappa gli stati Twilio agli stati interni
    /// </summary>
    private static string MapTwilioStatus(MessageResource.StatusEnum? status)
    {
        return status switch
        {
            MessageResource.StatusEnum.Queued => "queued",
            MessageResource.StatusEnum.Sending => "sending",
            MessageResource.StatusEnum.Sent => "sent",
            MessageResource.StatusEnum.Delivered => "delivered",
            MessageResource.StatusEnum.Failed => "failed",
            MessageResource.StatusEnum.Undelivered => "failed",
            _ => "unknown"
        };
    }
}

/// <summary>
/// Implementazione mock del servizio SMS per testing
/// </summary>
public class MockSmsService : ISmsService
{
    private readonly ILogger<MockSmsService> _logger;
    private readonly Dictionary<string, NotificationStatusResponse> _messageStatuses = new();
    
    public MockSmsService(ILogger<MockSmsService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<NotificationResponse> SendSmsAsync(SendSmsRequest request, CancellationToken cancellationToken = default)
    {
        if (!ValidatePhoneNumber(request.PhoneNumber))
        {
            return Task.FromResult(NotificationResponse.Error("Numero di telefono non valido"));
        }

        var messageId = Guid.NewGuid().ToString();
        
        // Simula invio SMS
        _logger.LogInformation("SMS MOCK inviato a {PhoneNumber}: {Message}", request.PhoneNumber, request.Message);
        
        // Salva stato messaggio
        _messageStatuses[messageId] = new NotificationStatusResponse
        {
            Success = true,
            Status = "delivered",
            ExternalId = messageId,
            UpdatedAt = DateTime.UtcNow
        };

        return Task.FromResult(new NotificationResponse
        {
            Success = true,
            MessageId = messageId,
            ExternalId = messageId,
            Status = "sent"
        });
    }

    /// <inheritdoc/>
    public async Task<BulkNotificationResponse> SendBulkSmsAsync(List<SendSmsRequest> requests, CancellationToken cancellationToken = default)
    {
        var response = new BulkNotificationResponse
        {
            TotalRequests = requests.Count
        };

        foreach (var request in requests)
        {
            var result = await SendSmsAsync(request, cancellationToken);
            if (result.Success)
            {
                response.SuccessCount++;
                response.Results.Add(new BulkNotificationResult
                {
                    Recipient = request.PhoneNumber,
                    Success = true,
                    MessageId = result.MessageId,
                    ExternalId = result.ExternalId
                });
            }
            else
            {
                response.FailureCount++;
                response.Results.Add(new BulkNotificationResult
                {
                    Recipient = request.PhoneNumber,
                    Success = false,
                    Error = result.Error
                });
            }
        }

        response.Success = response.SuccessCount > 0;
        return response;
    }

    /// <inheritdoc/>
    public Task<NotificationStatusResponse> GetSmsStatusAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (_messageStatuses.TryGetValue(externalId, out var status))
        {
            return Task.FromResult(status);
        }

        return Task.FromResult(new NotificationStatusResponse
        {
            Success = false,
            Error = "Messaggio non trovato"
        });
    }

    /// <inheritdoc/>
    public bool ValidatePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var phoneRegex = new Regex(@"^\+[1-9]\d{6,14}$");
        return phoneRegex.IsMatch(phoneNumber);
    }
}