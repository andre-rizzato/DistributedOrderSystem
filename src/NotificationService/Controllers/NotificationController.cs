using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;
using NotificationService.Services;

namespace NotificationService.Controllers;

/// <summary>
/// Controller principale per la gestione delle notifiche del sistema
/// Fornisce endpoint unificati per l'invio di notifiche tramite template e gestione centralizzata
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
    /// Invia notifica usando un template predefinito
    /// Questo è l'endpoint principale per l'invio di notifiche strutturate
    /// </summary>
    /// <param name="request">Richiesta di notifica con template</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'invio</returns>
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

            // Valida che il template esista e le variabili siano corrette
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
                _logger.LogInformation("Notifica template {TemplateName} inviata con successo a {Recipient}", 
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
            _logger.LogError(ex, "Errore durante l'invio notifica template {TemplateName} a {Recipient}", 
                request.TemplateName, request.Recipient);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'invio della notifica",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Invia notifica diretta senza template
    /// Utile per notifiche ad-hoc o personalizzate
    /// </summary>
    /// <param name="request">Richiesta di notifica diretta</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'invio</returns>
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
                _logger.LogInformation("Notifica diretta {Type} inviata con successo a {Recipient}", 
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
            _logger.LogError(ex, "Errore durante l'invio notifica diretta {Type} a {Recipient}", 
                request.Type, request.Recipient);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'invio della notifica",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Invia notifiche multiple in batch
    /// Ottimizzato per l'invio di grandi volumi di notifiche
    /// </summary>
    /// <param name="requests">Lista di richieste notifica</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultati degli invii</returns>
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
                return BadRequest("Lista richieste non valida o vuota");
            }

            if (requests.Count > 1000) // Limite di sicurezza
            {
                return BadRequest("Massimo 1000 notifiche per richiesta bulk");
            }

            var result = await _notificationService.SendBulkNotificationsAsync(requests, cancellationToken);
            
            _logger.LogInformation("Invio bulk completato: {SuccessCount}/{TotalCount} notifiche inviate", 
                result.SuccessCount, result.TotalRequests);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio bulk di {Count} notifiche", requests?.Count ?? 0);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'invio bulk delle notifiche",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Programma notifica per invio futuro
    /// Utilizza Hangfire per gestire la schedulazione
    /// </summary>
    /// <param name="request">Richiesta di notifica programmata</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>ID della notifica programmata</returns>
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
                return BadRequest("La data di programmazione deve essere futura");
            }

            var notificationId = await _notificationService.ScheduleNotificationAsync(
                request.NotificationRequest, 
                request.ScheduledAt, 
                cancellationToken);
            
            _logger.LogInformation("Notifica programmata per {ScheduledAt}. ID: {NotificationId}", 
                request.ScheduledAt, notificationId);
            
            return Ok(new
            {
                success = true,
                notificationId,
                scheduledAt = request.ScheduledAt,
                message = "Notifica programmata con successo"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la programmazione notifica per {ScheduledAt}", request.ScheduledAt);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante la programmazione della notifica",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Annulla notifica programmata
    /// Rimuove la notifica dalla coda di Hangfire
    /// </summary>
    /// <param name="notificationId">ID della notifica da annullare</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'annullamento</returns>
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
                _logger.LogInformation("Notifica programmata {NotificationId} annullata", notificationId);
                return Ok(new
                {
                    success = true,
                    notificationId,
                    message = "Notifica programmata annullata con successo"
                });
            }
            else
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Notification not found",
                    Detail = "Notifica programmata non trovata o già eseguita",
                    Status = 404
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'annullamento notifica {NotificationId}", notificationId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'annullamento della notifica",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Ottieni stato di una notifica specifica
    /// Include informazioni su invio, consegna ed eventuali errori
    /// </summary>
    /// <param name="notificationId">ID della notifica</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Stato dettagliato della notifica</returns>
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
                    Detail = $"Notifica con ID {notificationId} non trovata",
                    Status = 404
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero stato notifica {NotificationId}", notificationId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante il recupero dello stato",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Ottieni notifiche di un utente con paginazione
    /// Include filtri per tipo e stato delle notifiche
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="page">Numero di pagina (default: 1)</param>
    /// <param name="pageSize">Dimensione pagina (default: 20, max: 100)</param>
    /// <param name="type">Filtro per tipo notifica (opzionale)</param>
    /// <param name="status">Filtro per stato notifica (opzionale)</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Lista paginata delle notifiche utente</returns>
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
                return BadRequest("User ID è richiesto");
            }

            if (page <= 0)
            {
                return BadRequest("Il numero di pagina deve essere maggiore di 0");
            }

            if (pageSize <= 0 || pageSize > 100)
            {
                return BadRequest("La dimensione della pagina deve essere tra 1 e 100");
            }

            var notifications = await _notificationService.GetUserNotificationsAsync(
                userId, page, pageSize, cancellationToken);
            
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero notifiche per utente {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante il recupero delle notifiche",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Ottieni statistiche delle notifiche
    /// Include conteggi per tipo, stato e trend temporali
    /// </summary>
    /// <param name="userId">ID utente per statistiche specifiche (opzionale)</param>
    /// <param name="fromDate">Data inizio periodo (opzionale)</param>
    /// <param name="toDate">Data fine periodo (opzionale)</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Statistiche dettagliate delle notifiche</returns>
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
            // Validazione date
            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            {
                return BadRequest("La data di inizio deve essere precedente alla data di fine");
            }

            // Limite periodo massimo (1 anno)
            if (fromDate.HasValue && toDate.HasValue && (toDate - fromDate).Value.TotalDays > 365)
            {
                return BadRequest("Il periodo massimo per le statistiche è di 1 anno");
            }

            var stats = await _notificationService.GetNotificationStatsAsync(
                userId, fromDate, toDate, cancellationToken);
            
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero statistiche notifiche per utente {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante il recupero delle statistiche",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Ritenta l'invio di una notifica fallita
    /// Utilizza la stessa configurazione della notifica originale
    /// </summary>
    /// <param name="notificationId">ID della notifica da ritentare</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato del nuovo tentativo</returns>
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
                _logger.LogInformation("Retry notifica {NotificationId} completato con successo", notificationId);
                return Ok(result);
            }
            else if (result.Error?.Contains("non trovata") == true)
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
            _logger.LogError(ex, "Errore durante il retry notifica {NotificationId}", notificationId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante il retry della notifica",
                Status = 500
            });
        }
    }
}

/// <summary>
/// DTO per richiesta di notifica programmata
/// </summary>
public class ScheduleNotificationRequest
{
    /// <summary>Dati della notifica da programmare</summary>
    public SendNotificationRequest NotificationRequest { get; set; } = new();
    
    /// <summary>Data e ora di invio programmato (UTC)</summary>
    public DateTime ScheduledAt { get; set; }
}