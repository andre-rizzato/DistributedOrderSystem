using NotificationService.Models;

namespace NotificationService.Configuration;

/// <summary>
/// Configuration for the SMS provider (Twilio)
/// </summary>
public class SmsSettings
{
    /// <summary>
    /// SMS provider to use (twilio, nexmo, etc.)
    /// </summary>
    public string Provider { get; set; } = "twilio";

    /// <summary>
    /// Twilio Account SID
    /// </summary>
    public required string AccountSid { get; set; }

    /// <summary>
    /// Twilio Auth Token
    /// </summary>
    public required string AuthToken { get; set; }

    /// <summary>
    /// Sender phone number
    /// </summary>
    public required string FromNumber { get; set; }

    /// <summary>
    /// Sender phone number (alias for compatibility)
    /// </summary>
    public string FromPhoneNumber => FromNumber;

    /// <summary>
    /// Webhook for status callbacks
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// SMS send timeout
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of retries
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Configuration for the Email provider (SMTP)
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// SMTP server
    /// </summary>
    public required string SmtpHost { get; set; }

    /// <summary>
    /// SMTP port
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Use SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// SMTP username
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    /// SMTP password
    /// </summary>
    public required string Password { get; set; }

    /// <summary>
    /// Default sender email
    /// </summary>
    public required string DefaultFromEmail { get; set; }

    /// <summary>
    /// Default sender name
    /// </summary>
    public string? DefaultFromName { get; set; }

    /// <summary>
    /// Reply-to email
    /// </summary>
    public string? ReplyToEmail { get; set; }

    /// <summary>
    /// Email send timeout
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Maximum number of retries
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// SMTP host (alias for compatibility)
    /// </summary>
    public string Host => SmtpHost;

    /// <summary>
    /// SMTP port (alias for compatibility)
    /// </summary>
    public int Port => SmtpPort;

    /// <summary>
    /// Use SSL (alias for compatibility)
    /// </summary>
    public bool UseSsl => EnableSsl;

    /// <summary>
    /// From email (alias for compatibility)
    /// </summary>
    public string FromEmail => DefaultFromEmail;

    /// <summary>
    /// From name (alias for compatibility)
    /// </summary>
    public string FromName => DefaultFromName ?? "ShopVerse";
}

/// <summary>
/// Configuration for Push notifications (Firebase)
/// </summary>
public class PushNotificationSettings
{
    /// <summary>
    /// Push provider (firebase, apns, etc.)
    /// </summary>
    public string Provider { get; set; } = "firebase";

    /// <summary>
    /// Path to the Firebase credentials file
    /// </summary>
    public required string FirebaseCredentialsPath { get; set; }

    /// <summary>
    /// Path to the Firebase credentials file (alias for compatibility)
    /// </summary>
    public string ServiceAccountJson => FirebaseCredentialsPath;

    /// <summary>
    /// Firebase Project ID
    /// </summary>
    public required string FirebaseProjectId { get; set; }

    /// <summary>
    /// URL for deep linking
    /// </summary>
    public string? DeepLinkUrl { get; set; }

    /// <summary>
    /// Push notification timeout
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of retries
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// General configuration for the notification service
/// </summary>
public class NotificationSettings
{
    /// <summary>
    /// Enables the notification service
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Debug mode (prints detailed logs)
    /// </summary>
    public bool DebugMode { get; set; } = false;

    /// <summary>
    /// Maximum number of notifications to process per batch
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Batch processing interval (in seconds)
    /// </summary>
    public int BatchIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Global maximum number of retries
    /// </summary>
    public int MaxGlobalRetries { get; set; } = 5;

    /// <summary>
    /// Wait time between retries (backoff)
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Multiplier for exponential backoff
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Keep notifications for N days
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// Enables automatic cleanup of old notifications
    /// </summary>
    public bool AutoCleanup { get; set; } = true;

    /// <summary>
    /// Time of day for automatic cleanup (HH:mm format)
    /// </summary>
    public string CleanupTime { get; set; } = "02:00";
}

/// <summary>
/// Configuration for the Redis cache
/// </summary>
public class RedisSettings
{
    /// <summary>
    /// Redis connection string
    /// </summary>
    public required string ConnectionString { get; set; }

    /// <summary>
    /// Key prefix
    /// </summary>
    public string KeyPrefix { get; set; } = "notifications:";

    /// <summary>
    /// Default cache TTL (in minutes)
    /// </summary>
    public int DefaultTtlMinutes { get; set; } = 60;

    /// <summary>
    /// Redis database to use
    /// </summary>
    public int Database { get; set; } = 0;
}

/// <summary>
/// Configuration for notification templates
/// </summary>
public class TemplateSettings
{
    /// <summary>
    /// Template folder path
    /// </summary>
    public string TemplatePath { get; set; } = "Templates";

    /// <summary>
    /// Default template language
    /// </summary>
    public string DefaultLanguage { get; set; } = "en";

    /// <summary>
    /// Enables template caching
    /// </summary>
    public bool EnableCache { get; set; } = true;

    /// <summary>
    /// Template cache duration (in minutes)
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 30;
}

/// <summary>
/// Configuration for webhooks and callbacks
/// </summary>
public class WebhookSettings
{
    /// <summary>
    /// Base URL for status webhooks
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Secret token for validating webhooks
    /// </summary>
    public string? SecretToken { get; set; }

    /// <summary>
    /// Webhook call timeout
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum number of retries for webhooks
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Configuration for rate limiting
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Enables rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of notifications per minute per type
    /// </summary>
    public Dictionary<NotificationType, int> LimitsPerMinute { get; set; } = new()
    {
        { NotificationType.Email, 100 },
        { NotificationType.SMS, 50 },
        { NotificationType.Push, 200 },
        { NotificationType.InApp, 500 }
    };

    /// <summary>
    /// Maximum number of notifications per user per hour
    /// </summary>
    public int PerUserPerHourLimit { get; set; } = 50;

    /// <summary>
    /// Time window for rate limiting (in minutes)
    /// </summary>
    public int WindowMinutes { get; set; } = 1;
}
