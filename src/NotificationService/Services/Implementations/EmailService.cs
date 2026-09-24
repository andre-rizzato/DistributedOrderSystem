using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using NotificationService.Configuration;
using NotificationService.Models;
using NotificationService.Models.Requests;
using NotificationService.Models.Responses;
using NotificationService.Services;
using System.Text.RegularExpressions;

namespace NotificationService.Services.Implementations;

/// <summary>
/// Email service implementation using MailKit
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
            // Validate email
            if (!ValidateEmail(request.To))
            {
                _logger.LogWarning("Invalid email address: {Email}", request.To);
                return NotificationResponse.CreateError("Invalid email address");
            }

            var message = CreateMimeMessage(request);
            var messageId = await SendMimeMessageAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Email}. MessageId: {MessageId}", request.To, messageId);

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
            _logger.LogError(ex, "Error sending email to {Email}", request.To);
            return NotificationResponse.CreateError($"Email send error: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<BulkNotificationResponse> SendBulkEmailAsync(List<SendEmailRequest> requests, CancellationToken cancellationToken = default)
    {
        var response = new BulkNotificationResponse
        {
            TotalRequests = requests.Count
        };

        // Use an SMTP connection pool for better performance
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
                            Error = "Invalid email address"
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
                    _logger.LogError(ex, "Error sending bulk email to {Email}", request.To);
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
                return NotificationResponse.CreateError("Invalid email address");
            }

            var message = CreateMimeMessage(request, attachments);
            var messageId = await SendMimeMessageAsync(message, cancellationToken);

            _logger.LogInformation("Email with attachments sent to {Email}. Attachments: {AttachmentCount}", request.To, attachments.Count);

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
            _logger.LogError(ex, "Error sending email with attachments to {Email}", request.To);
            return NotificationResponse.CreateError($"Error sending email with attachments: {ex.Message}");
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
    /// Builds a MimeMessage from the request
    /// </summary>
    private MimeMessage CreateMimeMessage(SendEmailRequest request, List<EmailAttachment>? attachments = null)
    {
        var message = new MimeMessage();

        // Sender
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));

        // Recipient
        message.To.Add(MailboxAddress.Parse(request.To));

        // CC and BCC
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

        // Subject
        message.Subject = request.Subject;

        // Message body
        var bodyBuilder = new BodyBuilder();

        if (!string.IsNullOrEmpty(request.HtmlContent))
        {
            bodyBuilder.HtmlBody = request.HtmlContent;
            bodyBuilder.TextBody = request.Content; // Text fallback
        }
        else
        {
            bodyBuilder.TextBody = request.Content;
        }

        // Attachments
        if (attachments?.Any() == true)
        {
            foreach (var attachment in attachments)
            {
                if (attachment.IsInline)
                {
                    // Inline attachment (for images in the HTML)
                    var inline = bodyBuilder.LinkedResources.Add(attachment.FileName, attachment.Content);
                    inline.ContentId = attachment.ContentId ?? attachment.FileName;
                }
                else
                {
                    // Regular attachment
                    bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
                }
            }
        }

        message.Body = bodyBuilder.ToMessageBody();

        // Custom headers - removed as Metadata property doesn't exist

        return message;
    }

    /// <summary>
    /// Sends a MimeMessage
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
    /// Connects to the SMTP server
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
/// Mock implementation of the Email service for testing
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
            return Task.FromResult(NotificationResponse.CreateError("Invalid email address"));
        }

        var messageId = Guid.NewGuid().ToString();

        _logger.LogInformation("MOCK EMAIL sent to {Email}\nSubject: {Subject}\nContent: {Content}",
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
            return Task.FromResult(NotificationResponse.CreateError("Invalid email address"));
        }

        var messageId = Guid.NewGuid().ToString();

        _logger.LogInformation("MOCK EMAIL with attachments sent to {Email}\nSubject: {Subject}\nAttachments: {AttachmentCount}",
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
