namespace NotificationService.Models;

/// <summary>
/// Notification types supported by the system.
/// Each type corresponds to a specific communication channel.
/// </summary>
public enum NotificationType
{
    Email = 1,
    SMS = 2,
    Push = 3,
    InApp = 4,
    WebSocket = 5
}

/// <summary>
/// Notification priority, used to determine processing order.
/// High-priority notifications are processed first.
/// </summary>
public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Notification statuses.
/// </summary>
public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    Delivered = 4,
    Read = 5,
    Scheduled = 6
}

/// <summary>
/// Main notification entity - represents a single notification
/// in the system with all the metadata needed for tracking and audit.
/// </summary>
public class Notification
{
    public long Id { get; set; }

    /// <summary>
    /// Notification type (SMS, Email, Push, etc.)
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Notification recipient
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
    /// HTML content (for email)
    /// </summary>
    public string? HtmlContent { get; set; }

    /// <summary>
    /// Notification priority
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Current notification status
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>
    /// ID of the user that generated the notification
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Originating channel or service
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// References for tracking (order ID, product ID, etc.)
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Reference type (order, product, user, etc.)
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Additional metadata in JSON format
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// When to send the notification (for scheduled notifications)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// When the notification was sent
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// When the notification was delivered/read
    /// </summary>
    public DateTime? DeliveredAt { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of send attempts
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// Last error message
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// External provider ID (Twilio SID, etc.)
    /// </summary>
    public string? ExternalId { get; set; }
}

/// <summary>
/// Template for recurring notifications
/// </summary>
public class NotificationTemplate
{
    public long Id { get; set; }

    /// <summary>
    /// Unique template name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Template description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Notification type
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Subject template with placeholders
    /// </summary>
    public required string SubjectTemplate { get; set; }

    /// <summary>
    /// Content template with placeholders
    /// </summary>
    public required string ContentTemplate { get; set; }

    /// <summary>
    /// HTML template for email
    /// </summary>
    public string? HtmlTemplate { get; set; }

    /// <summary>
    /// Variables available in the template
    /// </summary>
    public string? Variables { get; set; }

    /// <summary>
    /// Whether the template is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Per-user notification preferences
/// </summary>
public class NotificationPreference
{
    public long Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Notification type
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Notification category (orders, marketing, system, etc.)
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Whether the user has enabled this notification type
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Preferred channel for this category
    /// </summary>
    public string? PreferredChannel { get; set; }

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modified date
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Notification send log for audit purposes
/// </summary>
public class NotificationLog
{
    public long Id { get; set; }

    /// <summary>
    /// Reference to the notification
    /// </summary>
    public long NotificationId { get; set; }

    /// <summary>
    /// Navigation property to the notification
    /// </summary>
    public Notification? Notification { get; set; }

    /// <summary>
    /// Action performed (sent, failed, retry, etc.)
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Action details
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Provider used (twilio, smtp, firebase, etc.)
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Timestamp of the action
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
