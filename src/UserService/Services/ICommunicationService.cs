namespace UserService.Services;

/// <summary>
/// Cliente per chiamare il CommunicationService per inviare email
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
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CommunicationService> _logger;
    private readonly string _communicationServiceUrl;

    public CommunicationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CommunicationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _communicationServiceUrl = _configuration.GetValue<string>("Services:CommunicationService:BaseUrl") 
            ?? "https://localhost:5011";
    }

    public async Task SendEmailVerificationAsync(string email, string verificationUrl)
    {
        try
        {
            var payload = new
            {
                To = email,
                Subject = "Verifica il tuo indirizzo email",
                TemplateName = "EmailVerification",
                TemplateData = new { VerificationUrl = verificationUrl }
            };

            var response = await _httpClient.PostAsJsonAsync($"{_communicationServiceUrl}/api/emails/send", payload);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Errore invio email di verifica a {Email}: {StatusCode}", email, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio dell'email di verifica a {Email}", email);
        }
    }

    public async Task SendPasswordResetAsync(string email, string resetUrl)
    {
        try
        {
            var payload = new
            {
                To = email,
                Subject = "Reimposta la tua password",
                TemplateName = "PasswordReset",
                TemplateData = new { ResetUrl = resetUrl }
            };

            var response = await _httpClient.PostAsJsonAsync($"{_communicationServiceUrl}/api/emails/send", payload);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Errore invio email reset password a {Email}: {StatusCode}", email, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio dell'email di reset password a {Email}", email);
        }
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName)
    {
        try
        {
            var payload = new
            {
                To = email,
                Subject = "Benvenuto!",
                TemplateName = "Welcome",
                TemplateData = new { FirstName = firstName }
            };

            await _httpClient.PostAsJsonAsync($"{_communicationServiceUrl}/api/emails/send", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio dell'email di benvenuto a {Email}", email);
        }
    }

    public async Task SendPasswordChangedNotificationAsync(string email)
    {
        try
        {
            var payload = new
            {
                To = email,
                Subject = "Password modificata",
                TemplateName = "PasswordChanged",
                TemplateData = new { }
            };

            await _httpClient.PostAsJsonAsync($"{_communicationServiceUrl}/api/emails/send", payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio della notifica di cambio password a {Email}", email);
        }
    }
}
