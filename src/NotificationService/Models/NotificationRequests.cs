namespace NotificationService.Models.Requests;

/// <summary>
/// Richiesta per inviare una notifica generica
/// </summary>
public class SendNotificationRequest
{
    /// <summary>
    /// Tipo di notifica da inviare
    /// </summary>
    public NotificationType Type { get; set; }
    
    /// <summary>
    /// Destinatario (email, numero telefono, user ID, etc.)
    /// </summary>
    public required string Recipient { get; set; }
    
    /// <summary>
    /// Oggetto/Titolo della notifica
    /// </summary>
    public required string Subject { get; set; }
    
    /// <summary>
    /// Contenuto della notifica
    /// </summary>
    public required string Content { get; set; }
    
    /// <summary>
    /// Contenuto HTML (opzionale, per email)
    /// </summary>
    public string? HtmlContent { get; set; }
    
    /// <summary>
    /// Priorità della notifica
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    
    /// <summary>
    /// Quando inviare la notifica (null = subito)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }
    
    /// <summary>
    /// Metadati aggiuntivi
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
    
    /// <summary>
    /// ID di riferimento per tracking
    /// </summary>
    public string? ReferenceId { get; set; }
    
    /// <summary>
    /// Tipo di riferimento
    /// </summary>
    public string? ReferenceType { get; set; }
    
    /// <summary>
    /// Servizio che ha generato la notifica
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// ID dell'utente destinatario
    /// </summary>
    public string? UserId { get; set; }
}

/// <summary>
/// Richiesta specifica per invio email
/// </summary>
public class SendEmailRequest
{
    /// <summary>
    /// Destinatario email
    /// </summary>
    public required string To { get; set; }
    
    /// <summary>
    /// Destinatari in copia
    /// </summary>
    public List<string>? Cc { get; set; }
    
    /// <summary>
    /// Destinatari in copia nascosta
    /// </summary>
    public List<string>? Bcc { get; set; }
    
    /// <summary>
    /// Oggetto email
    /// </summary>
    public required string Subject { get; set; }
    
    /// <summary>
    /// Contenuto testuale
    /// </summary>
    public string? TextContent { get; set; }
    
    /// <summary>
    /// Contenuto HTML
    /// </summary>
    public string? HtmlContent { get; set; }
    
    /// <summary>
    /// Contenuto testuale semplice (alias per compatibilità)
    /// </summary>
    public string? Content
    {
        get => TextContent;
        set => TextContent = value;
    }
    
    /// <summary>
    /// Allegati
    /// </summary>
    public List<EmailAttachment>? Attachments { get; set; }
    
    /// <summary>
    /// Template da utilizzare
    /// </summary>
    public string? TemplateName { get; set; }
    
    /// <summary>
    /// Variabili per il template
    /// </summary>
    public Dictionary<string, string>? TemplateVariables { get; set; }
    
    /// <summary>
    /// Priorità email
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}

/// <summary>
/// Allegato email
/// </summary>
public class EmailAttachment
{
    /// <summary>
    /// Nome file
    /// </summary>
    public required string FileName { get; set; }
    
    /// <summary>
    /// Tipo MIME
    /// </summary>
    public required string ContentType { get; set; }
    
    /// <summary>
    /// Contenuto file in base64
    /// </summary>
    public required string Content { get; set; }
}

/// <summary>
/// Richiesta per invio SMS
/// </summary>
public class SendSmsRequest
{
    /// <summary>
    /// Numero di telefono destinatario (formato internazionale)
    /// </summary>
    public required string PhoneNumber { get; set; }
    
    /// <summary>
    /// Messaggio SMS (max 160 caratteri per SMS singolo)
    /// </summary>
    public required string Message { get; set; }
    
    /// <summary>
    /// Template da utilizzare
    /// </summary>
    public string? TemplateName { get; set; }
    
    /// <summary>
    /// Variabili per il template
    /// </summary>
    public Dictionary<string, string>? TemplateVariables { get; set; }
    
    /// <summary>
    /// Priorità SMS
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    
    /// <summary>
    /// Quando inviare l'SMS
    /// </summary>
    public DateTime? ScheduledAt { get; set; }
}

/// <summary>
/// Richiesta per notifiche push
/// </summary>
public class SendPushNotificationRequest
{
    /// <summary>
    /// Device token o topic
    /// </summary>
    public required string Target { get; set; }
    
    /// <summary>
    /// Device token (alias per Target)
    /// </summary>
    public string DeviceToken
    {
        get => Target;
        set => Target = value;
    }
    
    /// <summary>
    /// Titolo notifica
    /// </summary>
    public required string Title { get; set; }
    
    /// <summary>
    /// Corpo della notifica
    /// </summary>
    public required string Body { get; set; }
    
    /// <summary>
    /// Icona notifica
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Immagine notifica
    /// </summary>
    public string? Image { get; set; }
    
    /// <summary>
    /// URL immagine (alias per Image)
    /// </summary>
    public string? ImageUrl
    {
        get => Image;
        set => Image = value;
    }
    
    /// <summary>
    /// Action quando si clicca la notifica
    /// </summary>
    public string? ClickAction { get; set; }
    
    /// <summary>
    /// Dati aggiuntivi
    /// </summary>
    public Dictionary<string, string>? Data { get; set; }
    
    /// <summary>
    /// Badge count (iOS)
    /// </summary>
    public int? Badge { get; set; }
    
    /// <summary>
    /// Suono notifica
    /// </summary>
    public string? Sound { get; set; } = "default";
}

/// <summary>
/// Richiesta per notifiche in-app
/// </summary>
public class SendInAppNotificationRequest
{
    /// <summary>
    /// ID utente destinatario
    /// </summary>
    public required string UserId { get; set; }
    
    /// <summary>
    /// Titolo notifica
    /// </summary>
    public required string Title { get; set; }
    
    /// <summary>
    /// Messaggio notifica
    /// </summary>
    public required string Message { get; set; }
    
    /// <summary>
    /// Tipo/categoria della notifica
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// Tipo di notifica (alias per Category)
    /// </summary>
    public string? NotificationType
    {
        get => Category;
        set => Category = value;
    }
    
    /// <summary>
    /// URL o action associata
    /// </summary>
    public string? ActionUrl { get; set; }
    
    /// <summary>
    /// Icona notifica
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Dati aggiuntivi
    /// </summary>
    public Dictionary<string, string>? Data { get; set; }
    
    /// <summary>
    /// Priorità notifica
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    
    /// <summary>
    /// Servizio che ha generato la notifica
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// ID di riferimento per tracking
    /// </summary>
    public string? ReferenceId { get; set; }
    
    /// <summary>
    /// Tipo di riferimento
    /// </summary>
    public string? ReferenceType { get; set; }
}

/// <summary>
/// Richiesta per invio notifica tramite template
/// </summary>
public class SendTemplateNotificationRequest
{
    /// <summary>
    /// Nome del template
    /// </summary>
    public required string TemplateName { get; set; }
    
    /// <summary>
    /// Destinatario
    /// </summary>
    public required string Recipient { get; set; }
    
    /// <summary>
    /// Variabili per sostituire i placeholder nel template
    /// </summary>
    public required Dictionary<string, string> Variables { get; set; }
    
    /// <summary>
    /// Tipo di notifica (se non specificato nel template)
    /// </summary>
    public NotificationType? Type { get; set; }
    
    /// <summary>
    /// Priorità notifica
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    
    /// <summary>
    /// ID utente (opzionale)
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Servizio che ha generato la notifica
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// ID di riferimento per tracking
    /// </summary>
    public string? ReferenceId { get; set; }
    
    /// <summary>
    /// Tipo di riferimento
    /// </summary>
    public string? ReferenceType { get; set; }
    
    /// <summary>
    /// Metadati aggiuntivi
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }
    
    /// <summary>
    /// Quando inviare la notifica
    /// </summary>
    public DateTime? ScheduledAt { get; set; }
}

/// <summary>
/// Richiesta per notifiche multiple (bulk)
/// </summary>
public class SendBulkNotificationRequest
{
    /// <summary>
    /// Lista di destinatari
    /// </summary>
    public required List<string> Recipients { get; set; }
    
    /// <summary>
    /// Tipo di notifica
    /// </summary>
    public NotificationType Type { get; set; }
    
    /// <summary>
    /// Oggetto/Titolo
    /// </summary>
    public required string Subject { get; set; }
    
    /// <summary>
    /// Contenuto
    /// </summary>
    public required string Content { get; set; }
    
    /// <summary>
    /// Template da utilizzare (opzionale)
    /// </summary>
    public string? TemplateName { get; set; }
    
    /// <summary>
    /// Variabili comuni per tutti i destinatari
    /// </summary>
    public Dictionary<string, string>? CommonVariables { get; set; }
    
    /// <summary>
    /// Variabili specifiche per destinatario
    /// </summary>
    public Dictionary<string, Dictionary<string, string>>? RecipientVariables { get; set; }
    
    /// <summary>
    /// Priorità notifiche
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}