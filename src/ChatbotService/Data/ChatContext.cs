namespace ChatbotService.Data;

using Microsoft.EntityFrameworkCore;
using ChatbotService.Data.Entities;
using IntentTypes = ChatbotService.Models.IntentTypes;

public class ChatContext : DbContext
{
    public ChatContext(DbContextOptions<ChatContext> options) : base(options) { }

    public DbSet<Entities.ChatSession> ChatSessions { get; set; }
    public DbSet<Entities.ChatMessage> ChatMessages { get; set; }
    public DbSet<TrainingData> TrainingData { get; set; }
    public DbSet<FineTuningJob> FineTuningJobs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ChatSession
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserEmail);
            entity.Property(e => e.Id).HasMaxLength(50);
        });

        // Configure ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Intent);
            
            // Foreign key relationship
            entity.HasOne(e => e.Session)
                  .WithMany(e => e.Messages)
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TrainingData
        modelBuilder.Entity<TrainingData>(entity =>
        {
            entity.HasIndex(e => e.Intent);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsValidated);
        });

        // Configure FineTuningJob
        modelBuilder.Entity<FineTuningJob>(entity =>
        {
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.CreatedBy);
        });

        // Seed data for development
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed some training data examples
        modelBuilder.Entity<TrainingData>().HasData(
            new TrainingData 
            { 
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Input = "Ciao, come stai?",
                ExpectedOutput = "Ciao! Sto bene, grazie. Come posso aiutarti oggi?",
                Intent = IntentTypes.GREETING,
                IsValidated = true,
                Source = "Seed"
            },
            new TrainingData 
            { 
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Input = "Qual è lo stato del mio ordine 12345?",
                ExpectedOutput = "Cerco le informazioni del tuo ordine #12345. Un momento...",
                Intent = IntentTypes.ORDER_STATUS,
                Entities = "{\"order_id\": \"12345\"}",
                IsValidated = true,
                Source = "Seed"
            },
            new TrainingData 
            { 
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Input = "Voglio cancellare il mio ordine",
                ExpectedOutput = "Mi dispiace sentire che vuoi cancellare il tuo ordine. Puoi fornirmi il numero dell'ordine?",
                Intent = IntentTypes.CANCEL_ORDER,
                IsValidated = true,
                Source = "Seed"
            },
            new TrainingData 
            { 
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Input = "Cerca laptop gaming",
                ExpectedOutput = "Sto cercando laptop gaming disponibili nel nostro catalogo...",
                Intent = IntentTypes.PRODUCT_SEARCH,
                Entities = "{\"product_name\": \"laptop gaming\"}",
                IsValidated = true,
                Source = "Seed"
            }
        );
    }
}