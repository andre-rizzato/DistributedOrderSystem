namespace ChatbotService.Data.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ChatMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string SessionId { get; set; } = string.Empty;
    
    public string? UserId { get; set; }
    
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string UserMessage { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string BotResponse { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Intent { get; set; } = string.Empty;
    
    public float Confidence { get; set; }
    
    [MaxLength(20)]
    public string Sentiment { get; set; } = "neutral";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool RequiredHumanEscalation { get; set; }
    
    [Column(TypeName = "nvarchar(max)")]
    public string? Metadata { get; set; }
    
    // Foreign key
    public virtual ChatSession? Session { get; set; }
}