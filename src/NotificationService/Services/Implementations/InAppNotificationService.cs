using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Models.Requests;
using NotificationService.Models.Responses;
using NotificationService.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace NotificationService.Services.Implementations;

/// <summary>
/// Implementation of the in-app notification service using SignalR
/// </summary>
public class SignalRInAppNotificationService : IInAppNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly NotificationContext _context;
    private readonly IDatabase _redis;
    private readonly ILogger<SignalRInAppNotificationService> _logger;

    public SignalRInAppNotificationService(
        IHubContext<NotificationHub> hubContext,
        NotificationContext context,
        IConnectionMultiplexer redisConnection,
        ILogger<SignalRInAppNotificationService> logger)
    {
        _hubContext = hubContext;
        _context = context;
        _redis = redisConnection.GetDatabase();
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<NotificationResponse> SendInAppNotificationAsync(SendInAppNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Save the notification to the database
            var notification = new Notification
            {
                Type = NotificationType.InApp,
                Recipient = request.UserId,
                Subject = request.Title,
                Content = request.Message,
                Priority = request.Priority,
                Status = NotificationStatus.Sent,
                UserId = request.UserId,
                Source = request.Source ?? "System",
                ReferenceId = request.ReferenceId,
                ReferenceType = request.ReferenceType,
                Metadata = request.Data != null ? JsonSerializer.Serialize(request.Data) : null,
                CreatedAt = DateTime.UtcNow,
                SentAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            // Send the notification in real time via SignalR
            await _hubContext.Clients.Group($"user_{request.UserId}").SendAsync("ReceiveNotification", new
            {
                id = notification.Id,
                title = request.Title,
                message = request.Message,
                type = request.NotificationType ?? "info",
                priority = request.Priority.ToString().ToLower(),
                data = request.Data,
                timestamp = DateTime.UtcNow,
                source = request.Source,
                referenceId = request.ReferenceId,
                referenceType = request.ReferenceType
            }, cancellationToken);

            // Update the unread notification counter in Redis
            await UpdateUnreadCountAsync(request.UserId, cancellationToken);

            _logger.LogInformation("In-app notification sent to user {UserId}: {Title}", request.UserId, request.Title);

            return new NotificationResponse
            {
                Success = true,
                MessageId = notification.Id.ToString(),
                ExternalId = notification.Id.ToString(),
                Status = "delivered"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending in-app notification to user {UserId}", request.UserId);
            return NotificationResponse.CreateError($"In-app notification send error: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<int> SendBroadcastNotificationAsync(string title, string message, string type = "info", Dictionary<string, object>? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Send to all connected users
            await _hubContext.Clients.All.SendAsync("ReceiveBroadcast", new
            {
                title,
                message,
                type,
                data,
                timestamp = DateTime.UtcNow,
                source = "System"
            }, cancellationToken);

            // Count connected users (approximate, via Redis)
            var connectedUsers = await GetConnectedUsersCountAsync();

            _logger.LogInformation("Broadcast notification sent: {Title} - Users reached: {UserCount}", title, connectedUsers);

            return connectedUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending broadcast notification");
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<int> SendToGroupAsync(List<string> userIds, string title, string message, string type = "info", Dictionary<string, object>? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            int sentCount = 0;

            foreach (var userId in userIds)
            {
                try
                {
                    // Send to a specific user
                    await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", new
                    {
                        title,
                        message,
                        type,
                        data,
                        timestamp = DateTime.UtcNow,
                        source = "System",
                        userId
                    }, cancellationToken);

                    // Save persistent notification
                    var notification = new Notification
                    {
                        Type = NotificationType.InApp,
                        Recipient = userId,
                        Subject = title,
                        Content = message,
                        Priority = NotificationPriority.Normal,
                        Status = NotificationStatus.Sent,
                        UserId = userId,
                        Source = "System",
                        Metadata = data != null ? JsonSerializer.Serialize(data) : null,
                        CreatedAt = DateTime.UtcNow,
                        SentAt = DateTime.UtcNow
                    };

                    _context.Notifications.Add(notification);
                    sentCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error sending group notification for user {UserId}", userId);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Update unread counters for all users
            foreach (var userId in userIds)
            {
                await UpdateUnreadCountAsync(userId, cancellationToken);
            }

            _logger.LogInformation("Group notification sent: {Title} - Users reached: {SentCount}/{TotalCount}",
                title, sentCount, userIds.Count);

            return sentCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending group notification");
            return 0;
        }
    }

    /// <inheritdoc/>
    public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId &&
                           n.Type == NotificationType.InApp &&
                           n.Status != NotificationStatus.Read)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving unread notifications for user {UserId}", userId);
            return new List<Notification>();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

            if (notification == null)
            {
                return false;
            }

            notification.Status = NotificationStatus.Read;
            notification.DeliveredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Update the counter in Redis
            await UpdateUnreadCountAsync(userId, cancellationToken);

            // Notify the client of the update
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("NotificationRead", new
            {
                notificationId,
                timestamp = DateTime.UtcNow
            }, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {NotificationId} as read for user {UserId}",
                notificationId, userId);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId &&
                           n.Type == NotificationType.InApp &&
                           n.Status != NotificationStatus.Read)
                .ToListAsync(cancellationToken);

            foreach (var notification in unreadNotifications)
            {
                notification.Status = NotificationStatus.Read;
                notification.DeliveredAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Update the counter in Redis
            await _redis.StringSetAsync($"unread_count_{userId}", "0");

            // Notify the client
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("AllNotificationsRead", new
            {
                count = unreadNotifications.Count,
                timestamp = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Marked {Count} notifications as read for user {UserId}",
                unreadNotifications.Count, userId);

            return unreadNotifications.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    /// Updates the unread notification counter in Redis
    /// </summary>
    private async Task UpdateUnreadCountAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId &&
                                n.Type == NotificationType.InApp &&
                                n.Status != NotificationStatus.Read,
                           cancellationToken);

            await _redis.StringSetAsync($"unread_count_{userId}", unreadCount.ToString());

            // Notify the updated counter
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("UnreadCountUpdate", new
            {
                count = unreadCount,
                timestamp = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating unread counter for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Gets the approximate count of connected users
    /// </summary>
    private async Task<int> GetConnectedUsersCountAsync()
    {
        try
        {
            var keys = _redis.Multiplexer.GetServer(_redis.Multiplexer.GetEndPoints().First())
                .Keys(pattern: "user_connections_*");

            return keys.Count();
        }
        catch
        {
            return 0;
        }
    }
}

/// <summary>
/// SignalR hub for real-time notifications
/// </summary>
public class NotificationHub : Hub
{
    private readonly IDatabase _redis;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(IConnectionMultiplexer redisConnection, ILogger<NotificationHub> logger)
    {
        _redis = redisConnection.GetDatabase();
        _logger = logger;
    }

    /// <summary>
    /// Associates a connection with a user
    /// </summary>
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        await _redis.StringSetAsync($"connection_{Context.ConnectionId}", userId);
        await _redis.SetAddAsync($"user_connections_{userId}", Context.ConnectionId);

        _logger.LogDebug("User {UserId} associated with connection {ConnectionId}", userId, Context.ConnectionId);
    }

    /// <summary>
    /// Removes a connection from a user
    /// </summary>
    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        await _redis.KeyDeleteAsync($"connection_{Context.ConnectionId}");
        await _redis.SetRemoveAsync($"user_connections_{userId}", Context.ConnectionId);

        _logger.LogDebug("User {UserId} removed from connection {ConnectionId}", userId, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Clean up Redis mappings when the user disconnects
        var userId = await _redis.StringGetAsync($"connection_{Context.ConnectionId}");
        if (userId.HasValue)
        {
            await _redis.SetRemoveAsync($"user_connections_{userId}", Context.ConnectionId);
            await _redis.KeyDeleteAsync($"connection_{Context.ConnectionId}");

            _logger.LogDebug("Connection {ConnectionId} cleaned up for user {UserId}", Context.ConnectionId, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
