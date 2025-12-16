using NotificationService.Models;

namespace NotificationService.Services;

/// <summary>
/// Interfaccia per l'invio di SMS
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Invia SMS singolo
    /// </summary>
    /// <param name="request">Richiesta invio SMS</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'operazione</returns>
    Task<NotificationResponse> SendSmsAsync(SendSmsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invia SMS multipli in batch
    /// </summary>
    /// <param name="requests">Lista di richieste SMS</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultati delle operazioni</returns>
    Task<BulkNotificationResponse> SendBulkSmsAsync(List<SendSmsRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica lo stato di un SMS inviato
    /// </summary>
    /// <param name="externalId">ID esterno del messaggio</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Stato del messaggio</returns>
    Task<NotificationStatusResponse> GetSmsStatusAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida il numero di telefono
    /// </summary>
    /// <param name="phoneNumber">Numero di telefono</param>
    /// <returns>True se valido</returns>
    bool ValidatePhoneNumber(string phoneNumber);
}

/// <summary>
/// Interfaccia per l'invio di email
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Invia email singola
    /// </summary>
    /// <param name="request">Richiesta invio email</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'operazione</returns>
    Task<NotificationResponse> SendEmailAsync(SendEmailRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invia email multiple in batch
    /// </summary>
    /// <param name="requests">Lista di richieste email</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultati delle operazioni</returns>
    Task<BulkNotificationResponse> SendBulkEmailAsync(List<SendEmailRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invia email con allegati
    /// </summary>
    /// <param name="request">Richiesta email</param>
    /// <param name="attachments">Lista allegati</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'operazione</returns>
    Task<NotificationResponse> SendEmailWithAttachmentsAsync(SendEmailRequest request, List<EmailAttachment> attachments, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida indirizzo email
    /// </summary>
    /// <param name="email">Indirizzo email</param>
    /// <returns>True se valido</returns>
    bool ValidateEmail(string email);
}

/// <summary>
/// Interfaccia per l'invio di notifiche push
/// </summary>
public interface IPushService
{
    /// <summary>
    /// Invia notifica push singola
    /// </summary>
    /// <param name="request">Richiesta notifica push</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'operazione</returns>
    Task<NotificationResponse> SendPushNotificationAsync(SendPushNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invia notifiche push multiple in batch
    /// </summary>
    /// <param name="requests">Lista di richieste push</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultati delle operazioni</returns>
    Task<BulkNotificationResponse> SendBulkPushNotificationAsync(List<SendPushNotificationRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invia notifica push a tutti i dispositivi di un utente
    /// </summary>
    /// <param name="userId">ID utente</param>
    /// <param name="title">Titolo notifica</param>
    /// <param name="body">Corpo notifica</param>
    /// <param name="data">Dati aggiuntivi</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'operazione</returns>
    Task<NotificationResponse> SendToUserAsync(string userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invia notifica push a un topic
    /// </summary>
    /// <param name="topic">Nome del topic</param>
    /// <param name="title">Titolo notifica</param>
    /// <param name="body">Corpo notifica</param>
    /// <param name="data">Dati aggiuntivi</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'operazione</returns>
    Task<NotificationResponse> SendToTopicAsync(string topic, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra token dispositivo per un utente
    /// </summary>
    /// <param name="userId">ID utente</param>
    /// <param name="deviceToken">Token dispositivo</param>
    /// <param name="platform">Piattaforma (iOS, Android)</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>True se registrato con successo</returns>
    Task<bool> RegisterDeviceTokenAsync(string userId, string deviceToken, string platform, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interfaccia per le notifiche in-app (tempo reale)
/// </summary>
public interface IInAppNotificationService
{
    /// <summary>
    /// Invia notifica in-app a un utente specifico
    /// </summary>
    /// <param name="request">Richiesta notifica in-app</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'operazione</returns>
    Task<NotificationResponse> SendInAppNotificationAsync(SendInAppNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invia notifica broadcast a tutti gli utenti connessi
    /// </summary>
    /// <param name="title">Titolo notifica</param>
    /// <param name="message">Messaggio</param>
    /// <param name="type">Tipo notifica</param>
    /// <param name="data">Dati aggiuntivi</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Numero di utenti raggiunti</returns>
    Task<int> SendBroadcastNotificationAsync(string title, string message, string type = "info", Dictionary<string, object>? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invia notifica a un gruppo di utenti
    /// </summary>
    /// <param name="userIds">Lista ID utenti</param>
    /// <param name="title">Titolo notifica</param>
    /// <param name="message">Messaggio</param>
    /// <param name="type">Tipo notifica</param>
    /// <param name="data">Dati aggiuntivi</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Numero di utenti raggiunti</returns>
    Task<int> SendToGroupAsync(List<string> userIds, string title, string message, string type = "info", Dictionary<string, object>? data = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottieni notifiche non lette per un utente
    /// </summary>
    /// <param name="userId">ID utente</param>
    /// <param name="limit">Numero massimo notifiche</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Lista notifiche non lette</returns>
    Task<List<Notification>> GetUnreadNotificationsAsync(string userId, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca notifica come letta
    /// </summary>
    /// <param name="notificationId">ID notifica</param>
    /// <param name="userId">ID utente</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>True se marcata con successo</returns>
    Task<bool> MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca tutte le notifiche come lette per un utente
    /// </summary>
    /// <param name="userId">ID utente</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Numero di notifiche marcate</returns>
    Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interfaccia per la gestione dei template di notifica
/// </summary>
public interface INotificationTemplateService
{
    /// <summary>
    /// Ottieni template per nome
    /// </summary>
    /// <param name="templateName">Nome template</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Template o null</returns>
    Task<NotificationTemplate?> GetTemplateAsync(string templateName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottieni tutti i template attivi per tipo
    /// </summary>
    /// <param name="type">Tipo notifica</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Lista template</returns>
    Task<List<NotificationTemplate>> GetTemplatesByTypeAsync(NotificationType type, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renderizza template con variabili
    /// </summary>
    /// <param name="templateName">Nome template</param>
    /// <param name="variables">Variabili da sostituire</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Template renderizzato</returns>
    Task<RenderedTemplate> RenderTemplateAsync(string templateName, Dictionary<string, string> variables, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea nuovo template
    /// </summary>
    /// <param name="template">Template da creare</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Template creato</returns>
    Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggiorna template esistente
    /// </summary>
    /// <param name="templateName">Nome template</param>
    /// <param name="template">Dati aggiornati</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Template aggiornato</returns>
    Task<NotificationTemplate?> UpdateTemplateAsync(string templateName, NotificationTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina template
    /// </summary>
    /// <param name="templateName">Nome template</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>True se eliminato con successo</returns>
    Task<bool> DeleteTemplateAsync(string templateName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida variabili template
    /// </summary>
    /// <param name="templateName">Nome template</param>
    /// <param name="variables">Variabili da validare</param>
    /// <returns>Risultato validazione</returns>
    Task<ValidationResult> ValidateTemplateVariablesAsync(string templateName, Dictionary<string, string> variables);
}

/// <summary>
/// Interfaccia principale del servizio notifiche
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Invia notifica usando il template
    /// </summary>
    /// <param name="request">Richiesta notifica con template</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'operazione</returns>
    Task<NotificationResponse> SendNotificationAsync(SendTemplateNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invia notifica diretta (senza template)
    /// </summary>
    /// <param name="request">Richiesta notifica diretta</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato dell'operazione</returns>
    Task<NotificationResponse> SendDirectNotificationAsync(SendNotificationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invia notifiche multiple
    /// </summary>
    /// <param name="requests">Lista richieste</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultati operazioni</returns>
    Task<BulkNotificationResponse> SendBulkNotificationsAsync(List<SendNotificationRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Programma notifica per invio futuro
    /// </summary>
    /// <param name="request">Richiesta notifica</param>
    /// <param name="scheduledAt">Data/ora programmata</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>ID notifica programmata</returns>
    Task<int> ScheduleNotificationAsync(SendNotificationRequest request, DateTime scheduledAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Annulla notifica programmata
    /// </summary>
    /// <param name="notificationId">ID notifica</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>True se annullata</returns>
    Task<bool> CancelScheduledNotificationAsync(int notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottieni stato notifica
    /// </summary>
    /// <param name="notificationId">ID notifica</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Stato notifica</returns>
    Task<NotificationStatusResponse?> GetNotificationStatusAsync(int notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottieni notifiche utente con paginazione
    /// </summary>
    /// <param name="userId">ID utente</param>
    /// <param name="page">Pagina</param>
    /// <param name="pageSize">Dimensione pagina</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Lista paginata notifiche</returns>
    Task<NotificationListResponse> GetUserNotificationsAsync(string userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottieni statistiche notifiche
    /// </summary>
    /// <param name="userId">ID utente (opzionale)</param>
    /// <param name="fromDate">Data inizio</param>
    /// <param name="toDate">Data fine</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Statistiche</returns>
    Task<NotificationStatsResponse> GetNotificationStatsAsync(string? userId = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ritenta invio notifica fallita
    /// </summary>
    /// <param name="notificationId">ID notifica</param>
    /// <param name="cancellationToken">Token di cancellazione</param>
    /// <returns>Risultato nuovo tentativo</returns>
    Task<NotificationResponse> RetryNotificationAsync(int notificationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Modello per template renderizzato
/// </summary>
public class RenderedTemplate
{
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? HtmlContent { get; set; }
}

/// <summary>
/// Risultato validazione variabili template
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> MissingVariables { get; set; } = new();
}

/// <summary>
/// Modello per allegato email
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public bool IsInline { get; set; } = false;
    public string? ContentId { get; set; }
}