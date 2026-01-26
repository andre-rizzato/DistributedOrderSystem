namespace ChatbotService.Data.Entities;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class TrainingData
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [Column(TypeName = "text")]
    public string Input { get; set; } = string.Empty;
    
    [Required]
    [Column(TypeName = "text")]
    public string ExpectedOutput { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Intent { get; set; } = string.Empty;
    
    [Column(TypeName = "text")]
    public string? Entities { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public string? UserId { get; set; }
    
    [MaxLength(50)]
    public string Source { get; set; } = "Manual";
    
    public bool IsValidated { get; set; }
    
    public bool IsActive { get; set; } = true;
}

public class FineTuningJob
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [MaxLength(100)]
    public string? ModelName { get; set; }
    
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
    
    public int TotalEpochs { get; set; }
    public int CurrentEpoch { get; set; }
    
    public float CurrentLoss { get; set; }
    public float ValidationAccuracy { get; set; }
    
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    [Column(TypeName = "text")]
    public string? ErrorMessage { get; set; }
    
    [Column(TypeName = "text")]
    public string? TrainingParameters { get; set; }
    
    [MaxLength(500)]
    public string? ModelPath { get; set; }
    
    public int TrainingDataCount { get; set; }
    
    public string? CreatedBy { get; set; }
}