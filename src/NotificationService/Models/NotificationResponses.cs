namespace NotificationService.Models.Responses;

/// <summary>
/// Risposta base per le operazioni di notifica
/// </summary>
public class NotificationResponse
{
    /// <summary>
    /// Successo dell'operazione
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// ID della notifica creata
    /// </summary>
    public long? NotificationId { get; set; }
    
    /// <summary>
    /// Messaggio di conferma o errore
    /// </summary>
    public string? Message { get; set; }
    
    /// <summary>
    /// ID esterno dal provider (Twilio SID, etc.)
    /// </summary>
    public string? ExternalId { get; set; }
    
    /// <summary>
    /// Dettagli aggiuntivi
    /// </summary>
    public Dictionary<string, string>? Details { get; set; }
    
    /// <summary>
    /// Timestamp dell'operazione
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Risposta per invio notifiche multiple
/// </summary>
public class BulkNotificationResponse
{
    /// <summary>
    /// Numero totale di notifiche processate
    /// </summary>
    public int TotalProcessed { get; set; }
    
    /// <summary>
    /// Numero di notifiche inviate con successo
    /// </summary>
    public int SuccessCount { get; set; }
    
    /// <summary>
    /// Numero di notifiche fallite
    /// </summary>
    public int FailureCount { get; set; }
    
    /// <summary>
    /// Lista di risultati per singola notifica
    /// </summary>
    public List<NotificationResponse> Results { get; set; } = new();
    
    /// <summary>
    /// Tempo totale di processamento
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Stato di una notifica
/// </summary>
public class NotificationStatusResponse
{
    /// <summary>
    /// ID della notifica
    /// </summary>
    public long NotificationId { get; set; }
    
    /// <summary>
    /// Stato attuale
    /// </summary>
    public NotificationStatus Status { get; set; }
    
    /// <summary>
    /// Tipo di notifica
    /// </summary>
    public NotificationType Type { get; set; }
    
    /// <summary>
    /// Destinatario
    /// </summary>
    public required string Recipient { get; set; }
    
    /// <summary>
    /// Oggetto/Titolo
    /// </summary>
    public required string Subject { get; set; }
    
    /// <summary>
    /// Data di creazione
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Data di invio
    /// </summary>
    public DateTime? SentAt { get; set; }
    
    /// <summary>
    /// Data di consegna/lettura
    /// </summary>
    public DateTime? DeliveredAt { get; set; }
    
    /// <summary>
    /// Numero tentativi
    /// </summary>
    public int RetryCount { get; set; }
    
    /// <summary>
    /// Ultimo messaggio di errore
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// ID esterno provider
    /// </summary>
    public string? ExternalId { get; set; }
}

/// <summary>
/// Risposta con lista di notifiche
/// </summary>
public class NotificationListResponse
{
    /// <summary>
    /// Lista di notifiche
    /// </summary>
    public List<NotificationStatusResponse> Notifications { get; set; } = new();
    
    /// <summary>
    /// Numero totale di elementi
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Pagina corrente
    /// </summary>
    public int Page { get; set; }
    
    /// <summary>
    /// Elementi per pagina
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Numero totale di pagine
    /// </summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// Risposta con statistiche notifiche
/// </summary>
public class NotificationStatsResponse
{
    /// <summary>
    /// Statistiche per tipo di notifica
    /// </summary>
    public Dictionary<NotificationType, TypeStats> TypeStats { get; set; } = new();
    
    /// <summary>
    /// Statistiche per stato
    /// </summary>
    public Dictionary<NotificationStatus, int> StatusStats { get; set; } = new();
    
    /// <summary>
    /// Totale notifiche nel periodo
    /// </summary>
    public int TotalNotifications { get; set; }
    
    /// <summary>
    /// Tasso di successo generale
    /// </summary>
    public double SuccessRate { get; set; }
    
    /// <summary>
    /// Periodo delle statistiche
    /// </summary>
    public DateRange Period { get; set; } = new();
    
    /// <summary>
    /// Tempo medio di consegna
    /// </summary>
    public TimeSpan? AverageDeliveryTime { get; set; }
}

/// <summary>
/// Statistiche per tipo di notifica
/// </summary>
public class TypeStats
{
    /// <summary>
    /// Numero totale per questo tipo
    /// </summary>
    public int Total { get; set; }
    
    /// <summary>
    /// Numero di successi
    /// </summary>
    public int Success { get; set; }
    
    /// <summary>
    /// Numero di fallimenti
    /// </summary>
    public int Failed { get; set; }
    
    /// <summary>
    /// Tasso di successo
    /// </summary>
    public double SuccessRate { get; set; }
    
    /// <summary>
    /// Tempo medio di invio
    /// </summary>
    public TimeSpan? AverageTime { get; set; }
}

/// <summary>
/// Range di date per statistiche
/// </summary>
public class DateRange
{
    /// <summary>
    /// Data di inizio
    /// </summary>
    public DateTime From { get; set; }
    
    /// <summary>
    /// Data di fine
    /// </summary>
    public DateTime To { get; set; }
}

/// <summary>
/// Risposta per verifica health del servizio
/// </summary>
public class NotificationHealthResponse
{
    /// <summary>
    /// Stato generale del servizio
    /// </summary>
    public bool IsHealthy { get; set; }
    
    /// <summary>
    /// Versione del servizio
    /// </summary>
    public required string Version { get; set; }
    
    /// <summary>
    /// Timestamp check
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Stato dei provider esterni
    /// </summary>
    public Dictionary<string, ProviderHealth> Providers { get; set; } = new();
    
    /// <summary>
    /// Stato database
    /// </summary>
    public bool DatabaseHealthy { get; set; }
    
    /// <summary>
    /// Stato cache Redis
    /// </summary>
    public bool CacheHealthy { get; set; }
    
    /// <summary>
    /// Notifiche in coda
    /// </summary>
    public int QueuedNotifications { get; set; }
    
    /// <summary>
    /// Jobs in elaborazione
    /// </summary>
    public int ProcessingJobs { get; set; }
}

/// <summary>
/// Stato di un provider di notifiche
/// </summary>
public class ProviderHealth
{
    /// <summary>
    /// Nome del provider
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Provider operativo
    /// </summary>
    public bool IsHealthy { get; set; }
    
    /// <summary>
    /// Ultimo test eseguito
    /// </summary>
    public DateTime LastCheck { get; set; }
    
    /// <summary>
    /// Messaggi di errore
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Tempo di risposta
    /// </summary>
    public TimeSpan? ResponseTime { get; set; }
}