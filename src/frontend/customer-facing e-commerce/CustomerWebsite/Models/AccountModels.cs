using System.ComponentModel.DataAnnotations;

namespace CustomerWebsite.Models;

/// <summary>
/// Model for user authentication
/// </summary>
public class LoginModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Model for user registration
/// </summary>
public class RegisterModel
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "You must accept the terms")]
    [Display(Name = "I accept the Terms and Conditions")]
    public bool AcceptTerms { get; set; }

    [Display(Name = "Subscribe to the newsletter")]
    public bool SubscribeToNewsletter { get; set; }
}

/// <summary>
/// User profile model
/// </summary>
public class UserProfileModel
{
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(50)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Phone")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime? DateOfBirth { get; set; }

    [Display(Name = "Gender")]
    public string? Gender { get; set; }

    public List<AddressModel> Addresses { get; set; } = new();
    public List<PaymentMethodModel> PaymentMethods { get; set; } = new();

    [Display(Name = "Communication Preferences")]
    public CommunicationPreferencesModel CommunicationPreferences { get; set; } = new();

    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>
/// Address model
/// </summary>
public class AddressModel
{
    public Guid AddressId { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required")]
    [Display(Name = "Address")]
    public string Street { get; set; } = string.Empty;

    [Display(Name = "Apartment/Suite")]
    public string? Apartment { get; set; }

    [Required(ErrorMessage = "City is required")]
    [Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "State/Province is required")]
    [Display(Name = "State/Province")]
    public string State { get; set; } = string.Empty;

    [Required(ErrorMessage = "Postal code is required")]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "Invalid postal code")]
    [Display(Name = "Postal Code")]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Country is required")]
    [Display(Name = "Country")]
    public string Country { get; set; } = "Italy";

    [Phone]
    [Display(Name = "Phone")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Default Address")]
    public bool IsDefault { get; set; }

    [Display(Name = "Address Type")]
    public AddressType Type { get; set; } = AddressType.Home;

    public string FormattedAddress => $"{Street}, {City}, {State} {PostalCode}, {Country}";
}

/// <summary>
/// Address types
/// </summary>
public enum AddressType
{
    [Display(Name = "Home")]
    Home,

    [Display(Name = "Office")]
    Office,

    [Display(Name = "Other")]
    Other
}

/// <summary>
/// Payment method model
/// </summary>
public class PaymentMethodModel
{
    public Guid PaymentMethodId { get; set; }

    [Display(Name = "Type")]
    public PaymentType Type { get; set; }

    [Display(Name = "Cardholder Name")]
    public string? CardHolderName { get; set; }

    [Display(Name = "Last 4 digits")]
    public string? Last4Digits { get; set; }

    [Display(Name = "Expiry")]
    public string? ExpiryDate { get; set; }

    [Display(Name = "Card Type")]
    public string? CardBrand { get; set; }

    [Display(Name = "Default Method")]
    public bool IsDefault { get; set; }

    [Display(Name = "Display Name")]
    public string DisplayName => Type == PaymentType.CreditCard ?
        $"{CardBrand} ending in {Last4Digits}" :
        Type.ToString();
}

/// <summary>
/// Payment types
/// </summary>
public enum PaymentType
{
    [Display(Name = "Credit Card")]
    CreditCard,

    [Display(Name = "PayPal")]
    PayPal,

    [Display(Name = "Bank Transfer")]
    BankTransfer,

    [Display(Name = "Cash on Delivery")]
    CashOnDelivery
}

/// <summary>
/// Communication preferences model
/// </summary>
public class CommunicationPreferencesModel
{
    [Display(Name = "Newsletter")]
    public bool Newsletter { get; set; } = true;

    [Display(Name = "Special Offers")]
    public bool SpecialOffers { get; set; } = true;

    [Display(Name = "Order Updates")]
    public bool OrderUpdates { get; set; } = true;

    [Display(Name = "SMS Notifications")]
    public bool SmsNotifications { get; set; } = false;

    [Display(Name = "Push Notifications")]
    public bool PushNotifications { get; set; } = true;
}
