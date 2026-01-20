using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

/// <summary>
/// Metodo di pagamento salvato (carta di credito criptata)
/// </summary>
public class PaymentMethod
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    public PaymentType Type { get; set; } = PaymentType.CreditCard;
    
    [Required]
    [MaxLength(100)]
    public string CardHolderName { get; set; } = string.Empty;
    
    // Ultimi 4 numeri della carta (in chiaro)
    [Required]
    [MaxLength(4)]
    public string Last4Digits { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string CardBrand { get; set; } = string.Empty; // Visa, Mastercard, etc.
    
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    
    // Token criptato (mai salvare il numero completo!)
    [Required]
    public string EncryptedToken { get; set; } = string.Empty;
    
    public bool IsDefault { get; set; }
    public bool IsExpired => new DateTime(ExpiryYear, ExpiryMonth, 1) < DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Relazioni
    public virtual ApplicationUser User { get; set; } = null!;
}

public enum PaymentType
{
    CreditCard = 1,
    DebitCard = 2,
    PayPal = 3,
    BankTransfer = 4
}
