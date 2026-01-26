using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data;

/// <summary>
/// Contesto database per il servizio notifiche
/// </summary>
public class NotificationContext : DbContext
{
    public NotificationContext(DbContextOptions<NotificationContext> options) : base(options)
    {
    }

    /// <summary>
    /// Notifiche
    /// </summary>
    public DbSet<Notification> Notifications { get; set; }
    
    /// <summary>
    /// Template di notifiche
    /// </summary>
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }
    
    /// <summary>
    /// Preferenze utenti per notifiche
    /// </summary>
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }
    
    /// <summary>
    /// Log delle operazioni di notifica
    /// </summary>
    public DbSet<NotificationLog> NotificationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurazione tabella Notifications
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Recipient).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.HtmlContent);
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.Source).HasMaxLength(100);
            entity.Property(e => e.ReferenceId).HasMaxLength(100);
            entity.Property(e => e.ReferenceType).HasMaxLength(50);
            entity.Property(e => e.Metadata);
            entity.Property(e => e.ScheduledAt);
            entity.Property(e => e.SentAt);
            entity.Property(e => e.DeliveredAt);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.RetryCount).HasDefaultValue(0);
            entity.Property(e => e.ErrorMessage);
            entity.Property(e => e.ExternalId).HasMaxLength(200);

            // Indexes per performance
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => new { e.ReferenceId, e.ReferenceType });
        });

        // Configurazione tabella NotificationTemplates
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.SubjectTemplate).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ContentTemplate).IsRequired();
            entity.Property(e => e.HtmlTemplate);
            entity.Property(e => e.Variables);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Index univoco per nome template
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
        });

        // Configurazione tabella NotificationPreferences
        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            entity.Property(e => e.PreferredChannel).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Index univoco per combinazione utente-tipo-categoria
            entity.HasIndex(e => new { e.UserId, e.Type, e.Category }).IsUnique();
        });

        // Configurazione tabella NotificationLogs
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            
            entity.Property(e => e.NotificationId).IsRequired();
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Details);
            entity.Property(e => e.Provider).HasMaxLength(50);
            entity.Property(e => e.Timestamp).IsRequired();

            // Foreign key verso Notifications
            entity.HasOne(e => e.Notification)
                  .WithMany()
                  .HasForeignKey(e => e.NotificationId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes per performance
            entity.HasIndex(e => e.NotificationId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Provider);
        });

        // Seed data per template di base
        SeedTemplates(modelBuilder);
    }

    /// <summary>
    /// Popola alcuni template di base per il sistema
    /// </summary>
    private static void SeedTemplates(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        modelBuilder.Entity<NotificationTemplate>().HasData(
            new NotificationTemplate
            {
                Id = 1,
                Name = "order_confirmation",
                Description = "Conferma ordine effettuato",
                Type = NotificationType.Email,
                SubjectTemplate = "Conferma ordine #{orderId} - {companyName}",
                ContentTemplate = "Ciao {customerName},\\n\\nGrazie per il tuo ordine #{orderId}!\\n\\nDettagli ordine:\\n{orderDetails}\\n\\nTotale: {total}\\n\\nGrazie per averci scelto!\\n\\n{companyName}",
                HtmlTemplate = "<h2>Conferma ordine #{orderId}</h2><p>Ciao <strong>{customerName}</strong>,</p><p>Grazie per il tuo ordine!</p><div>{orderDetails}</div><p><strong>Totale: {total}</strong></p>",
                Variables = @"{""orderId"": ""ID ordine"", ""customerName"": ""Nome cliente"", ""orderDetails"": ""Dettagli ordine"", ""total"": ""Totale ordine"", ""companyName"": ""Nome azienda""}",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new NotificationTemplate
            {
                Id = 2,
                Name = "order_shipped",
                Description = "Notifica spedizione ordine",
                Type = NotificationType.SMS,
                SubjectTemplate = "Ordine #{orderId} spedito",
                ContentTemplate = "Ciao {customerName}! Il tuo ordine #{orderId} è stato spedito. Tracking: {trackingNumber}. Consegna prevista: {deliveryDate}",
                Variables = @"{""orderId"": ""ID ordine"", ""customerName"": ""Nome cliente"", ""trackingNumber"": ""Codice tracking"", ""deliveryDate"": ""Data consegna""}",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new NotificationTemplate
            {
                Id = 3,
                Name = "payment_reminder",
                Description = "Promemoria pagamento in scadenza",
                Type = NotificationType.Email,
                SubjectTemplate = "Promemoria pagamento - Ordine #{orderId}",
                ContentTemplate = "Ciao {customerName},\\n\\nIl pagamento per l'ordine #{orderId} scadrà il {dueDate}.\\n\\nImporto: {amount}\\n\\nEffettua il pagamento entro la scadenza per evitare interruzioni del servizio.\\n\\nGrazie!",
                HtmlTemplate = "<h3>Promemoria Pagamento</h3><p>Ciao {customerName},</p><p>Il pagamento per l'ordine <strong>#{orderId}</strong> scadrà il <strong>{dueDate}</strong>.</p><p>Importo: <strong>{amount}</strong></p><p>Ti preghiamo di effettuare il pagamento entro la scadenza.</p>",
                Variables = @"{""orderId"": ""ID ordine"", ""customerName"": ""Nome cliente"", ""dueDate"": ""Data scadenza"", ""amount"": ""Importo da pagare""}",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new NotificationTemplate
            {
                Id = 4,
                Name = "welcome_user",
                Description = "Messaggio di benvenuto nuovo utente",
                Type = NotificationType.Push,
                SubjectTemplate = "Benvenuto in {appName}!",
                ContentTemplate = "Ciao {userName}! Benvenuto in {appName}. Scopri tutte le funzionalità della nostra app e inizia subito a fare shopping!",
                Variables = @"{""userName"": ""Nome utente"", ""appName"": ""Nome applicazione""}",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new NotificationTemplate
            {
                Id = 5,
                Name = "system_maintenance",
                Description = "Notifica manutenzione sistema",
                Type = NotificationType.InApp,
                SubjectTemplate = "Manutenzione programmata sistema",
                ContentTemplate = "Attenzione: il sistema sarà in manutenzione il {maintenanceDate} dalle {startTime} alle {endTime}. Alcune funzionalità potrebbero non essere disponibili.",
                Variables = @"{""maintenanceDate"": ""Data manutenzione"", ""startTime"": ""Ora inizio"", ""endTime"": ""Ora fine""}",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );
    }
}