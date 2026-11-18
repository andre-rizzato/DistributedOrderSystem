namespace ChatbotService.Data;

using Microsoft.EntityFrameworkCore;
using ChatbotService.Data.Entities;

public class ChatContext : DbContext
{
    public ChatContext(DbContextOptions<ChatContext> options) : base(options) { }

    public DbSet<Entities.ChatSession> ChatSessions { get; set; }
    public DbSet<Entities.ChatMessage> ChatMessages { get; set; }

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
    }
}