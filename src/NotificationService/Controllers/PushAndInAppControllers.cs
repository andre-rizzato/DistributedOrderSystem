using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;
using NotificationService.Models.Requests;
using NotificationService.Models.Responses;
using NotificationService.Services;

namespace NotificationService.Controllers;

/// <summary>
/// Controller per gestire notifiche push e in-app
/// Gestisce l'invio di notifiche push tramite Firebase FCM e notifiche real-time tramite SignalR
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
    /// Invia notifica push singola a un dispositivo specifico
    /// </summary>
    /// <param name="request">Dati della notifica push</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'invio</returns>
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
            _logger.LogError(ex, "Errore durante l'invio notifica push a token {Token}", request.DeviceToken);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'invio della notifica push",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Invia notifica push a tutti i dispositivi di un utente specifico
    /// Recupera automaticamente tutti i token registrati per l'utente
    /// </summary>
    /// <param name="request">Richiesta di invio all'utente</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'invio</returns>
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
            _logger.LogError(ex, "Errore durante l'invio notifica push all'utente {UserId}", request.UserId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'invio della notifica all'utente",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Invia notifica push a un topic Firebase
    /// Utile per notifiche broadcast a gruppi di utenti iscritti
    /// </summary>
    /// <param name="request">Richiesta di invio al topic</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'invio</returns>
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
            _logger.LogError(ex, "Errore durante l'invio notifica push al topic {Topic}", request.Topic);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'invio della notifica al topic",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Registra un token dispositivo per un utente
    /// Necessario per poter inviare notifiche push all'utente
    /// </summary>
    /// <param name="request">Dati registrazione token</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato della registrazione</returns>
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
                    message = "Token dispositivo registrato con successo",
                    userId = request.UserId,
                    platform = request.Platform
                });
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Token registration failed",
                    Detail = "Non è stato possibile registrare il token dispositivo",
                    Status = 400
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la registrazione token per utente {UserId}", request.UserId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante la registrazione del token",
                Status = 500
            });
        }
    }
}

/// <summary>
/// Controller per gestire notifiche in-app real-time tramite SignalR
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
    /// Invia notifica in-app a un utente specifico
    /// La notifica viene salvata nel database e inviata in real-time se l'utente è connesso
    /// </summary>
    /// <param name="request">Dati della notifica in-app</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'invio</returns>
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
            _logger.LogError(ex, "Errore durante l'invio notifica in-app all'utente {UserId}", request.UserId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'invio della notifica in-app",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Ottieni le notifiche non lette per un utente
    /// Restituisce le notifiche in-app non ancora lette con paginazione
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="limit">Numero massimo di notifiche da restituire</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Lista delle notifiche non lette</returns>
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
                return BadRequest("User ID è richiesto");
            }

            if (limit <= 0 || limit > 100)
            {
                return BadRequest("Limit deve essere tra 1 e 100");
            }

            var notifications = await _inAppService.GetUnreadNotificationsAsync(userId, limit, cancellationToken);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero notifiche non lette per utente {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante il recupero delle notifiche",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Marca una notifica come letta
    /// Aggiorna lo stato della notifica e invia conferma real-time all'utente
    /// </summary>
    /// <param name="notificationId">ID della notifica</param>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'operazione</returns>
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
                    message = "Notifica marcata come letta",
                    notificationId,
                    timestamp = DateTime.UtcNow
                });
            }
            else
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Notification not found",
                    Detail = "Notifica non trovata o non appartiene all'utente",
                    Status = 404
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la marcatura notifica {NotificationId} per utente {UserId}", 
                notificationId, request.UserId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante la marcatura della notifica",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Marca tutte le notifiche come lette per un utente
    /// Aggiorna tutte le notifiche non lette dell'utente
    /// </summary>
    /// <param name="userId">ID dell'utente</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Numero di notifiche aggiornate</returns>
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
                return BadRequest("User ID è richiesto");
            }

            var count = await _inAppService.MarkAllAsReadAsync(userId, cancellationToken);
            
            return Ok(new
            {
                success = true,
                message = $"{count} notifiche marcate come lette",
                count,
                userId,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la marcatura di tutte le notifiche per utente {UserId}", userId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante la marcatura delle notifiche",
                Status = 500
            });
        }
    }
}

// DTO aggiuntivi per i controller Push e InApp

/// <summary>
/// Richiesta per invio push a utente
/// </summary>
public class SendPushToUserRequest
{
    /// <summary>ID dell'utente destinatario</summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>Titolo della notifica</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>Corpo del messaggio</summary>
    public string Body { get; set; } = string.Empty;
    
    /// <summary>Dati aggiuntivi opzionali</summary>
    public Dictionary<string, string>? Data { get; set; }
}

/// <summary>
/// Richiesta per invio push a topic
/// </summary>
public class SendPushToTopicRequest
{
    /// <summary>Nome del topic Firebase</summary>
    public string Topic { get; set; } = string.Empty;
    
    /// <summary>Titolo della notifica</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>Corpo del messaggio</summary>
    public string Body { get; set; } = string.Empty;
    
    /// <summary>Dati aggiuntivi opzionali</summary>
    public Dictionary<string, string>? Data { get; set; }
}

/// <summary>
/// Richiesta per registrazione token dispositivo
/// </summary>
public class RegisterDeviceTokenRequest
{
    /// <summary>ID dell'utente</summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>Token del dispositivo Firebase</summary>
    public string DeviceToken { get; set; } = string.Empty;
    
    /// <summary>Piattaforma del dispositivo (iOS, Android)</summary>
    public string Platform { get; set; } = string.Empty;
}

/// <summary>
/// Richiesta per marcare notifica come letta
/// </summary>
public class MarkAsReadRequest
{
    /// <summary>ID dell'utente</summary>
    public string UserId { get; set; } = string.Empty;
}