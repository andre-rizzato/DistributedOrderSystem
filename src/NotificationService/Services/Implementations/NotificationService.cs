using Hangfire;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Models.Requests;
using NotificationService.Models.Responses;
using NotificationService.Services;
using System.Text.Json;

namespace NotificationService.Services.Implementations;

/// <summary>
/// Implementation of the main notification service.
/// Coordinates all notification providers and handles the central business logic.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly NotificationContext _context;
    private readonly INotificationTemplateService _templateService;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly IPushService _pushService;
    private readonly IInAppNotificationService _inAppService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationContext context,
        INotificationTemplateService templateService,
        ISmsService smsService,
        IEmailService emailService,
        IPushService pushService,
        IInAppNotificationService inAppService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _templateService = templateService;
        _smsService = smsService;
        _emailService = emailService;
        _pushService = pushService;
        _inAppService = inAppService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <summary>
    /// Sends a notification using a predefined template.
    /// The template is rendered with the provided variables before sending.
    /// </summary>
    public async Task<NotificationResponse> SendNotificationAsync(SendTemplateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting to send template notification {TemplateName} to {Recipient}",
                request.TemplateName, request.Recipient);

            // Render the template with the variables
            var renderedTemplate = await _templateService.RenderTemplateAsync(
                request.TemplateName, request.Variables, cancellationToken);

            // Build a direct request from the rendered template
            var directRequest = new SendNotificationRequest
            {
                Type = request.Type ?? renderedTemplate.Type,
                Recipient = request.Recipient,
                Subject = renderedTemplate.Subject,
                Content = renderedTemplate.Content,
                HtmlContent = renderedTemplate.HtmlContent,
                Priority = request.Priority,
                UserId = request.UserId,
                Source = request.Source ?? "TemplateSystem",
                ReferenceId = request.ReferenceId,
                ReferenceType = request.ReferenceType,
                Metadata = request.Metadata,
                ScheduledAt = request.ScheduledAt
            };

            return await SendDirectNotificationAsync(directRequest, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending template notification {TemplateName}", request.TemplateName);
            return NotificationResponse.CreateError($"Template error: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends a direct notification without using a template.
    /// Handles the routing logic toward the appropriate provider.
    /// </summary>
    public async Task<NotificationResponse> SendDirectNotificationAsync(SendNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting to send direct notification {Type} to {Recipient}",
                request.Type, request.Recipient);

            // Save the notification to the database for audit purposes
            var notification = await CreateNotificationEntityAsync(request, cancellationToken);

            // If scheduled, delegate to Hangfire
            if (request.ScheduledAt.HasValue && request.ScheduledAt > DateTime.UtcNow)
            {
                return await ScheduleNotificationInternalAsync(notification, request.ScheduledAt.Value);
            }

            // Immediate send through the specific provider
            var result = await SendThroughProviderAsync(request, notification, cancellationToken);

            // Update notification status based on the result
            await UpdateNotificationStatusAsync(notification, result, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending direct notification {Type} to {Recipient}",
                request.Type, request.Recipient);
            return NotificationResponse.CreateError($"Send error: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends multiple notifications in batch to optimize performance.
    /// Groups notifications by type and uses the providers' batch capabilities.
    /// </summary>
    public async Task<BulkNotificationResponse> SendBulkNotificationsAsync(List<SendNotificationRequest> requests, CancellationToken cancellationToken = default)
    {
        var response = new BulkNotificationResponse
        {
            TotalRequests = requests.Count
        };

        try
        {
            _logger.LogInformation("Starting bulk send of {Count} notifications", requests.Count);

            // Group by type to optimize sending
            var groupedRequests = requests.GroupBy(r => r.Type);

            foreach (var group in groupedRequests)
            {
                var groupResults = await ProcessBulkGroupAsync(group.ToList(), cancellationToken);

                response.SuccessCount += groupResults.SuccessCount;
                response.FailureCount += groupResults.FailureCount;
                response.Results.AddRange(groupResults.Results);
            }

            response.Success = response.SuccessCount > 0;

            _logger.LogInformation("Bulk send completed: {SuccessCount}/{TotalCount}",
                response.SuccessCount, response.TotalRequests);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk send of {Count} notifications", requests.Count);

            // Mark all as failed on a general error
            response.FailureCount = response.TotalRequests;
            response.Results = requests.Select(r => new BulkNotificationResult
            {
                Recipient = r.Recipient,
                Success = false,
                Error = ex.Message
            }).ToList();

            return response;
        }
    }

    /// <summary>
    /// Schedules a notification for future delivery using Hangfire.
    /// </summary>
    public async Task<int> ScheduleNotificationAsync(SendNotificationRequest request, DateTime scheduledAt, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scheduling notification {Type} for {ScheduledAt}",
                request.Type, scheduledAt);

            // Create the notification entity with a scheduled status
            var notification = await CreateNotificationEntityAsync(request, cancellationToken);
            notification.Status = NotificationStatus.Pending;
            notification.ScheduledAt = scheduledAt;

            await _context.SaveChangesAsync(cancellationToken);

            // Schedule the Hangfire job
            var jobId = _backgroundJobClient.Schedule(
                () => ExecuteScheduledNotificationAsync((int)notification.Id),
                scheduledAt);

            // Save the job ID for potential cancellation
            notification.ExternalId = jobId;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Notification {NotificationId} scheduled for {ScheduledAt} with job {JobId}",
                notification.Id, scheduledAt, jobId);

            return (int)notification.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notification for {ScheduledAt}", scheduledAt);
            throw;
        }
    }

    /// <summary>
    /// Cancels a scheduled notification by removing the job from Hangfire.
    /// </summary>
    public async Task<bool> CancelScheduledNotificationAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

            if (notification == null || notification.Status != NotificationStatus.Scheduled)
            {
                return false;
            }

            // Cancel the Hangfire job if present
            if (!string.IsNullOrEmpty(notification.ExternalId))
            {
                _backgroundJobClient.Delete(notification.ExternalId);
            }

            // Update notification status
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = "Notification canceled by the user";

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Scheduled notification {NotificationId} canceled", notificationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling notification {NotificationId}", notificationId);
            return false;
        }
    }

    /// <summary>
    /// Gets the detailed status of a specific notification.
    /// </summary>
    public async Task<NotificationStatusResponse?> GetNotificationStatusAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

            if (notification == null)
            {
                return null;
            }

            return new NotificationStatusResponse
            {
                Success = true,
                NotificationId = notification.Id,
                Status = notification.Status,
                Type = notification.Type,
                Recipient = notification.Recipient,
                Subject = notification.Subject,
                CreatedAt = notification.CreatedAt,
                SentAt = notification.SentAt,
                DeliveredAt = notification.DeliveredAt,
                RetryCount = notification.RetryCount,
                ErrorMessage = notification.ErrorMessage,
                ExternalId = notification.ExternalId,
                UpdatedAt = notification.DeliveredAt ?? notification.SentAt ?? notification.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status for notification {NotificationId}", notificationId);
            return new NotificationStatusResponse
            {
                Success = false,
                NotificationId = notificationId,
                Status = NotificationStatus.Failed,
                Type = NotificationType.Email,
                Recipient = "unknown",
                Subject = "unknown",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Gets a user's notifications with pagination.
    /// </summary>
    public async Task<NotificationListResponse> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);
            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new NotificationListResponse
            {
                Notifications = notifications.Select(n => new NotificationStatusResponse
                {
                    NotificationId = n.Id,
                    Status = n.Status,
                    Type = n.Type,
                    Recipient = n.Recipient,
                    Subject = n.Subject,
                    CreatedAt = n.CreatedAt,
                    SentAt = n.SentAt,
                    DeliveredAt = n.DeliveredAt,
                    RetryCount = n.RetryCount,
                    ErrorMessage = n.ErrorMessage,
                    ExternalId = n.ExternalId,
                    Success = n.Status == NotificationStatus.Sent || n.Status == NotificationStatus.Delivered,
                    UpdatedAt = n.DeliveredAt ?? n.SentAt ?? n.CreatedAt
                }).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
            return new NotificationListResponse
            {
                Notifications = new List<NotificationStatusResponse>()
            };
        }
    }

    /// <summary>
    /// Generates detailed notification statistics.
    /// </summary>
    public async Task<NotificationStatsResponse> GetNotificationStatsAsync(string? userId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Notifications.AsQueryable();

            // Optional filters
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(n => n.UserId == userId);

            if (fromDate.HasValue)
                query = query.Where(n => n.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(n => n.CreatedAt <= toDate.Value);

            // Compute statistics
            var totalCount = await query.CountAsync(cancellationToken);

            var statusStats = await query
                .GroupBy(n => n.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status.ToString(), x => x.Count, cancellationToken);

            var typeStats = await query
                .GroupBy(n => n.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type.ToString(), x => x.Count, cancellationToken);

            var priorityStats = await query
                .GroupBy(n => n.Priority)
                .Select(g => new { Priority = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Priority.ToString(), x => x.Count, cancellationToken);

            return new NotificationStatsResponse
            {
                TotalNotifications = totalCount,
                StatusStats = statusStats.ToDictionary(x => Enum.Parse<NotificationStatus>(x.Key, true), x => x.Value),
                TypeStats = typeStats.ToDictionary(x => Enum.Parse<NotificationType>(x.Key, true), x => new TypeStats
                {
                    Total = x.Value,
                    Success = 0,
                    Failed = 0,
                    SuccessRate = 0
                }),
                Period = new DateRange
                {
                    From = fromDate ?? DateTime.UtcNow.AddDays(-30),
                    To = toDate ?? DateTime.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing notification statistics");
            return new NotificationStatsResponse
            {
                TotalNotifications = 0
            };
        }
    }

    /// <summary>
    /// Retries sending a failed notification.
    /// </summary>
    public async Task<NotificationResponse> RetryNotificationAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

            if (notification == null)
            {
                return NotificationResponse.CreateError("Notification not found");
            }

            if (notification.Status == NotificationStatus.Delivered)
            {
                return NotificationResponse.CreateError("The notification has already been delivered successfully");
            }

            // Increment the retry counter
            notification.RetryCount++;
            notification.ErrorMessage = null;

            // Rebuild the original request
            var request = new SendNotificationRequest
            {
                Type = notification.Type,
                Recipient = notification.Recipient,
                Subject = notification.Subject,
                Content = notification.Content,
                HtmlContent = notification.HtmlContent,
                Priority = notification.Priority,
                UserId = notification.UserId,
                Source = notification.Source,
                ReferenceId = notification.ReferenceId,
                ReferenceType = notification.ReferenceType,
                Metadata = string.IsNullOrEmpty(notification.Metadata) ? null :
                    JsonSerializer.Deserialize<Dictionary<string, string>>(notification.Metadata)
            };

            // Retry the send
            var result = await SendThroughProviderAsync(request, notification, cancellationToken);

            // Update status
            await UpdateNotificationStatusAsync(notification, result, cancellationToken);

            _logger.LogInformation("Retry of notification {NotificationId} completed. Success: {Success}",
                notificationId, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying notification {NotificationId}", notificationId);
            return NotificationResponse.CreateError($"Retry error: {ex.Message}");
        }
    }

    // Private methods for internal logic

    /// <summary>
    /// Creates the notification entity in the database for audit and tracking.
    /// </summary>
    private async Task<Notification> CreateNotificationEntityAsync(SendNotificationRequest request, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            Type = request.Type,
            Recipient = request.Recipient,
            Subject = request.Subject,
            Content = request.Content,
            HtmlContent = request.HtmlContent,
            Priority = request.Priority,
            Status = NotificationStatus.Pending,
            UserId = request.UserId,
            Source = request.Source ?? "System",
            ReferenceId = request.ReferenceId,
            ReferenceType = request.ReferenceType,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
            ScheduledAt = request.ScheduledAt,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return notification;
    }

    /// <summary>
    /// Routes the notification to the appropriate provider based on its type.
    /// </summary>
    private async Task<NotificationResponse> SendThroughProviderAsync(SendNotificationRequest request, Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            return request.Type switch
            {
                NotificationType.SMS => await _smsService.SendSmsAsync(new SendSmsRequest
                {
                    PhoneNumber = request.Recipient,
                    Message = request.Content
                }, cancellationToken),

                NotificationType.Email => await _emailService.SendEmailAsync(new SendEmailRequest
                {
                    To = request.Recipient,
                    Subject = request.Subject,
                    Content = request.Content,
                    HtmlContent = request.HtmlContent
                }, cancellationToken),

                NotificationType.Push => await _pushService.SendPushNotificationAsync(new SendPushNotificationRequest
                {
                    Target = request.Recipient,
                    DeviceToken = request.Recipient,
                    Title = request.Subject,
                    Body = request.Content,
                    Data = request.Metadata
                }, cancellationToken),

                NotificationType.InApp => await _inAppService.SendInAppNotificationAsync(new SendInAppNotificationRequest
                {
                    UserId = request.UserId ?? request.Recipient,
                    Title = request.Subject,
                    Message = request.Content,
                    Priority = request.Priority,
                    Source = request.Source,
                    ReferenceId = request.ReferenceId,
                    ReferenceType = request.ReferenceType,
                    Data = request.Metadata
                }, cancellationToken),

                _ => NotificationResponse.CreateError($"Notification type {request.Type} not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending through provider {Type}", request.Type);
            return NotificationResponse.CreateError($"Provider error: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the notification status in the database based on the send result.
    /// </summary>
    private async Task UpdateNotificationStatusAsync(Notification notification, NotificationResponse result, CancellationToken cancellationToken)
    {
        try
        {
            if (result.Success)
            {
                notification.Status = NotificationStatus.Sent;
                notification.SentAt = DateTime.UtcNow;
                notification.ExternalId = result.ExternalId;

                // For in-app notifications, mark as delivered immediately
                if (notification.Type == NotificationType.InApp)
                {
                    notification.Status = NotificationStatus.Delivered;
                    notification.DeliveredAt = DateTime.UtcNow;
                }
            }
            else
            {
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = result.Error;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for notification {NotificationId}", notification.Id);
        }
    }

    /// <summary>
    /// Processes a group of notifications of the same type in batch.
    /// </summary>
    private async Task<BulkNotificationResponse> ProcessBulkGroupAsync(List<SendNotificationRequest> requests, CancellationToken cancellationToken)
    {
        var response = new BulkNotificationResponse
        {
            TotalRequests = requests.Count
        };

        // Create entities for all notifications in the group
        var notifications = new List<Notification>();
        foreach (var request in requests)
        {
            var notification = await CreateNotificationEntityAsync(request, cancellationToken);
            notifications.Add(notification);
        }

        try
        {
            // Use type-specific bulk sending if available
            var firstRequest = requests.First();
            BulkNotificationResponse? bulkResult = null;

            switch (firstRequest.Type)
            {
                case NotificationType.SMS:
                    var smsRequests = requests.Select(r => new SendSmsRequest
                    {
                        PhoneNumber = r.Recipient,
                        Message = r.Content
                    }).ToList();
                    bulkResult = await _smsService.SendBulkSmsAsync(smsRequests, cancellationToken);
                    break;

                case NotificationType.Email:
                    var emailRequests = requests.Select(r => new SendEmailRequest
                    {
                        To = r.Recipient,
                        Subject = r.Subject,
                        Content = r.Content,
                        HtmlContent = r.HtmlContent
                    }).ToList();
                    bulkResult = await _emailService.SendBulkEmailAsync(emailRequests, cancellationToken);
                    break;

                default:
                    // For types that don't support bulk sending, send individually
                    foreach (var (request, notification) in requests.Zip(notifications))
                    {
                        var singleResult = await SendThroughProviderAsync(request, notification, cancellationToken);
                        await UpdateNotificationStatusAsync(notification, singleResult, cancellationToken);

                        if (singleResult.Success)
                        {
                            response.SuccessCount++;
                            response.Results.Add(new BulkNotificationResult
                            {
                                Recipient = request.Recipient,
                                Success = true,
                                MessageId = singleResult.MessageId,
                                ExternalId = singleResult.ExternalId
                            });
                        }
                        else
                        {
                            response.FailureCount++;
                            response.Results.Add(new BulkNotificationResult
                            {
                                Recipient = request.Recipient,
                                Success = false,
                                Error = singleResult.Error
                            });
                        }
                    }
                    break;
            }

            // Process the bulk result if available
            if (bulkResult != null)
            {
                response.SuccessCount = bulkResult.SuccessCount;
                response.FailureCount = bulkResult.FailureCount;
                response.Results = bulkResult.Results;

                // Update statuses in the database
                for (int i = 0; i < notifications.Count; i++)
                {
                    var result = bulkResult.Results[i];
                    var notification = notifications[i];

                    if (result.Success)
                    {
                        notification.Status = NotificationStatus.Sent;
                        notification.SentAt = DateTime.UtcNow;
                        notification.ExternalId = result.ExternalId;
                    }
                    else
                    {
                        notification.Status = NotificationStatus.Failed;
                        notification.ErrorMessage = result.Error;
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
            }

            response.Success = response.SuccessCount > 0;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk group {Type}", requests.FirstOrDefault()?.Type);

            // Mark all as failed
            response.FailureCount = response.TotalRequests;
            response.Results = requests.Select(r => new BulkNotificationResult
            {
                Recipient = r.Recipient,
                Success = false,
                Error = ex.Message
            }).ToList();

            return response;
        }
    }

    /// <summary>
    /// Schedules a notification for future delivery.
    /// </summary>
    private async Task<NotificationResponse> ScheduleNotificationInternalAsync(Notification notification, DateTime scheduledAt)
    {
        try
        {
            notification.Status = NotificationStatus.Pending;
            notification.ScheduledAt = scheduledAt;

            var jobId = _backgroundJobClient.Schedule(
                () => ExecuteScheduledNotificationAsync((int)notification.Id),
                scheduledAt);

            notification.ExternalId = jobId;
            await _context.SaveChangesAsync();

            return new NotificationResponse
            {
                Success = true,
                MessageId = notification.Id.ToString(),
                Status = "scheduled",
                ExternalId = jobId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notification {NotificationId}", notification.Id);
            return NotificationResponse.CreateError($"Scheduling error: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes a scheduled notification (called by Hangfire).
    /// </summary>
    [Queue("notifications")]
    public async Task ExecuteScheduledNotificationAsync(int notificationId)
    {
        try
        {
            _logger.LogInformation("Executing scheduled notification {NotificationId}", notificationId);

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null || notification.Status != NotificationStatus.Scheduled)
            {
                _logger.LogWarning("Scheduled notification {NotificationId} not found or not in scheduled status", notificationId);
                return;
            }

            // Rebuild the original request
            var request = new SendNotificationRequest
            {
                Type = notification.Type,
                Recipient = notification.Recipient,
                Subject = notification.Subject,
                Content = notification.Content,
                HtmlContent = notification.HtmlContent,
                Priority = notification.Priority,
                UserId = notification.UserId,
                Source = notification.Source,
                ReferenceId = notification.ReferenceId,
                ReferenceType = notification.ReferenceType,
                Metadata = string.IsNullOrEmpty(notification.Metadata) ? null :
                    JsonSerializer.Deserialize<Dictionary<string, string>>(notification.Metadata)
            };

            // Perform the send
            var result = await SendThroughProviderAsync(request, notification, CancellationToken.None);
            await UpdateNotificationStatusAsync(notification, result, CancellationToken.None);

            _logger.LogInformation("Scheduled notification {NotificationId} executed. Success: {Success}",
                notificationId, result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scheduled notification {NotificationId}", notificationId);

            // Update status as failed
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.Status = NotificationStatus.Failed;
                notification.ErrorMessage = ex.Message;
                await _context.SaveChangesAsync();
            }
        }
    }
}
