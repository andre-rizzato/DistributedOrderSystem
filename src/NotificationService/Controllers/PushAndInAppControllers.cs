using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;
using NotificationService.Models.Requests;
using NotificationService.Models.Responses;
using NotificationService.Services;

namespace NotificationService.Controllers;

/// <summary>
/// Controller for handling push and in-app notifications.
/// Manages sending push notifications via Firebase FCM and real-time notifications via SignalR.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PushController : ControllerBase
{
    private readonly IPushService _pushService;
    private readonly ILogger<PushController> _logger;

    public PushController(IPushService pushService, ILogger<PushController> logger)
    {
        _pushService = pushService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a single push notification to a specific device
    /// </summary>
    /// <param name="request">Push notification data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send result</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationResponse>> SendPushNotification(
        [FromBody] SendPushNotificationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _pushService.SendPushNotificationAsync(request, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Push notification sending failed",
                    Detail = result.Error,
                    Status = 400
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to token {Token}", request.DeviceToken);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while sending the push notification",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Sends a push notification to all of a specific user's devices.
    /// Automatically retrieves all tokens registered for the user.
    /// </summary>
    /// <param name="request">Send-to-user request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send result</returns>
    [HttpPost("send-to-user")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationResponse>> SendToUser(
        [FromBody] SendPushToUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _pushService.SendToUserAsync(
                request.UserId,
                request.Title,
                request.Body,
                request.Data,
                cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Push notification to user failed",
                    Detail = result.Error,
                    Status = 400
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to user {UserId}", request.UserId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while sending the notification to the user",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Sends a push notification to a Firebase topic.
    /// Useful for broadcast notifications to groups of subscribed users.
    /// </summary>
    /// <param name="request">Send-to-topic request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send result</returns>
    [HttpPost("send-to-topic")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationResponse>> SendToTopic(
        [FromBody] SendPushToTopicRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _pushService.SendToTopicAsync(
                request.Topic,
                request.Title,
                request.Body,
                request.Data,
                cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Push notification to topic failed",
                    Detail = result.Error,
                    Status = 400
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to topic {Topic}", request.Topic);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while sending the notification to the topic",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Registers a device token for a user.
    /// Required in order to send push notifications to the user.
    /// </summary>
    /// <param name="request">Token registration data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result</returns>
    [HttpPost("register-token")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult> RegisterDeviceToken(
        [FromBody] RegisterDeviceTokenRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _pushService.RegisterDeviceTokenAsync(
                request.UserId,
                request.DeviceToken,
                request.Platform,
                cancellationToken);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = "Device token registered successfully",
                    userId = request.UserId,
                    platform = request.Platform
                });
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Token registration failed",
                    Detail = "Unable to register the device token",
                    Status = 400
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering token for user {UserId}", request.UserId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while registering the token",
                Status = 500
            });
        }
    }
}

/// <summary>
/// Controller for handling real-time in-app notifications via SignalR
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InAppNotificationController : ControllerBase
{
    private readonly IInAppNotificationService _inAppService;
    private readonly ILogger<InAppNotificationController> _logger;

    public InAppNotificationController(
        IInAppNotificationService inAppService,
        ILogger<InAppNotificationController> logger)
    {
        _inAppService = inAppService;
        _logger = logger;
    }

    /// <summary>
    /// Sends an in-app notification to a specific user.
    /// The notification is saved to the database and sent in real time if the user is connected.
    /// </summary>
    /// <param name="request">In-app notification data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send result</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationResponse>> SendInAppNotification(
        [FromBody] SendInAppNotificationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _inAppService.SendInAppNotificationAsync(request, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "In-app notification sending failed",
                    Detail = result.Error,
                    Status = 400
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending in-app notification to user {UserId}", request.UserId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while sending the in-app notification",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Gets the unread notifications for a user.
    /// Returns in-app notifications not yet read, with pagination.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="limit">Maximum number of notifications to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unread notifications</returns>
    [HttpGet("unread/{userId}")]
    [ProducesResponseType(typeof(List<Notification>), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<List<Notification>>> GetUnreadNotifications(
        string userId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID is required");
            }

            if (limit <= 0 || limit > 100)
            {
                return BadRequest("Limit must be between 1 and 100");
            }

            var notifications = await _inAppService.GetUnreadNotificationsAsync(userId, limit, cancellationToken);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread notifications for user {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while retrieving notifications",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Marks a notification as read.
    /// Updates the notification status and sends a real-time confirmation to the user.
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    [HttpPost("{notificationId}/mark-read")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult> MarkAsRead(
        int notificationId,
        [FromBody] MarkAsReadRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _inAppService.MarkAsReadAsync(notificationId, request.UserId, cancellationToken);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = "Notification marked as read",
                    notificationId,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Notification not found",
                    Detail = "Notification not found or does not belong to the user",
                    Status = 404
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} for user {UserId}",
                notificationId, request.UserId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while marking the notification",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Marks all notifications as read for a user.
    /// Updates all of the user's unread notifications.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of notifications updated</returns>
    [HttpPost("mark-all-read/{userId}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult> MarkAllAsRead(
        string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest("User ID is required");
            }

            var count = await _inAppService.MarkAllAsReadAsync(userId, cancellationToken);

            return Ok(new
            {
                success = true,
                message = $"{count} notifications marked as read",
                count,
                userId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while marking the notifications",
                Status = 500
            });
        }
    }
}

// Additional DTOs for the Push and InApp controllers

/// <summary>
/// Request to send a push notification to a user
/// </summary>
public class SendPushToUserRequest
{
    /// <summary>Recipient user ID</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Notification title</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Message body</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Optional additional data</summary>
    public Dictionary<string, string>? Data { get; set; }
}

/// <summary>
/// Request to send a push notification to a topic
/// </summary>
public class SendPushToTopicRequest
{
    /// <summary>Firebase topic name</summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>Notification title</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Message body</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Optional additional data</summary>
    public Dictionary<string, string>? Data { get; set; }
}

/// <summary>
/// Request to register a device token
/// </summary>
public class RegisterDeviceTokenRequest
{
    /// <summary>User ID</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Firebase device token</summary>
    public string DeviceToken { get; set; } = string.Empty;

    /// <summary>Device platform (iOS, Android)</summary>
    public string Platform { get; set; } = string.Empty;
}

/// <summary>
/// Request to mark a notification as read
/// </summary>
public class MarkAsReadRequest
{
    /// <summary>User ID</summary>
    public string UserId { get; set; } = string.Empty;
}
