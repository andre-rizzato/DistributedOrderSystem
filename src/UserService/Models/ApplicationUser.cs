using Microsoft.AspNetCore.Identity;

namespace UserService.Models;

/// <summary>
/// User entity with custom fields for the e-commerce domain
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
    
    // Preferences
    public string PreferredLanguage { get; set; } = "it-IT";
    public string PreferredCurrency { get; set; } = "EUR";
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool SmsNotificationsEnabled { get; set; } = false;
    public bool PushNotificationsEnabled { get; set; } = true;
    
    // Loyalty program (Amazon Prime-style)
    public bool IsPrimeMember { get; set; }
    public DateTime? PrimeMembershipExpiry { get; set; }
    public int LoyaltyPoints { get; set; }
    
    // Social login
    public string? GoogleId { get; set; }
    public string? FacebookId { get; set; }
    
    // Tracking fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Relationships
    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
    public virtual ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();
}
