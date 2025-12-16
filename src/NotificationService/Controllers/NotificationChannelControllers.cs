using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;
using NotificationService.Services;

namespace NotificationService.Controllers;

/// <summary>
/// Controller per gestire l'invio di notifiche SMS
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SmsController : ControllerBase
{
    private readonly ISmsService _smsService;
    private readonly ILogger<SmsController> _logger;

    public SmsController(ISmsService smsService, ILogger<SmsController> logger)
    {
        _smsService = smsService;
        _logger = logger;
    }

    /// <summary>
    /// Invia SMS singolo
    /// </summary>
    /// <param name="request">Dati SMS da inviare</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'invio</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationResponse>> SendSms([FromBody] SendSmsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _smsService.SendSmsAsync(request, cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "SMS sending failed",
                    Detail = result.Error,
                    Status = 400
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio SMS a {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'invio dell'SMS",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Invia SMS multipli
    /// </summary>
    /// <param name="requests">Lista di SMS da inviare</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultati degli invii</returns>
    [HttpPost("send-bulk")]
    [ProducesResponseType(typeof(BulkNotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<BulkNotificationResponse>> SendBulkSms([FromBody] List<SendSmsRequest> requests, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid || !requests.Any())
            {
                return BadRequest("Lista richieste non valida o vuota");
            }

            if (requests.Count > 100) // Limite di sicurezza
            {
                return BadRequest("Massimo 100 SMS per richiesta bulk");
            }

            var result = await _smsService.SendBulkSmsAsync(requests, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio bulk SMS per {Count} richieste", requests?.Count ?? 0);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'invio bulk degli SMS",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Verifica lo stato di un SMS
    /// </summary>
    /// <param name="messageId">ID esterno del messaggio</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Stato del messaggio</returns>
    [HttpGet("status/{messageId}")]
    [ProducesResponseType(typeof(NotificationStatusResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationStatusResponse>> GetSmsStatus(string messageId, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(messageId))
            {
                return BadRequest("Message ID è richiesto");
            }

            var result = await _smsService.GetSmsStatusAsync(messageId, cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return NotFound(new ProblemDetails
                {
                    Title = "SMS not found",
                    Detail = result.Error,
                    Status = 404
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero stato SMS {MessageId}", messageId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante il recupero dello stato",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Valida un numero di telefono
    /// </summary>
    /// <param name="phoneNumber">Numero di telefono da validare</param>
    /// <returns>Risultato della validazione</returns>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(object), 200)]
    public ActionResult ValidatePhoneNumber([FromQuery] string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return BadRequest("Phone number è richiesto");
        }

        var isValid = _smsService.ValidatePhoneNumber(phoneNumber);
        
        return Ok(new
        {
            phoneNumber,
            isValid,
            message = isValid ? "Numero valido" : "Numero non valido. Formato richiesto: +[codice paese][numero]"
        });
    }
}

/// <summary>
/// Controller per gestire l'invio di email
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(IEmailService emailService, ILogger<EmailController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Invia email singola
    /// </summary>
    /// <param name="request">Dati email da inviare</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'invio</returns>
    [HttpPost("send")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationResponse>> SendEmail([FromBody] SendEmailRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _emailService.SendEmailAsync(request, cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Email sending failed",
                    Detail = result.Error,
                    Status = 400
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio email a {Email}", request.To);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'invio dell'email",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Invia email multiple
    /// </summary>
    /// <param name="requests">Lista di email da inviare</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultati degli invii</returns>
    [HttpPost("send-bulk")]
    [ProducesResponseType(typeof(BulkNotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<BulkNotificationResponse>> SendBulkEmail([FromBody] List<SendEmailRequest> requests, CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid || !requests.Any())
            {
                return BadRequest("Lista richieste non valida o vuota");
            }

            if (requests.Count > 50) // Limite di sicurezza per email
            {
                return BadRequest("Massimo 50 email per richiesta bulk");
            }

            var result = await _emailService.SendBulkEmailAsync(requests, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio bulk email per {Count} richieste", requests?.Count ?? 0);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'invio bulk delle email",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Invia email con allegati
    /// </summary>
    /// <param name="request">Dati email</param>
    /// <param name="attachments">Allegati</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'invio</returns>
    [HttpPost("send-with-attachments")]
    [ProducesResponseType(typeof(NotificationResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 500)]
    public async Task<ActionResult<NotificationResponse>> SendEmailWithAttachments(
        [FromBody] SendEmailWithAttachmentsRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Converti base64 attachments
            var attachments = request.Attachments?.Select(a => new EmailAttachment
            {
                FileName = a.FileName,
                Content = Convert.FromBase64String(a.ContentBase64),
                ContentType = a.ContentType,
                IsInline = a.IsInline,
                ContentId = a.ContentId
            }).ToList() ?? new List<EmailAttachment>();

            var result = await _emailService.SendEmailWithAttachmentsAsync(request.EmailRequest, attachments, cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Email sending failed",
                    Detail = result.Error,
                    Status = 400
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio email con allegati a {Email}", request.EmailRequest?.To);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "Si è verificato un errore durante l'invio dell'email con allegati",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Valida un indirizzo email
    /// </summary>
    /// <param name="email">Indirizzo email da validare</param>
    /// <returns>Risultato della validazione</returns>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(object), 200)]
    public ActionResult ValidateEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email è richiesta");
        }

        var isValid = _emailService.ValidateEmail(email);
        
        return Ok(new
        {
            email,
            isValid,
            message = isValid ? "Email valida" : "Indirizzo email non valido"
        });
    }
}

/// <summary>
/// DTO per email con allegati
/// </summary>
public class SendEmailWithAttachmentsRequest
{
    public SendEmailRequest EmailRequest { get; set; } = new();
    public List<EmailAttachmentDto>? Attachments { get; set; }
}

/// <summary>
/// DTO per allegato email
/// </summary>
public class EmailAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentBase64 { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public bool IsInline { get; set; } = false;
    public string? ContentId { get; set; }
}