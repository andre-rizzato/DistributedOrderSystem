namespace NotificationService.Models.Requests;

/// <summary>
/// Request to send a generic notification
/// </summary>
public class SendNotificationRequest
{
    /// <summary>
    /// Type of notification to send
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Recipient (email, phone number, user ID, etc.)
    /// </summary>
    public required string Recipient { get; set; }

    /// <summary>
    /// Notification subject/title
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Notification content
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// HTML content (optional, for email)
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// Notification priority
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// When to send the notification (null = immediately)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Reference ID for tracking
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Reference type
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Service that generated the notification
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// ID of the recipient user
    /// </summary>
    public string? UserId { get; set; }
}

/// <summary>
/// Request specific to sending email
/// </summary>
public class SendEmailRequest
{
    /// <summary>
    /// Email recipient
    /// </summary>
    public required string To { get; set; }

    /// <summary>
    /// CC recipients
    /// </summary>
    public List<string>? Cc { get; set; }

    /// <summary>
    /// BCC recipients
    /// </summary>
    public List<string>? Bcc { get; set; }

    /// <summary>
    /// Email subject
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Plain text content
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// HTML content
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// Plain text content (alias, for compatibility)
    /// </summary>
    public string? Content
    {
        get => TextContent;
        set => TextContent = value;
    }

    /// <summary>
    /// Attachments
    /// </summary>
    public List<EmailAttachment>? Attachments { get; set; }

    /// <summary>
    /// Template to use
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Variables for the template
    /// </summary>
    public Dictionary<string, string>? TemplateVariables { get; set; }

    /// <summary>
    /// Email priority
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}

/// <summary>
/// Email attachment
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// File name
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// MIME type
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// File content in base64
    /// </summary>
    public required string Content { get; set; }
}

/// <summary>
/// Request to send an SMS
/// </summary>
public class SendSmsRequest
{
    /// <summary>
    /// Recipient phone number (international format)
    /// </summary>
    public required string PhoneNumber { get; set; }

    /// <summary>
    /// SMS message (max 160 characters per single SMS)
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Template to use
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Variables for the template
    /// </summary>
    public Dictionary<string, string>? TemplateVariables { get; set; }

    /// <summary>
    /// SMS priority
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// When to send the SMS
    /// </summary>
    public DateTime? ScheduledAt { get; set; }
}

/// <summary>
/// Request for push notifications
/// </summary>
public class SendPushNotificationRequest
{
    /// <summary>
    /// Device token or topic
    /// </summary>
    public required string Target { get; set; }

    /// <summary>
    /// Device token (alias for Target)
    /// </summary>
    public string DeviceToken
    {
        get => Target;
        set => Target = value;
    }

    /// <summary>
    /// Notification title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Notification body
    /// </summary>
    public required string Body { get; set; }

    /// <summary>
    /// Notification icon
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Notification image
    /// </summary>
    public string? Image { get; set; }

    /// <summary>
    /// Image URL (alias for Image)
    /// </summary>
    public string? ImageUrl
    {
        get => Image;
        set => Image = value;
    }

    /// <summary>
    /// Action when the notification is clicked
    /// </summary>
    public string? ClickAction { get; set; }

    /// <summary>
    /// Additional data
    /// </summary>
    public Dictionary<string, string>? Data { get; set; }

    /// <summary>
    /// Badge count (iOS)
    /// </summary>
    public int? Badge { get; set; }

    /// <summary>
    /// Notification sound
    /// </summary>
    public string? Sound { get; set; } = "default";
}

/// <summary>
/// Request for in-app notifications
/// </summary>
public class SendInAppNotificationRequest
{
    /// <summary>
    /// Recipient user ID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Notification title
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Notification message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Notification type/category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Notification type (alias for Category)
    /// </summary>
    public string? NotificationType
    {
        get => Category;
        set => Category = value;
    }

    /// <summary>
    /// Associated URL or action
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Notification icon
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Additional data
    /// </summary>
    public Dictionary<string, string>? Data { get; set; }

    /// <summary>
    /// Notification priority
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Service that generated the notification
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Reference ID for tracking
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Reference type
    /// </summary>
    public string? ReferenceType { get; set; }
}

/// <summary>
/// Request to send a notification via a template
/// </summary>
public class SendTemplateNotificationRequest
{
    /// <summary>
    /// Template name
    /// </summary>
    public required string TemplateName { get; set; }

    /// <summary>
    /// Recipient
    /// </summary>
    public required string Recipient { get; set; }

    /// <summary>
    /// Variables to substitute the template placeholders
    /// </summary>
    public required Dictionary<string, string> Variables { get; set; }

    /// <summary>
    /// Notification type (if not specified in the template)
    /// </summary>
    public NotificationType? Type { get; set; }

    /// <summary>
    /// Notification priority
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// User ID (optional)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Service that generated the notification
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Reference ID for tracking
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Reference type
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// When to send the notification
    /// </summary>
    public DateTime? ScheduledAt { get; set; }
}

/// <summary>
/// Request for multiple (bulk) notifications
/// </summary>
public class SendBulkNotificationRequest
{
    /// <summary>
    /// List of recipients
    /// </summary>
    public required List<string> Recipients { get; set; }

    /// <summary>
    /// Notification type
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Subject/title
    /// </summary>
    public required string Subject { get; set; }

    /// <summary>
    /// Content
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Template to use (optional)
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Common variables for all recipients
    /// </summary>
    public Dictionary<string, string>? CommonVariables { get; set; }

    /// <summary>
    /// Per-recipient specific variables
    /// </summary>
    public Dictionary<string, Dictionary<string, string>>? RecipientVariables { get; set; }

    /// <summary>
    /// Notification priority
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}
