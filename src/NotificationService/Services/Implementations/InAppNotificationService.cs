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
/// Implementazione del servizio per notifiche in-app usando SignalR
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
            // Salva la notifica nel database
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

            // Invia notifica real-time tramite SignalR
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

            // Aggiorna contatore notifiche non lette in Redis
            await UpdateUnreadCountAsync(request.UserId, cancellationToken);

            _logger.LogInformation("Notifica in-app inviata all'utente {UserId}: {Title}", request.UserId, request.Title);

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
            _logger.LogError(ex, "Errore durante l'invio notifica in-app all'utente {UserId}", request.UserId);
            return NotificationResponse.CreateError($"Errore invio notifica in-app: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<int> SendBroadcastNotificationAsync(string title, string message, string type = "info", Dictionary<string, object>? data = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Invia a tutti gli utenti connessi
            await _hubContext.Clients.All.SendAsync("ReceiveBroadcast", new
            {
                title,
                message,
                type,
                data,
                timestamp = DateTime.UtcNow,
                source = "System"
            }, cancellationToken);

            // Conta utenti connessi (approssimativo tramite Redis)
            var connectedUsers = await GetConnectedUsersCountAsync();
            
            _logger.LogInformation("Notifica broadcast inviata: {Title} - Utenti raggiunti: {UserCount}", title, connectedUsers);
            
            return connectedUsers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio notifica broadcast");
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
                    // Invia a utente specifico
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

                    // Salva notifica persistente
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
                    _logger.LogWarning(ex, "Errore invio notifica gruppo per utente {UserId}", userId);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            // Aggiorna contatori non lette per tutti gli utenti
            foreach (var userId in userIds)
            {
                await UpdateUnreadCountAsync(userId, cancellationToken);
            }

            _logger.LogInformation("Notifica gruppo inviata: {Title} - Utenti raggiunti: {SentCount}/{TotalCount}", 
                title, sentCount, userIds.Count);
            
            return sentCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio notifica gruppo");
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
            _logger.LogError(ex, "Errore durante il recupero notifiche non lette per utente {UserId}", userId);
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
            
            // Aggiorna contatore in Redis
            await UpdateUnreadCountAsync(userId, cancellationToken);
            
            // Notifica il client dell'aggiornamento
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("NotificationRead", new
            {
                notificationId,
                timestamp = DateTime.UtcNow
            }, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la marcatura come letta della notifica {NotificationId} per utente {UserId}", 
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
            
            // Aggiorna contatore in Redis
            await _redis.StringSetAsync($"unread_count_{userId}", "0");
            
            // Notifica il client
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("AllNotificationsRead", new
            {
                count = unreadNotifications.Count,
                timestamp = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("Marcate come lette {Count} notifiche per utente {UserId}", 
                unreadNotifications.Count, userId);

            return unreadNotifications.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la marcatura di tutte le notifiche come lette per utente {UserId}", userId);
            return 0;
        }
    }

    /// <summary>
    /// Aggiorna il contatore di notifiche non lette in Redis
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
            
            // Notifica il contatore aggiornato
            await _hubContext.Clients.Group($"user_{userId}").SendAsync("UnreadCountUpdate", new
            {
                count = unreadCount,
                timestamp = DateTime.UtcNow
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Errore durante l'aggiornamento contatore non lette per utente {UserId}", userId);
        }
    }

    /// <summary>
    /// Ottiene il conteggio approssimativo degli utenti connessi
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
/// Hub SignalR per le notifiche in tempo reale
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
    /// Associa connessione a un utente
    /// </summary>
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        await _redis.StringSetAsync($"connection_{Context.ConnectionId}", userId);
        await _redis.SetAddAsync($"user_connections_{userId}", Context.ConnectionId);
        
        _logger.LogDebug("Utente {UserId} associato alla connessione {ConnectionId}", userId, Context.ConnectionId);
    }

    /// <summary>
    /// Rimuove connessione da un utente
    /// </summary>
    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        await _redis.KeyDeleteAsync($"connection_{Context.ConnectionId}");
        await _redis.SetRemoveAsync($"user_connections_{userId}", Context.ConnectionId);
        
        _logger.LogDebug("Utente {UserId} rimosso dalla connessione {ConnectionId}", userId, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Pulisci mappings Redis quando l'utente si disconnette
        var userId = await _redis.StringGetAsync($"connection_{Context.ConnectionId}");
        if (userId.HasValue)
        {
            await _redis.SetRemoveAsync($"user_connections_{userId}", Context.ConnectionId);
            await _redis.KeyDeleteAsync($"connection_{Context.ConnectionId}");
            
            _logger.LogDebug("Connessione {ConnectionId} pulita per utente {UserId}", Context.ConnectionId, userId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
