namespace ChatbotService.Data.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ChatSession
{
    [Key]
    public string Id { get; set; } = string.Empty;
    
    public string? UserId { get; set; }
    
    [MaxLength(100)]
    public string? UserEmail { get; set; }
    
    [MaxLength(20)]
    public string? UserPhone { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastInteractionAt { get; set; } = DateTime.UtcNow;
    
    [MaxLength(50)]
    public string Status { get; set; } = "Active";
    
    public bool IsAuthenticated { get; set; }
    
    [Column(TypeName = "nvarchar(max)")]
    public string? Context { get; set; }
    
    // Navigation property
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}