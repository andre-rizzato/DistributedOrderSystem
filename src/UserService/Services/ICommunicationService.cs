namespace UserService.Services;

/// <summary>
/// Client for calling CommunicationService to send emails
/// </summary>
public interface ICommunicationService
{
    Task SendEmailVerificationAsync(string email, string verificationUrl);
    Task SendPasswordResetAsync(string email, string resetUrl);
    Task SendWelcomeEmailAsync(string email, string firstName);
    Task SendPasswordChangedNotificationAsync(string email);
}

public class CommunicationService : ICommunicationService
{
    // NotificationType.Email in NotificationService (src/NotificationService/Models/NotificationModels.cs) -
    // that service isn't referenced as a project here, so the numeric value is duplicated rather than
    // shared. NotificationService has no JsonStringEnumConverter registered, so this must be sent as a number.
    private const int NotificationTypeEmail = 1;

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CommunicationService> _logger;
    private readonly string _notificationServiceUrl;

    public CommunicationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CommunicationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        // Despite the "CommunicationService" name (kept for the interface UserService talks to),
        // there is no standalone CommunicationService in this repo - email actually goes out through
        // NotificationService (Hangfire + MailKit), so this must point there.
        _notificationServiceUrl = _configuration.GetValue<string>("Services:CommunicationService:BaseUrl")
            ?? "http://localhost:5246";
    }

    public Task SendEmailVerificationAsync(string email, string verificationUrl) => SendDirectAsync(
        email,
        "Verify your email address",
        $"Please verify your email by visiting the following link: {verificationUrl}");

    public Task SendPasswordResetAsync(string email, string resetUrl) => SendDirectAsync(
        email,
        "Reset your password",
        $"You requested a password reset. Visit the following link to choose a new password: {resetUrl}");

    public Task SendWelcomeEmailAsync(string email, string firstName) => SendDirectAsync(
        email,
        "Welcome!",
        $"Hi {firstName}, welcome! Your account is ready to use.");

    public Task SendPasswordChangedNotificationAsync(string email) => SendDirectAsync(
        email,
        "Password changed",
        "Your password was just changed. If this wasn't you, please contact support immediately.");

    private async Task SendDirectAsync(string email, string subject, string content)
    {
        try
        {
            // NotificationService/Controllers/NotificationController.cs -> [Route("api/[controller]")],
            // POST "send-direct" takes a SendNotificationRequest with no pre-registered template needed
            // (unlike "send-template", whose seeded templates don't cover auth emails).
            var payload = new
            {
                Type = NotificationTypeEmail,
                Recipient = email,
                Subject = subject,
                Content = content,
                Source = "UserService"
            };

            var response = await _httpClient.PostAsJsonAsync($"{_notificationServiceUrl}/api/notification/send-direct", payload);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error sending email to {Email}: {StatusCode}", email, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", email);
        }
    }
}
