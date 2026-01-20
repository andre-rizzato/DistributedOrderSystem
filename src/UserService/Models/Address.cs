using System.ComponentModel.DataAnnotations;

namespace UserService.Models;

/// <summary>
/// Indirizzo di spedizione o fatturazione
/// </summary>
public class Address
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string AddressLine1 { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? AddressLine2 { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string? StateProvince { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string PostalCode { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(2)]
    public string CountryCode { get; set; } = "IT";
    
    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }
    
    public AddressType Type { get; set; } = AddressType.Shipping;
    public bool IsDefault { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Relazioni
    public virtual ApplicationUser User { get; set; } = null!;
}

public enum AddressType
{
    Shipping = 1,
    Billing = 2,
    Both = 3
}
