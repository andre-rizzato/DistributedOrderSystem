using NotificationService.Models;
using NotificationService.Models.Requests;
using NotificationService.Models.Responses;

namespace NotificationService.Services;

/// <summary>
/// Interface for sending SMS
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends a single SMS
    /// </summary>
    /// <param name="request">SMS send request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<NotificationResponse> SendSmsAsync(SendSmsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple SMS messages in batch
    /// </summary>
    /// <param name="requests">List of SMS requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation results</returns>
    Task<BulkNotificationResponse> SendBulkSmsAsync(List<SendSmsRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the status of a sent SMS
    /// </summary>
    /// <param name="externalId">External message ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Message status</returns>
    Task<NotificationStatusResponse> GetSmsStatusAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a phone number
    /// </summary>
    /// <param name="phoneNumber">Phone number</param>
    /// <returns>True if valid</returns>
    bool ValidatePhoneNumber(string phoneNumber);
}

/// <summary>
/// Interface for sending email
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a single email
    /// </summary>
    /// <param name="request">Email send request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<NotificationResponse> SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple emails in batch
    /// </summary>
    /// <param name="requests">List of email requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation results</returns>
    Task<BulkNotificationResponse> SendBulkEmailAsync(List<SendEmailRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email with attachments
    /// </summary>
    /// <param name="request">Email request</param>
    /// <param name="attachments">List of attachments</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<NotificationResponse> SendEmailWithAttachmentsAsync(SendEmailRequest request, List<EmailAttachment> attachments, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <returns>True if valid</returns>
    bool ValidateEmail(string email);
}

/// <summary>
/// Interface for sending push notifications
/// </summary>
public interface IPushService
{
    /// <summary>
    /// Sends a single push notification
    /// </summary>
    /// <param name="request">Push notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<NotificationResponse> SendPushNotificationAsync(SendPushNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple push notifications in batch
    /// </summary>
    /// <param name="requests">List of push requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation results</returns>
    Task<BulkNotificationResponse> SendBulkPushNotificationAsync(List<SendPushNotificationRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a push notification to all of a user's devices
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="title">Notification title</param>
    /// <param name="body">Notification body</param>
    /// <param name="data">Additional data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<NotificationResponse> SendToUserAsync(string userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a push notification to a topic
    /// </summary>
    /// <param name="topic">Topic name</param>
    /// <param name="title">Notification title</param>
    /// <param name="body">Notification body</param>
    /// <param name="data">Additional data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<NotificationResponse> SendToTopicAsync(string topic, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a device token for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="deviceToken">Device token</param>
    /// <param name="platform">Platform (iOS, Android)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if registered successfully</returns>
    Task<bool> RegisterDeviceTokenAsync(string userId, string deviceToken, string platform, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for in-app (real-time) notifications
/// </summary>
public interface IInAppNotificationService
{
    /// <summary>
    /// Sends an in-app notification to a specific user
    /// </summary>
    /// <param name="request">In-app notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<NotificationResponse> SendInAppNotificationAsync(SendInAppNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a broadcast notification to all connected users
    /// </summary>
    /// <param name="title">Notification title</param>
    /// <param name="message">Message</param>
    /// <param name="type">Notification type</param>
    /// <param name="data">Additional data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of users reached</returns>
    Task<int> SendBroadcastNotificationAsync(string title, string message, string type = "info", Dictionary<string, object>? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to a group of users
    /// </summary>
    /// <param name="userIds">List of user IDs</param>
    /// <param name="title">Notification title</param>
    /// <param name="message">Message</param>
    /// <param name="type">Notification type</param>
    /// <param name="data">Additional data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of users reached</returns>
    Task<int> SendToGroupAsync(List<string> userIds, string title, string message, string type = "info", Dictionary<string, object>? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets unread notifications for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="limit">Maximum number of notifications</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of unread notifications</returns>
    Task<List<Notification>> GetUnreadNotificationsAsync(string userId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a notification as read
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if marked successfully</returns>
    Task<bool> MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks all notifications as read for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of notifications marked</returns>
    Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for managing notification templates
/// </summary>
public interface INotificationTemplateService
{
    /// <summary>
    /// Gets a template by name
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template or null</returns>
    Task<NotificationTemplate?> GetTemplateAsync(string templateName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active templates for a type
    /// </summary>
    /// <param name="type">Notification type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of templates</returns>
    Task<List<NotificationTemplate>> GetTemplatesByTypeAsync(NotificationType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders a template with variables
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <param name="variables">Variables to substitute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rendered template</returns>
    Task<RenderedTemplate> RenderTemplateAsync(string templateName, Dictionary<string, string> variables, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new template
    /// </summary>
    /// <param name="template">Template to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created template</returns>
    Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing template
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <param name="template">Updated data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated template</returns>
    Task<NotificationTemplate?> UpdateTemplateAsync(string templateName, NotificationTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a template
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteTemplateAsync(string templateName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates template variables
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <param name="variables">Variables to validate</param>
    /// <returns>Validation result</returns>
    Task<ValidationResult> ValidateTemplateVariablesAsync(string templateName, Dictionary<string, string> variables);
}

/// <summary>
/// Main notification service interface
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification using a template
    /// </summary>
    /// <param name="request">Template notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<NotificationResponse> SendNotificationAsync(SendTemplateNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a direct notification (without a template)
    /// </summary>
    /// <param name="request">Direct notification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result</returns>
    Task<NotificationResponse> SendDirectNotificationAsync(SendNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple notifications
    /// </summary>
    /// <param name="requests">List of requests</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation results</returns>
    Task<BulkNotificationResponse> SendBulkNotificationsAsync(List<SendNotificationRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Schedules a notification for future delivery
    /// </summary>
    /// <param name="request">Notification request</param>
    /// <param name="scheduledAt">Scheduled date/time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Scheduled notification ID</returns>
    Task<int> ScheduleNotificationAsync(SendNotificationRequest request, DateTime scheduledAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a scheduled notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if canceled</returns>
    Task<bool> CancelScheduledNotificationAsync(int notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a notification's status
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Notification status</returns>
    Task<NotificationStatusResponse?> GetNotificationStatusAsync(int notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user's notifications with pagination
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="page">Page</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of notifications</returns>
    Task<NotificationListResponse> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets notification statistics
    /// </summary>
    /// <param name="userId">User ID (optional)</param>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Statistics</returns>
    Task<NotificationStatsResponse> GetNotificationStatsAsync(string? userId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries sending a failed notification
    /// </summary>
    /// <param name="notificationId">Notification ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the new attempt</returns>
    Task<NotificationResponse> RetryNotificationAsync(int notificationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Model for a rendered template
/// </summary>
public class RenderedTemplate
{
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
    public NotificationType Type { get; set; }
}

/// <summary>
/// Result of validating template variables
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> MissingVariables { get; set; } = new();
}

/// <summary>
/// Model for an email attachment
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public bool IsInline { get; set; } = false;
    public string? ContentId { get; set; }
}
