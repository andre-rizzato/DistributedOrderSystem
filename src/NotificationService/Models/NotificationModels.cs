namespace NotificationService.Models;

/// <summary>
/// Tipi di notifica supportati dal sistema
/// Ogni tipo corrisponde a un canale di comunicazione specifico
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
/// Priorità delle notifiche per determinare l'ordine di elaborazione
/// Le notifiche ad alta priorità vengono elaborate per prime
/// </summary>
public enum NotificationPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Stati delle notifiche
/// </summary>
public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3,
    Delivered = 4,
    Read = 5
}

/// <summary>
/// Entità principale per le notifiche - rappresenta una singola notifica
/// nel sistema con tutti i metadati necessari per il tracking e l'audit
/// </summary>
public class Notification
{
    public long Id { get; set; }
    
    /// <summary>
    /// Tipo di notifica (SMS, Email, Push, etc.)
    /// </summary>
    public NotificationType Type { get; set; }
    
    /// <summary>
    /// Destinatario della notifica
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
    /// Contenuto HTML (per email)
    /// </summary>
    public string? HtmlContent { get; set; }
    
    /// <summary>
    /// Priorità della notifica
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    
    /// <summary>
    /// Stato attuale della notifica
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    
    /// <summary>
    /// ID dell'utente che ha generato la notifica
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Canale o servizio di origine
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// Riferimenti per tracking (order ID, product ID, etc.)
    /// </summary>
    public string? ReferenceId { get; set; }
    
    /// <summary>
    /// Tipo di riferimento (order, product, user, etc.)
    /// </summary>
    public string? ReferenceType { get; set; }
    
    /// <summary>
    /// Metadati aggiuntivi in formato JSON
    /// </summary>
    public string? Metadata { get; set; }
    
    /// <summary>
    /// Quando inviare la notifica (per notifiche schedulate)
    /// </summary>
    public DateTime? ScheduledAt { get; set; }
    
    /// <summary>
    /// Quando la notifica è stata inviata
    /// </summary>
    public DateTime? SentAt { get; set; }
    
    /// <summary>
    /// Quando la notifica è stata consegnata/letta
    /// </summary>
    public DateTime? DeliveredAt { get; set; }
    
    /// <summary>
    /// Data di creazione
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Numero di tentativi di invio
    /// </summary>
    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Ultimo messaggio di errore
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// ID esterno del provider (Twilio SID, etc.)
    /// </summary>
    public string? ExternalId { get; set; }
}

/// <summary>
/// Template per notifiche ricorrenti
/// </summary>
public class NotificationTemplate
{
    public long Id { get; set; }
    
    /// <summary>
    /// Nome univoco del template
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Descrizione del template
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Tipo di notifica
    /// </summary>
    public NotificationType Type { get; set; }
    
    /// <summary>
    /// Template del soggetto con placeholder
    /// </summary>
    public required string SubjectTemplate { get; set; }
    
    /// <summary>
    /// Template del contenuto con placeholder
    /// </summary>
    public required string ContentTemplate { get; set; }
    
    /// <summary>
    /// Template HTML per email
    /// </summary>
    public string? HtmlTemplate { get; set; }
    
    /// <summary>
    /// Variabili disponibili nel template
    /// </summary>
    public string? Variables { get; set; }
    
    /// <summary>
    /// Template attivo
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Data di creazione
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Ultima modifica
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Preferenze di notifica per utente
/// </summary>
public class NotificationPreference
{
    public long Id { get; set; }
    
    /// <summary>
    /// ID dell'utente
    /// </summary>
    public required string UserId { get; set; }
    
    /// <summary>
    /// Tipo di notifica
    /// </summary>
    public NotificationType Type { get; set; }
    
    /// <summary>
    /// Categoria di notifica (ordini, marketing, sistema, etc.)
    /// </summary>
    public required string Category { get; set; }
    
    /// <summary>
    /// Se l'utente ha abilitato questo tipo di notifica
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Canale preferito per questa categoria
    /// </summary>
    public string? PreferredChannel { get; set; }
    
    /// <summary>
    /// Data di creazione
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Ultima modifica
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Log di invio notifiche per audit
/// </summary>
public class NotificationLog
{
    public long Id { get; set; }
    
    /// <summary>
    /// Riferimento alla notifica
    /// </summary>
    public long NotificationId { get; set; }
    
    /// <summary>
    /// Navigazione alla notifica
    /// </summary>
    public Notification? Notification { get; set; }
    
    /// <summary>
    /// Azione eseguita (sent, failed, retry, etc.)
    /// </summary>
    public required string Action { get; set; }
    
    /// <summary>
    /// Dettagli dell'azione
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// Provider utilizzato (twilio, smtp, firebase, etc.)
    /// </summary>
    public string? Provider { get; set; }
    
    /// <summary>
    /// Timestamp dell'azione
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}