using Hangfire;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Services;
using System.Text.Json;

namespace NotificationService.Services.Implementations;

/// <summary>
/// Implementazione del servizio principale per la gestione delle notifiche
/// Coordina tutti i provider di notifica e gestisce la logica di business centrale
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
    /// Invia notifica utilizzando un template predefinito
    /// Il template viene renderizzato con le variabili fornite prima dell'invio
    /// </summary>
    public async Task<NotificationResponse> SendNotificationAsync(SendTemplateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Inizio invio notifica template {TemplateName} a {Recipient}", 
                request.TemplateName, request.Recipient);

            // Renderizza il template con le variabili
            var renderedTemplate = await _templateService.RenderTemplateAsync(
                request.TemplateName, request.Variables, cancellationToken);

            // Crea richiesta diretta dal template renderizzato
            var directRequest = new SendNotificationRequest
            {
                Type = request.Type,
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
            _logger.LogError(ex, "Errore durante l'invio notifica template {TemplateName}", request.TemplateName);
            return NotificationResponse.Error($"Errore template: {ex.Message}");
        }
    }

    /// <summary>
    /// Invia notifica diretta senza utilizzo di template
    /// Gestisce la logica di routing verso il provider appropriato
    /// </summary>
    public async Task<NotificationResponse> SendDirectNotificationAsync(SendNotificationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Inizio invio notifica diretta {Type} a {Recipient}", 
                request.Type, request.Recipient);

            // Salva la notifica nel database per audit
            var notification = await CreateNotificationEntityAsync(request, cancellationToken);

            // Se è programmata, delega a Hangfire
            if (request.ScheduledAt.HasValue && request.ScheduledAt > DateTime.UtcNow)
            {
                return await ScheduleNotificationInternalAsync(notification, request.ScheduledAt.Value);
            }

            // Invio immediato tramite provider specifico
            var result = await SendThroughProviderAsync(request, notification, cancellationToken);
            
            // Aggiorna stato notifica in base al risultato
            await UpdateNotificationStatusAsync(notification, result, cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio notifica diretta {Type} a {Recipient}", 
                request.Type, request.Recipient);
            return NotificationResponse.Error($"Errore invio: {ex.Message}");
        }
    }

    /// <summary>
    /// Invia multiple notifiche in batch per ottimizzare le performance
    /// Raggruppa le notifiche per tipo e utilizza le funzionalità batch dei provider
    /// </summary>
    public async Task<BulkNotificationResponse> SendBulkNotificationsAsync(List<SendNotificationRequest> requests, CancellationToken cancellationToken = default)
    {
        var response = new BulkNotificationResponse
        {
            TotalRequests = requests.Count
        };

        try
        {
            _logger.LogInformation("Inizio invio bulk di {Count} notifiche", requests.Count);

            // Raggruppa per tipo per ottimizzare l'invio
            var groupedRequests = requests.GroupBy(r => r.Type);

            foreach (var group in groupedRequests)
            {
                var groupResults = await ProcessBulkGroupAsync(group.ToList(), cancellationToken);
                
                response.SuccessCount += groupResults.SuccessCount;
                response.FailureCount += groupResults.FailureCount;
                response.Results.AddRange(groupResults.Results);
            }

            response.Success = response.SuccessCount > 0;
            
            _logger.LogInformation("Invio bulk completato: {SuccessCount}/{TotalCount}", 
                response.SuccessCount, response.TotalRequests);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio bulk di {Count} notifiche", requests.Count);
            
            // Marca tutte come fallite se errore generale
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
    /// Programma una notifica per invio futuro utilizzando Hangfire
    /// </summary>
    public async Task<int> ScheduleNotificationAsync(SendNotificationRequest request, DateTime scheduledAt, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Programmazione notifica {Type} per {ScheduledAt}", 
                request.Type, scheduledAt);

            // Crea entità notifica con stato programmato
            var notification = await CreateNotificationEntityAsync(request, cancellationToken);
            notification.Status = NotificationStatus.Scheduled;
            notification.ScheduledAt = scheduledAt;
            
            await _context.SaveChangesAsync(cancellationToken);

            // Programma job Hangfire
            var jobId = _backgroundJobClient.Schedule(
                () => ExecuteScheduledNotificationAsync(notification.Id),
                scheduledAt);

            // Salva l'ID del job per eventuali cancellazioni
            notification.ExternalId = jobId;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Notifica {NotificationId} programmata per {ScheduledAt} con job {JobId}", 
                notification.Id, scheduledAt, jobId);

            return notification.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la programmazione notifica per {ScheduledAt}", scheduledAt);
            throw;
        }
    }

    /// <summary>
    /// Annulla una notifica programmata rimuovendo il job da Hangfire
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

            // Cancella job Hangfire se presente
            if (!string.IsNullOrEmpty(notification.ExternalId))
            {
                _backgroundJobClient.Delete(notification.ExternalId);
            }

            // Aggiorna stato notifica
            notification.Status = NotificationStatus.Cancelled;
            notification.ErrorMessage = "Notifica annullata dall'utente";
            
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Notifica programmata {NotificationId} annullata", notificationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'annullamento notifica {NotificationId}", notificationId);
            return false;
        }
    }

    /// <summary>
    /// Ottiene lo stato dettagliato di una notifica specifica
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
                Status = notification.Status.ToString().ToLower(),
                Type = notification.Type.ToString(),
                Recipient = notification.Recipient,
                CreatedAt = notification.CreatedAt,
                SentAt = notification.SentAt,
                DeliveredAt = notification.DeliveredAt,
                ScheduledAt = notification.ScheduledAt,
                RetryCount = notification.RetryCount,
                ErrorMessage = notification.ErrorMessage,
                ExternalId = notification.ExternalId,
                UpdatedAt = notification.DeliveredAt ?? notification.SentAt ?? notification.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero stato notifica {NotificationId}", notificationId);
            return new NotificationStatusResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Ottiene le notifiche di un utente con paginazione
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
                Success = true,
                Notifications = notifications,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero notifiche utente {UserId}", userId);
            return new NotificationListResponse
            {
                Success = false,
                Error = ex.Message,
                Notifications = new List<Notification>()
            };
        }
    }

    /// <summary>
    /// Genera statistiche dettagliate sulle notifiche
    /// </summary>
    public async Task<NotificationStatsResponse> GetNotificationStatsAsync(string? userId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Notifications.AsQueryable();

            // Filtri opzionali
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(n => n.UserId == userId);
            
            if (fromDate.HasValue)
                query = query.Where(n => n.CreatedAt >= fromDate.Value);
            
            if (toDate.HasValue)
                query = query.Where(n => n.CreatedAt <= toDate.Value);

            // Calcolo statistiche
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
                Success = true,
                TotalNotifications = totalCount,
                StatusCounts = statusStats,
                TypeCounts = typeStats,
                PriorityCounts = priorityStats,
                Period = new
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    UserId = userId
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il calcolo statistiche notifiche");
            return new NotificationStatsResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Ritenta l'invio di una notifica fallita
    /// </summary>
    public async Task<NotificationResponse> RetryNotificationAsync(int notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

            if (notification == null)
            {
                return NotificationResponse.Error("Notifica non trovata");
            }

            if (notification.Status == NotificationStatus.Delivered)
            {
                return NotificationResponse.Error("La notifica è già stata consegnata con successo");
            }

            // Incrementa contatore retry
            notification.RetryCount++;
            notification.ErrorMessage = null;

            // Ricrea la richiesta originale
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

            // Riprova l'invio
            var result = await SendThroughProviderAsync(request, notification, cancellationToken);
            
            // Aggiorna stato
            await UpdateNotificationStatusAsync(notification, result, cancellationToken);

            _logger.LogInformation("Retry notifica {NotificationId} completato. Successo: {Success}", 
                notificationId, result.Success);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante retry notifica {NotificationId}", notificationId);
            return NotificationResponse.Error($"Errore retry: {ex.Message}");
        }
    }

    // Metodi privati per la logica interna

    /// <summary>
    /// Crea entità notifica nel database per audit e tracking
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
    /// Instrada la notifica al provider appropriato in base al tipo
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
                    Message = request.Content,
                    Source = request.Source,
                    Metadata = request.Metadata
                }, cancellationToken),

                NotificationType.Email => await _emailService.SendEmailAsync(new SendEmailRequest
                {
                    To = request.Recipient,
                    Subject = request.Subject,
                    Content = request.Content,
                    HtmlContent = request.HtmlContent,
                    Source = request.Source,
                    Metadata = request.Metadata
                }, cancellationToken),

                NotificationType.Push => await _pushService.SendPushNotificationAsync(new SendPushNotificationRequest
                {
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

                _ => NotificationResponse.Error($"Tipo notifica {request.Type} non supportato")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio tramite provider {Type}", request.Type);
            return NotificationResponse.Error($"Errore provider: {ex.Message}");
        }
    }

    /// <summary>
    /// Aggiorna lo stato della notifica nel database in base al risultato dell'invio
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
                
                // Per notifiche in-app, marca come consegnate immediatamente
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
            _logger.LogError(ex, "Errore durante l'aggiornamento stato notifica {NotificationId}", notification.Id);
        }
    }

    /// <summary>
    /// Elabora un gruppo di notifiche dello stesso tipo in batch
    /// </summary>
    private async Task<BulkNotificationResponse> ProcessBulkGroupAsync(List<SendNotificationRequest> requests, CancellationToken cancellationToken)
    {
        var response = new BulkNotificationResponse
        {
            TotalRequests = requests.Count
        };

        // Crea entità per tutte le notifiche del gruppo
        var notifications = new List<Notification>();
        foreach (var request in requests)
        {
            var notification = await CreateNotificationEntityAsync(request, cancellationToken);
            notifications.Add(notification);
        }

        try
        {
            // Utilizza invio bulk specifico per tipo se disponibile
            var firstRequest = requests.First();
            BulkNotificationResponse? bulkResult = null;

            switch (firstRequest.Type)
            {
                case NotificationType.SMS:
                    var smsRequests = requests.Select(r => new SendSmsRequest
                    {
                        PhoneNumber = r.Recipient,
                        Message = r.Content,
                        Source = r.Source,
                        Metadata = r.Metadata
                    }).ToList();
                    bulkResult = await _smsService.SendBulkSmsAsync(smsRequests, cancellationToken);
                    break;

                case NotificationType.Email:
                    var emailRequests = requests.Select(r => new SendEmailRequest
                    {
                        To = r.Recipient,
                        Subject = r.Subject,
                        Content = r.Content,
                        HtmlContent = r.HtmlContent,
                        Source = r.Source,
                        Metadata = r.Metadata
                    }).ToList();
                    bulkResult = await _emailService.SendBulkEmailAsync(emailRequests, cancellationToken);
                    break;

                default:
                    // Per tipi che non supportano bulk, invia singolarmente
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

            // Processa risultato bulk se disponibile
            if (bulkResult != null)
            {
                response.SuccessCount = bulkResult.SuccessCount;
                response.FailureCount = bulkResult.FailureCount;
                response.Results = bulkResult.Results;

                // Aggiorna stati nel database
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
            _logger.LogError(ex, "Errore durante elaborazione bulk gruppo {Type}", firstRequest.Type);
            
            // Marca tutte come fallite
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
    /// Programma una notifica per invio futuro
    /// </summary>
    private async Task<NotificationResponse> ScheduleNotificationInternalAsync(Notification notification, DateTime scheduledAt)
    {
        try
        {
            notification.Status = NotificationStatus.Scheduled;
            notification.ScheduledAt = scheduledAt;

            var jobId = _backgroundJobClient.Schedule(
                () => ExecuteScheduledNotificationAsync(notification.Id),
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
            _logger.LogError(ex, "Errore durante programmazione notifica {NotificationId}", notification.Id);
            return NotificationResponse.Error($"Errore programmazione: {ex.Message}");
        }
    }

    /// <summary>
    /// Esegue una notifica programmata (chiamato da Hangfire)
    /// </summary>
    [Queue("notifications")]
    public async Task ExecuteScheduledNotificationAsync(int notificationId)
    {
        try
        {
            _logger.LogInformation("Esecuzione notifica programmata {NotificationId}", notificationId);

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null || notification.Status != NotificationStatus.Scheduled)
            {
                _logger.LogWarning("Notifica programmata {NotificationId} non trovata o non in stato scheduled", notificationId);
                return;
            }

            // Ricrea richiesta originale
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

            // Esegue invio
            var result = await SendThroughProviderAsync(request, notification, CancellationToken.None);
            await UpdateNotificationStatusAsync(notification, result, CancellationToken.None);

            _logger.LogInformation("Notifica programmata {NotificationId} eseguita. Successo: {Success}", 
                notificationId, result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante esecuzione notifica programmata {NotificationId}", notificationId);
            
            // Aggiorna stato come fallita
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