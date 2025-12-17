using System.ComponentModel.DataAnnotations;

namespace CustomerWebsite.Models;

/// <summary>
/// Modello per l'autenticazione dell'utente
/// </summary>
public class LoginModel
{
    [Required(ErrorMessage = "Email richiesta")]
    [EmailAddress(ErrorMessage = "Formato email non valido")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password richiesta")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
    
    [Display(Name = "Ricordami")]
    public bool RememberMe { get; set; }
    
    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Modello per la registrazione dell'utente
/// </summary>
public class RegisterModel
{
    [Required(ErrorMessage = "Nome richiesto")]
    [StringLength(50, ErrorMessage = "Il nome non può superare i 50 caratteri")]
    [Display(Name = "Nome")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Cognome richiesto")]
    [StringLength(50, ErrorMessage = "Il cognome non può superare i 50 caratteri")]
    [Display(Name = "Cognome")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email richiesta")]
    [EmailAddress(ErrorMessage = "Formato email non valido")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Password richiesta")]
    [StringLength(100, ErrorMessage = "La password deve essere di almeno {2} caratteri", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Conferma password richiesta")]
    [DataType(DataType.Password)]
    [Display(Name = "Conferma Password")]
    [Compare("Password", ErrorMessage = "Le password non corrispondono")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Accettazione termini richiesta")]
    [Display(Name = "Accetto i Termini e Condizioni")]
    public bool AcceptTerms { get; set; }
    
    [Display(Name = "Iscriviti alla newsletter")]
    public bool SubscribeToNewsletter { get; set; }
}

/// <summary>
/// Modello del profilo utente
/// </summary>
public class UserProfileModel
{
    public Guid UserId { get; set; }
    
    [Required(ErrorMessage = "Nome richiesto")]
    [StringLength(50)]
    [Display(Name = "Nome")]
    public string FirstName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Cognome richiesto")]
    [StringLength(50)]
    [Display(Name = "Cognome")]
    public string LastName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email richiesta")]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
    
    [Phone]
    [Display(Name = "Telefono")]
    public string? PhoneNumber { get; set; }
    
    [DataType(DataType.Date)]
    [Display(Name = "Data di Nascita")]
    public DateTime? DateOfBirth { get; set; }
    
    [Display(Name = "Genere")]
    public string? Gender { get; set; }
    
    public List<AddressModel> Addresses { get; set; } = new();
    public List<PaymentMethodModel> PaymentMethods { get; set; } = new();
    
    [Display(Name = "Preferenze di Comunicazione")]
    public CommunicationPreferencesModel CommunicationPreferences { get; set; } = new();
    
    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>
/// Modello dell'indirizzo
/// </summary>
public class AddressModel
{
    public Guid AddressId { get; set; }
    
    [Required(ErrorMessage = "Nome richiesto")]
    [Display(Name = "Nome Completo")]
    public string FullName { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Indirizzo richiesto")]
    [Display(Name = "Indirizzo")]
    public string Street { get; set; } = string.Empty;
    
    [Display(Name = "Appartamento/Scala")]
    public string? Apartment { get; set; }
    
    [Required(ErrorMessage = "Città richiesta")]
    [Display(Name = "Città")]
    public string City { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Provincia richiesta")]
    [Display(Name = "Provincia")]
    public string State { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "CAP richiesto")]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "CAP non valido")]
    [Display(Name = "CAP")]
    public string PostalCode { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Paese richiesto")]
    [Display(Name = "Paese")]
    public string Country { get; set; } = "Italia";
    
    [Phone]
    [Display(Name = "Telefono")]
    public string? PhoneNumber { get; set; }
    
    [Display(Name = "Indirizzo Principale")]
    public bool IsDefault { get; set; }
    
    [Display(Name = "Tipo Indirizzo")]
    public AddressType Type { get; set; } = AddressType.Home;
    
    public string FormattedAddress => $"{Street}, {City}, {State} {PostalCode}, {Country}";
}

/// <summary>
/// Tipi di indirizzo
/// </summary>
public enum AddressType
{
    [Display(Name = "Casa")]
    Home,
    
    [Display(Name = "Ufficio")]
    Office,
    
    [Display(Name = "Altro")]
    Other
}

/// <summary>
/// Modello del metodo di pagamento
/// </summary>
public class PaymentMethodModel
{
    public Guid PaymentMethodId { get; set; }
    
    [Display(Name = "Tipo")]
    public PaymentType Type { get; set; }
    
    [Display(Name = "Nome Carta")]
    public string? CardHolderName { get; set; }
    
    [Display(Name = "Ultime 4 cifre")]
    public string? Last4Digits { get; set; }
    
    [Display(Name = "Scadenza")]
    public string? ExpiryDate { get; set; }
    
    [Display(Name = "Tipo Carta")]
    public string? CardBrand { get; set; }
    
    [Display(Name = "Metodo Principale")]
    public bool IsDefault { get; set; }
    
    [Display(Name = "Nome Visualizzato")]
    public string DisplayName => Type == PaymentType.CreditCard ? 
        $"{CardBrand} terminante con {Last4Digits}" : 
        Type.ToString();
}

/// <summary>
/// Tipi di pagamento
/// </summary>
public enum PaymentType
{
    [Display(Name = "Carta di Credito")]
    CreditCard,
    
    [Display(Name = "PayPal")]
    PayPal,
    
    [Display(Name = "Bonifico Bancario")]
    BankTransfer,
    
    [Display(Name = "Contrassegno")]
    CashOnDelivery
}

/// <summary>
/// Modello delle preferenze di comunicazione
/// </summary>
public class CommunicationPreferencesModel
{
    [Display(Name = "Newsletter")]
    public bool Newsletter { get; set; } = true;
    
    [Display(Name = "Offerte Speciali")]
    public bool SpecialOffers { get; set; } = true;
    
    [Display(Name = "Aggiornamenti Ordini")]
    public bool OrderUpdates { get; set; } = true;
    
    [Display(Name = "Notifiche SMS")]
    public bool SmsNotifications { get; set; } = false;
    
    [Display(Name = "Notifiche Push")]
    public bool PushNotifications { get; set; } = true;
}