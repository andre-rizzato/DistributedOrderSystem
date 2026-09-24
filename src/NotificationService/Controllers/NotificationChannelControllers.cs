using Microsoft.AspNetCore.Mvc;
using NotificationService.Models;
using NotificationService.Models.Requests;
using NotificationService.Models.Responses;
using NotificationService.Services;

namespace NotificationService.Controllers;

/// <summary>
/// Controller for handling SMS notification sending
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
    /// Sends a single SMS
    /// </summary>
    /// <param name="request">SMS data to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send result</returns>
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
            _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", request.PhoneNumber);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while sending the SMS",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Sends multiple SMS messages
    /// </summary>
    /// <param name="requests">List of SMS messages to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send results</returns>
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
                return BadRequest("Request list is invalid or empty");
            }

            if (requests.Count > 100) // Safety limit
            {
                return BadRequest("Maximum 100 SMS messages per bulk request");
            }

            var result = await _smsService.SendBulkSmsAsync(requests, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk SMS for {Count} requests", requests?.Count ?? 0);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while sending the bulk SMS",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Checks the status of an SMS
    /// </summary>
    /// <param name="messageId">External message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message status</returns>
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
                return BadRequest("Message ID is required");
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
            _logger.LogError(ex, "Error retrieving status for SMS {MessageId}", messageId);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while retrieving the status",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Validates a phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number to validate</param>
    /// <returns>Validation result</returns>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(object), 200)]
    public ActionResult ValidatePhoneNumber([FromQuery] string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return BadRequest("Phone number is required");
        }

        var isValid = _smsService.ValidatePhoneNumber(phoneNumber);

        return Ok(new
        {
            phoneNumber,
            isValid,
            message = isValid ? "Valid number" : "Invalid number. Required format: +[country code][number]"
        });
    }
}

/// <summary>
/// Controller for handling email sending
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
    /// Sends a single email
    /// </summary>
    /// <param name="request">Email data to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send result</returns>
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
            _logger.LogError(ex, "Error sending email to {Email}", request.To);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while sending the email",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Sends multiple emails
    /// </summary>
    /// <param name="requests">List of emails to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send results</returns>
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
                return BadRequest("Request list is invalid or empty");
            }

            if (requests.Count > 50) // Safety limit for email
            {
                return BadRequest("Maximum 50 emails per bulk request");
            }

            var result = await _emailService.SendBulkEmailAsync(requests, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk email for {Count} requests", requests?.Count ?? 0);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while sending the bulk email",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Sends an email with attachments
    /// </summary>
    /// <param name="request">Email data</param>
    /// <param name="attachments">Attachments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Send result</returns>
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

            // Convert base64 attachments
            var attachments = request.Attachments?.Select(a => new NotificationService.Services.EmailAttachment
            {
                FileName = a.FileName,
                Content = Convert.FromBase64String(a.ContentBase64),
                ContentType = a.ContentType,
                IsInline = a.IsInline,
                ContentId = a.ContentId
            }).ToList() ?? new List<NotificationService.Services.EmailAttachment>();

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
            _logger.LogError(ex, "Error sending email with attachments to {Email}", request.EmailRequest?.To);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Internal server error",
                Detail = "An error occurred while sending the email with attachments",
                Status = 500
            });
        }
    }

    /// <summary>
    /// Validates an email address
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>Validation result</returns>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(object), 200)]
    public ActionResult ValidateEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest("Email is required");
        }

        var isValid = _emailService.ValidateEmail(email);

        return Ok(new
        {
            email,
            isValid,
            message = isValid ? "Valid email" : "Invalid email address"
        });
    }
}

/// <summary>
/// DTO for email with attachments
/// </summary>
public class SendEmailWithAttachmentsRequest
{
    public required SendEmailRequest EmailRequest { get; set; }
    public List<EmailAttachmentDto>? Attachments { get; set; }
}

/// <summary>
/// DTO for an email attachment
/// </summary>
public class EmailAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentBase64 { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public bool IsInline { get; set; } = false;
    public string? ContentId { get; set; }
}
