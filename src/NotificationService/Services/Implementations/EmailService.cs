using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Configuration;
using NotificationService.Models;
using NotificationService.Services;
using System.Text.RegularExpressions;

namespace NotificationService.Services.Implementations;

/// <summary>
/// Implementazione del servizio Email usando MailKit
/// </summary>
public class MailKitEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<MailKitEmailService> _logger;

    public MailKitEmailService(IOptions<EmailSettings> settings, ILogger<MailKitEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<NotificationResponse> SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Valida email
            if (!ValidateEmail(request.To))
            {
                _logger.LogWarning("Indirizzo email non valido: {Email}", request.To);
                return NotificationResponse.Error("Indirizzo email non valido");
            }

            var message = CreateMimeMessage(request);
            var messageId = await SendMimeMessageAsync(message, cancellationToken);

            _logger.LogInformation("Email inviata con successo a {Email}. MessageId: {MessageId}", request.To, messageId);

            return new NotificationResponse
            {
                Success = true,
                MessageId = messageId,
                ExternalId = messageId,
                Status = "sent"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio email a {Email}", request.To);
            return NotificationResponse.Error($"Errore invio email: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<BulkNotificationResponse> SendBulkEmailAsync(List<SendEmailRequest> requests, CancellationToken cancellationToken = default)
    {
        var response = new BulkNotificationResponse
        {
            TotalRequests = requests.Count
        };

        // Usa un pool di connessioni SMTP per performance migliori
        using var client = new SmtpClient();
        await ConnectToSmtpAsync(client, cancellationToken);

        try
        {
            foreach (var request in requests)
            {
                try
                {
                    if (!ValidateEmail(request.To))
                    {
                        response.FailureCount++;
                        response.Results.Add(new BulkNotificationResult
                        {
                            Recipient = request.To,
                            Success = false,
                            Error = "Indirizzo email non valido"
                        });
                        continue;
                    }

                    var message = CreateMimeMessage(request);
                    var messageId = await client.SendAsync(message, cancellationToken);

                    response.SuccessCount++;
                    response.Results.Add(new BulkNotificationResult
                    {
                        Recipient = request.To,
                        Success = true,
                        MessageId = messageId,
                        ExternalId = messageId
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Errore durante invio email bulk a {Email}", request.To);
                    response.FailureCount++;
                    response.Results.Add(new BulkNotificationResult
                    {
                        Recipient = request.To,
                        Success = false,
                        Error = ex.Message
                    });
                }
            }
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }

        response.Success = response.SuccessCount > 0;
        return response;
    }

    /// <inheritdoc/>
    public async Task<NotificationResponse> SendEmailWithAttachmentsAsync(SendEmailRequest request, List<EmailAttachment> attachments, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ValidateEmail(request.To))
            {
                return NotificationResponse.Error("Indirizzo email non valido");
            }

            var message = CreateMimeMessage(request, attachments);
            var messageId = await SendMimeMessageAsync(message, cancellationToken);

            _logger.LogInformation("Email con allegati inviata a {Email}. Allegati: {AttachmentCount}", request.To, attachments.Count);

            return new NotificationResponse
            {
                Success = true,
                MessageId = messageId,
                ExternalId = messageId,
                Status = "sent"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio email con allegati a {Email}", request.To);
            return NotificationResponse.Error($"Errore invio email con allegati: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public bool ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Crea MimeMessage dalla richiesta
    /// </summary>
    private MimeMessage CreateMimeMessage(SendEmailRequest request, List<EmailAttachment>? attachments = null)
    {
        var message = new MimeMessage();
        
        // Mittente
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        
        // Destinatario
        message.To.Add(MailboxAddress.Parse(request.To));
        
        // CC e BCC
        if (request.Cc?.Any() == true)
        {
            foreach (var cc in request.Cc)
            {
                if (ValidateEmail(cc))
                    message.Cc.Add(MailboxAddress.Parse(cc));
            }
        }
        
        if (request.Bcc?.Any() == true)
        {
            foreach (var bcc in request.Bcc)
            {
                if (ValidateEmail(bcc))
                    message.Bcc.Add(MailboxAddress.Parse(bcc));
            }
        }

        // Oggetto
        message.Subject = request.Subject;

        // Corpo del messaggio
        var bodyBuilder = new BodyBuilder();
        
        if (!string.IsNullOrEmpty(request.HtmlContent))
        {
            bodyBuilder.HtmlBody = request.HtmlContent;
            bodyBuilder.TextBody = request.Content; // Fallback testo
        }
        else
        {
            bodyBuilder.TextBody = request.Content;
        }

        // Allegati
        if (attachments?.Any() == true)
        {
            foreach (var attachment in attachments)
            {
                if (attachment.IsInline)
                {
                    // Allegato inline (per immagini nell'HTML)
                    var inline = bodyBuilder.LinkedResources.Add(attachment.FileName, attachment.Content);
                    inline.ContentId = attachment.ContentId ?? attachment.FileName;
                }
                else
                {
                    // Allegato normale
                    bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
                }
            }
        }

        message.Body = bodyBuilder.ToMessageBody();
        
        // Header personalizzati
        if (request.Metadata?.Any() == true)
        {
            foreach (var meta in request.Metadata)
            {
                message.Headers.Add($"X-Custom-{meta.Key}", meta.Value);
            }
        }

        return message;
    }

    /// <summary>
    /// Invia MimeMessage
    /// </summary>
    private async Task<string> SendMimeMessageAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        using var client = new SmtpClient();
        await ConnectToSmtpAsync(client, cancellationToken);
        
        try
        {
            var messageId = await client.SendAsync(message, cancellationToken);
            return messageId;
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }

    /// <summary>
    /// Connette al server SMTP
    /// </summary>
    private async Task ConnectToSmtpAsync(SmtpClient client, CancellationToken cancellationToken)
    {
        await client.ConnectAsync(_settings.Host, _settings.Port, _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, cancellationToken);
        
        if (!string.IsNullOrEmpty(_settings.Username))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
        }
    }
}

/// <summary>
/// Implementazione mock del servizio Email per testing
/// </summary>
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;
    
    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<NotificationResponse> SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        if (!ValidateEmail(request.To))
        {
            return Task.FromResult(NotificationResponse.Error("Indirizzo email non valido"));
        }

        var messageId = Guid.NewGuid().ToString();
        
        _logger.LogInformation("EMAIL MOCK inviata a {Email}\nOggetto: {Subject}\nContenuto: {Content}", 
            request.To, request.Subject, request.Content);

        return Task.FromResult(new NotificationResponse
        {
            Success = true,
            MessageId = messageId,
            ExternalId = messageId,
            Status = "sent"
        });
    }

    /// <inheritdoc/>
    public async Task<BulkNotificationResponse> SendBulkEmailAsync(List<SendEmailRequest> requests, CancellationToken cancellationToken = default)
    {
        var response = new BulkNotificationResponse
        {
            TotalRequests = requests.Count
        };

        foreach (var request in requests)
        {
            var result = await SendEmailAsync(request, cancellationToken);
            if (result.Success)
            {
                response.SuccessCount++;
                response.Results.Add(new BulkNotificationResult
                {
                    Recipient = request.To,
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
                    Recipient = request.To,
                    Success = false,
                    Error = result.Error
                });
            }
        }

        response.Success = response.SuccessCount > 0;
        return response;
    }

    /// <inheritdoc/>
    public Task<NotificationResponse> SendEmailWithAttachmentsAsync(SendEmailRequest request, List<EmailAttachment> attachments, CancellationToken cancellationToken = default)
    {
        if (!ValidateEmail(request.To))
        {
            return Task.FromResult(NotificationResponse.Error("Indirizzo email non valido"));
        }

        var messageId = Guid.NewGuid().ToString();
        
        _logger.LogInformation("EMAIL MOCK con allegati inviata a {Email}\nOggetto: {Subject}\nAllegati: {AttachmentCount}", 
            request.To, request.Subject, attachments.Count);

        return Task.FromResult(new NotificationResponse
        {
            Success = true,
            MessageId = messageId,
            ExternalId = messageId,
            Status = "sent"
        });
    }

    /// <inheritdoc/>
    public bool ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }
}