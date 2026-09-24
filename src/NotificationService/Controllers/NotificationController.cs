using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;
using NotificationService.Models.Requests;
using NotificationService.Models.Responses;
using NotificationService.Services;

namespace NotificationService.Controllers;

/// <summary>
/// Main controller for managing system notifications.
/// Provides unified endpoints for sending notifications via templates and centralized management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly INotificationTemplateService _templateService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        INotificationService notificationService,
        INotificationTemplateService templateService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _templateService = templateService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a notification using a predefined template.
    /// This is the main endpoint for sending structured notifications.
    /// </summary>
    /// <param name="request">Notification request with template</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send result</returns>
    [HttpPost("send-template")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationResponse>> SendTemplateNotification(
        [FromBody] SendTemplateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate that the template exists and the variables are correct
            var validation = await _templateService.ValidateTemplateVariablesAsync(request.TemplateName, request.Variables);
            if (!validation.IsValid)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Template validation failed",
                    Detail = string.Join("; ", validation.Errors),
                    Status = 400
                });
            }

            var result = await _notificationService.SendNotificationAsync(request, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Template notification {TemplateName} sent successfully to {Recipient}",
                    request.TemplateName, request.Recipient);
                return Ok(result);
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Notification sending failed",
                    Detail = result.Error,
                    Status = 400
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending template notification {TemplateName} to {Recipient}",
                request.TemplateName, request.Recipient);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while sending the notification",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Sends a direct notification without a template.
    /// Useful for ad-hoc or customized notifications.
    /// </summary>
    /// <param name="request">Direct notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send result</returns>
    [HttpPost("send-direct")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationResponse>> SendDirectNotification(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _notificationService.SendDirectNotificationAsync(request, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Direct notification {Type} sent successfully to {Recipient}",
                    request.Type, request.Recipient);
                return Ok(result);
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Notification sending failed",
                    Detail = result.Error,
                    Status = 400
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending direct notification {Type} to {Recipient}",
                request.Type, request.Recipient);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while sending the notification",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Sends multiple notifications in batch.
    /// Optimized for sending large volumes of notifications.
    /// </summary>
    /// <param name="requests">List of notification requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send results</returns>
    [HttpPost("send-bulk")]
    [ProducesResponseType(typeof(BulkNotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<BulkNotificationResponse>> SendBulkNotifications(
        [FromBody] List<SendNotificationRequest> requests,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid || !requests.Any())
            {
                return BadRequest("Request list is invalid or empty");
            }

            if (requests.Count > 1000) // Safety limit
            {
                return BadRequest("Maximum 1000 notifications per bulk request");
            }

            var result = await _notificationService.SendBulkNotificationsAsync(requests, cancellationToken);

            _logger.LogInformation("Bulk send completed: {SuccessCount}/{TotalCount} notifications sent",
                result.SuccessCount, result.TotalRequests);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk send of {Count} notifications", requests?.Count ?? 0);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred during the bulk notification send",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Schedules a notification for future delivery.
    /// Uses Hangfire to manage scheduling.
    /// </summary>
    /// <param name="request">Scheduled notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ID of the scheduled notification</returns>
    [HttpPost("schedule")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult> ScheduleNotification(
        [FromBody] ScheduleNotificationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.ScheduledAt <= DateTime.UtcNow)
            {
                return BadRequest("The scheduled date must be in the future");
            }

            var notificationId = await _notificationService.ScheduleNotificationAsync(
                request.NotificationRequest,
                request.ScheduledAt,
                cancellationToken);

            _logger.LogInformation("Notification scheduled for {ScheduledAt}. ID: {NotificationId}",
                request.ScheduledAt, notificationId);

            return Ok(new
            {
                success = true,
                notificationId,
                scheduledAt = request.ScheduledAt,
                message = "Notification scheduled successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notification for {ScheduledAt}", request.ScheduledAt);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while scheduling the notification",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Cancels a scheduled notification.
    /// Removes the notification from the Hangfire queue.
    /// </summary>
    /// <param name="notificationId">ID of the notification to cancel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancellation result</returns>
    [HttpDelete("schedule/{notificationId}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult> CancelScheduledNotification(
        int notificationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _notificationService.CancelScheduledNotificationAsync(notificationId, cancellationToken);

            if (success)
            {
                _logger.LogInformation("Scheduled notification {NotificationId} canceled", notificationId);
                return Ok(new
                {
                    success = true,
                    notificationId,
                    message = "Scheduled notification canceled successfully"
                });
            }
            else
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Notification not found",
                    Detail = "Scheduled notification not found or already executed",
                    Status = 404
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling notification {NotificationId}", notificationId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while canceling the notification",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Gets the status of a specific notification.
    /// Includes send, delivery, and error information.
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed notification status</returns>
    [HttpGet("{notificationId}/status")]
    [ProducesResponseType(typeof(NotificationStatusResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationStatusResponse>> GetNotificationStatus(
        int notificationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var status = await _notificationService.GetNotificationStatusAsync(notificationId, cancellationToken);

            if (status != null)
            {
                return Ok(status);
            }
            else
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Notification not found",
                    Detail = $"Notification with ID {notificationId} not found",
                    Status = 404
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving status for notification {NotificationId}", notificationId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while retrieving the status",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Gets a user's notifications with pagination.
    /// Includes filters by notification type and status.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="type">Filter by notification type (optional)</param>
    /// <param name="status">Filter by notification status (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of user notifications</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(NotificationListResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationListResponse>> GetUserNotifications(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] NotificationType? type = null,
        [FromQuery] NotificationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID is required");
            }

            if (page <= 0)
            {
                return BadRequest("Page number must be greater than 0");
            }

            if (pageSize <= 0 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(
                userId, page, pageSize, cancellationToken);

            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications for user {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while retrieving notifications",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Gets notification statistics.
    /// Includes counts by type, status, and time trends.
    /// </summary>
    /// <param name="userId">User ID for user-specific statistics (optional)</param>
    /// <param name="fromDate">Period start date (optional)</param>
    /// <param name="toDate">Period end date (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed notification statistics</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(NotificationStatsResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationStatsResponse>> GetNotificationStats(
        [FromQuery] string? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Date validation
            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            {
                return BadRequest("The start date must be before the end date");
            }

            // Maximum period limit (1 year)
            if (fromDate.HasValue && toDate.HasValue && (toDate - fromDate).Value.TotalDays > 365)
            {
                return BadRequest("The maximum period for statistics is 1 year");
            }

            var stats = await _notificationService.GetNotificationStatsAsync(
                userId, fromDate, toDate, cancellationToken);

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification statistics for user {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while retrieving statistics",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Retries sending a failed notification.
    /// Uses the same configuration as the original notification.
    /// </summary>
    /// <param name="notificationId">ID of the notification to retry</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the new attempt</returns>
    [HttpPost("{notificationId}/retry")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationResponse>> RetryNotification(
        int notificationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _notificationService.RetryNotificationAsync(notificationId, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation("Retry of notification {NotificationId} completed successfully", notificationId);
                return Ok(result);
            }
            else if (result.Error?.Contains("not found") == true)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Notification not found",
                    Detail = result.Error,
                    Status = 404
                });
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Retry failed",
                    Detail = result.Error,
                    Status = 400
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying notification {NotificationId}", notificationId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while retrying the notification",
                Status = 500
            });
        }
    }
}

/// <summary>
/// DTO for a scheduled notification request
/// </summary>
public class ScheduleNotificationRequest
{
    /// <summary>Data of the notification to schedule</summary>
    public required SendNotificationRequest NotificationRequest { get; set; }

    /// <summary>Scheduled send date and time (UTC)</summary>
    public DateTime ScheduledAt { get; set; }
}
