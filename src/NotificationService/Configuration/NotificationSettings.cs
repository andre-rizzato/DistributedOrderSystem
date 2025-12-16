namespace NotificationService.Configuration;

/// <summary>
/// Configurazione per provider SMS (Twilio)
/// </summary>
public class SmsSettings
{
    /// <summary>
    /// Provider SMS da utilizzare (twilio, nexmo, etc.)
    /// </summary>
    public string Provider { get; set; } = \"twilio\";
    
    /// <summary>
    /// Account SID di Twilio
    /// </summary>
    public required string AccountSid { get; set; }
    
    /// <summary>
    /// Auth Token di Twilio
    /// </summary>
    public required string AuthToken { get; set; }
    
    /// <summary>
    /// Numero di telefono mittente
    /// </summary>
    public required string FromNumber { get; set; }
    
    /// <summary>
    /// Webhook per status callback
    /// </summary>
    public string? WebhookUrl { get; set; }
    
    /// <summary>
    /// Timeout per invio SMS
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Numero massimo di tentativi
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Configurazione per provider Email (SMTP)
/// </summary>
public class EmailSettings
{
    /// <summary>
    /// Server SMTP
    /// </summary>
    public required string SmtpHost { get; set; }
    
    /// <summary>
    /// Porta SMTP
    /// </summary>
    public int SmtpPort { get; set; } = 587;
    
    /// <summary>
    /// Usa SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;
    
    /// <summary>
    /// Username SMTP
    /// </summary>
    public required string Username { get; set; }
    
    /// <summary>
    /// Password SMTP
    /// </summary>
    public required string Password { get; set; }
    
    /// <summary>
    /// Email mittente di default
    /// </summary>
    public required string DefaultFromEmail { get; set; }
    
    /// <summary>
    /// Nome mittente di default
    /// </summary>
    public string? DefaultFromName { get; set; }
    
    /// <summary>
    /// Email per risposte
    /// </summary>
    public string? ReplyToEmail { get; set; }
    
    /// <summary>
    /// Timeout invio email
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);
    
    /// <summary>
    /// Numero massimo di tentativi
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Configurazione per notifiche Push (Firebase)
/// </summary>
public class PushNotificationSettings
{
    /// <summary>
    /// Provider push (firebase, apns, etc.)
    /// </summary>
    public string Provider { get; set; } = \"firebase\";
    
    /// <summary>
    /// Percorso file credenziali Firebase
    /// </summary>
    public required string FirebaseCredentialsPath { get; set; }
    
    /// <summary>
    /// Project ID Firebase
    /// </summary>
    public required string FirebaseProjectId { get; set; }
    
    /// <summary>
    /// URL per deep linking
    /// </summary>
    public string? DeepLinkUrl { get; set; }
    
    /// <summary>
    /// Timeout per push notifications
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Numero massimo di tentativi
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Configurazione generale del servizio notifiche
/// </summary>
public class NotificationSettings
{
    /// <summary>
    /// Abilita il servizio di notifiche
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Modalità debug (stampa log dettagliati)
    /// </summary>
    public bool DebugMode { get; set; } = false;
    
    /// <summary>
    /// Numero massimo di notifiche da processare per batch
    /// </summary>
    public int BatchSize { get; set; } = 100;
    
    /// <summary>
    /// Intervallo di processamento batch (in secondi)
    /// </summary>
    public int BatchIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// Numero massimo di tentativi globale
    /// </summary>
    public int MaxGlobalRetries { get; set; } = 5;
    
    /// <summary>
    /// Tempo di attesa tra tentativi (backoff)
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Fattore moltiplicativo per backoff esponenziale
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;
    
    /// <summary>
    /// Conserva notifiche per N giorni
    /// </summary>
    public int RetentionDays { get; set; } = 90;
    
    /// <summary>
    /// Abilita cleanup automatico notifiche vecchie
    /// </summary>
    public bool AutoCleanup { get; set; } = true;
    
    /// <summary>
    /// Ora del cleanup automatico (formato HH:mm)
    /// </summary>
    public string CleanupTime { get; set; } = \"02:00\";
}

/// <summary>
/// Configurazione per cache Redis
/// </summary>
public class RedisSettings
{
    /// <summary>
    /// Connection string Redis
    /// </summary>
    public required string ConnectionString { get; set; }
    
    /// <summary>
    /// Prefisso per le chiavi
    /// </summary>
    public string KeyPrefix { get; set; } = \"notifications:\";
    
    /// <summary>
    /// TTL di default per cache (in minuti)
    /// </summary>
    public int DefaultTtlMinutes { get; set; } = 60;
    
    /// <summary>
    /// Database Redis da utilizzare
    /// </summary>
    public int Database { get; set; } = 0;
}

/// <summary>
/// Configurazione per template di notifiche
/// </summary>
public class TemplateSettings
{
    /// <summary>
    /// Percorso cartella template
    /// </summary>
    public string TemplatePath { get; set; } = \"Templates\";
    
    /// <summary>
    /// Linguaggio di default per template
    /// </summary>
    public string DefaultLanguage { get; set; } = \"it\";
    
    /// <summary>
    /// Abilita cache dei template
    /// </summary>
    public bool EnableCache { get; set; } = true;
    
    /// <summary>
    /// Durata cache template (in minuti)
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 30;
}

/// <summary>
/// Configurazione per webhook e callback
/// </summary>
public class WebhookSettings
{
    /// <summary>
    /// URL base per webhook di status
    /// </summary>
    public string? BaseUrl { get; set; }
    
    /// <summary>
    /// Token segreto per validare webhook
    /// </summary>
    public string? SecretToken { get; set; }
    
    /// <summary>
    /// Timeout per chiamate webhook
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Numero massimo di tentativi per webhook
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Configurazione per rate limiting
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Abilita rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Numero massimo di notifiche per minuto per tipo
    /// </summary>
    public Dictionary<NotificationType, int> LimitsPerMinute { get; set; } = new()
    {
        { NotificationType.Email, 100 },
        { NotificationType.SMS, 50 },
        { NotificationType.Push, 200 },
        { NotificationType.InApp, 500 }
    };
    
    /// <summary>
    /// Numero massimo di notifiche per utente per ora
    /// </summary>
    public int PerUserPerHourLimit { get; set; } = 50;
    
    /// <summary>
    /// Finestra temporale per rate limiting (in minuti)
    /// </summary>
    public int WindowMinutes { get; set; } = 1;
}