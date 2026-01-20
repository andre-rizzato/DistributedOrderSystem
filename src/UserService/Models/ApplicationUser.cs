using Microsoft.AspNetCore.Identity;

namespace UserService.Models;

/// <summary>
/// Entità utente con campi personalizzati per l'e-commerce
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
    
    // Preferenze
    public string PreferredLanguage { get; set; } = "it-IT";
    public string PreferredCurrency { get; set; } = "EUR";
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool SmsNotificationsEnabled { get; set; } = false;
    public bool PushNotificationsEnabled { get; set; } = true;
    
    // Programma fedeltà (tipo Amazon Prime)
    public bool IsPrimeMember { get; set; }
    public DateTime? PrimeMembershipExpiry { get; set; }
    public int LoyaltyPoints { get; set; }
    
    // Social login
    public string? GoogleId { get; set; }
    public string? FacebookId { get; set; }
    
    // Tracking
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Relazioni
    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
    public virtual ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
}
