using Microsoft.EntityFrameworkCore;
using NotificationService.Models;

namespace NotificationService.Data;

/// <summary>
/// Database context for the notification service
/// </summary>
public class NotificationContext : DbContext
{
    public NotificationContext(DbContextOptions<NotificationContext> options) : base(options)
    {
    }

    /// <summary>
    /// Notifications
    /// </summary>
    public DbSet<Notification> Notifications { get; set; }

    /// <summary>
    /// Notification templates
    /// </summary>
    public DbSet<NotificationTemplate> NotificationTemplates { get; set; }

    /// <summary>
    /// Per-user notification preferences
    /// </summary>
    public DbSet<NotificationPreference> NotificationPreferences { get; set; }

    /// <summary>
    /// Notification operation logs
    /// </summary>
    public DbSet<NotificationLog> NotificationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Notifications table configuration
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

            // Indexes for performance
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => new { e.ReferenceId, e.ReferenceType });
        });

        // NotificationTemplates table configuration
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

            // Unique index on template name
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
        });

        // NotificationPreferences table configuration
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

            // Unique index on the user-type-category combination
            entity.HasIndex(e => new { e.UserId, e.Type, e.Category }).IsUnique();
        });

        // NotificationLogs table configuration
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();

            entity.Property(e => e.NotificationId).IsRequired();
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Details);
            entity.Property(e => e.Provider).HasMaxLength(50);
            entity.Property(e => e.Timestamp).IsRequired();

            // Foreign key to Notifications
            entity.HasOne(e => e.Notification)
                  .WithMany()
                  .HasForeignKey(e => e.NotificationId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(e => e.NotificationId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.Provider);
        });

        // Seed data for the base templates
        SeedTemplates(modelBuilder);
    }

    /// <summary>
    /// Seeds a few base templates for the system
    /// </summary>
    private static void SeedTemplates(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<NotificationTemplate>().HasData(
            new NotificationTemplate
            {
                Id = 1,
                Name = "order_confirmation",
                Description = "Order confirmation",
                Type = NotificationType.Email,
                SubjectTemplate = "Order confirmation #{orderId} - {companyName}",
                ContentTemplate = "Hi {customerName},\\n\\nThank you for your order #{orderId}!\\n\\nOrder details:\\n{orderDetails}\\n\\nTotal: {total}\\n\\nThanks for choosing us!\\n\\n{companyName}",
                HtmlTemplate = "<h2>Order confirmation #{orderId}</h2><p>Hi <strong>{customerName}</strong>,</p><p>Thank you for your order!</p><div>{orderDetails}</div><p><strong>Total: {total}</strong></p>",
                Variables = @"{""orderId"": ""Order ID"", ""customerName"": ""Customer name"", ""orderDetails"": ""Order details"", ""total"": ""Order total"", ""companyName"": ""Company name""}",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new NotificationTemplate
            {
                Id = 2,
                Name = "order_shipped",
                Description = "Order shipped notification",
                Type = NotificationType.SMS,
                SubjectTemplate = "Order #{orderId} shipped",
                ContentTemplate = "Hi {customerName}! Your order #{orderId} has shipped. Tracking: {trackingNumber}. Expected delivery: {deliveryDate}",
                Variables = @"{""orderId"": ""Order ID"", ""customerName"": ""Customer name"", ""trackingNumber"": ""Tracking code"", ""deliveryDate"": ""Delivery date""}",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new NotificationTemplate
            {
                Id = 3,
                Name = "payment_reminder",
                Description = "Payment due reminder",
                Type = NotificationType.Email,
                SubjectTemplate = "Payment reminder - Order #{orderId}",
                ContentTemplate = "Hi {customerName},\\n\\nPayment for order #{orderId} is due on {dueDate}.\\n\\nAmount: {amount}\\n\\nPlease pay before the due date to avoid service interruption.\\n\\nThanks!",
                HtmlTemplate = "<h3>Payment Reminder</h3><p>Hi {customerName},</p><p>Payment for order <strong>#{orderId}</strong> is due on <strong>{dueDate}</strong>.</p><p>Amount: <strong>{amount}</strong></p><p>Please pay before the due date.</p>",
                Variables = @"{""orderId"": ""Order ID"", ""customerName"": ""Customer name"", ""dueDate"": ""Due date"", ""amount"": ""Amount due""}",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new NotificationTemplate
            {
                Id = 4,
                Name = "welcome_user",
                Description = "New user welcome message",
                Type = NotificationType.Push,
                SubjectTemplate = "Welcome to {appName}!",
                ContentTemplate = "Hi {userName}! Welcome to {appName}. Discover all our app's features and start shopping right away!",
                Variables = @"{""userName"": ""User name"", ""appName"": ""Application name""}",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new NotificationTemplate
            {
                Id = 5,
                Name = "system_maintenance",
                Description = "System maintenance notification",
                Type = NotificationType.InApp,
                SubjectTemplate = "Scheduled system maintenance",
                ContentTemplate = "Notice: the system will be under maintenance on {maintenanceDate} from {startTime} to {endTime}. Some features may be unavailable.",
                Variables = @"{""maintenanceDate"": ""Maintenance date"", ""startTime"": ""Start time"", ""endTime"": ""End time""}",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            }
        );
    }
}
