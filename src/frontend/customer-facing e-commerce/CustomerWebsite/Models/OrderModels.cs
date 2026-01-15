using System.ComponentModel.DataAnnotations;

namespace CustomerWebsite.Models;

/// <summary>
/// Modello dell'ordine per il checkout
/// </summary>
public class OrderModel
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    
    // Informazioni cliente
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    
    // Indirizzi
    public AddressModel ShippingAddress { get; set; } = new();
    public AddressModel BillingAddress { get; set; } = new();
    public bool UseSameAddressForBilling { get; set; } = true;
    
    // Elementi dell'ordine
    public List<OrderItemModel> Items { get; set; } = new();
    
    // Informazioni di pagamento
    public PaymentMethodModel PaymentMethod { get; set; } = new();
    
    // Spedizione
    public ShippingOptionModel ShippingOption { get; set; } = new();
    
    // Totali
    public decimal Subtotal => Items.Sum(item => item.TotalPrice);
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    
    // Informazioni aggiuntive
    public string? PromoCode { get; set; }
    public string? OrderNotes { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
}

/// <summary>
/// Modello degli elementi dell'ordine
/// </summary>
public class OrderItemModel
{
    public Guid OrderItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
    public string? VariantInfo { get; set; }
    public DateTime? ShippingDate { get; set; }
    public string? TrackingNumber { get; set; }
}

/// <summary>
/// Stati dell'ordine
/// </summary>
public enum OrderStatus
{
    [Display(Name = "In Attesa")]
    Pending,
    
    [Display(Name = "Confermato")]
    Confirmed,
    
    [Display(Name = "In Elaborazione")]
    Processing,
    
    [Display(Name = "Spedito")]
    Shipped,
    
    [Display(Name = "Consegnato")]
    Delivered,
    
    [Display(Name = "Annullato")]
    Cancelled,
    
    [Display(Name = "Restituito")]
    Returned
}

/// <summary>
/// Modello del checkout
/// </summary>
public class CheckoutModel
{
    // Step 1: Revisione carrello
    public ShoppingCartModel Cart { get; set; } = new();
    
    // Step 2: Indirizzo di spedizione
    [Required(ErrorMessage = "Indirizzo di spedizione richiesto")]
    public AddressModel ShippingAddress { get; set; } = new();
    
    // Step 3: Indirizzo di fatturazione
    public AddressModel? BillingAddress { get; set; }
    public bool UseSameAddressForBilling { get; set; } = true;
    
    // Step 4: Metodo di spedizione
    [Required(ErrorMessage = "Metodo di spedizione richiesto")]
    public ShippingOptionModel SelectedShippingOption { get; set; } = new();
    public List<ShippingOptionModel> AvailableShippingOptions { get; set; } = new();
    
    // Step 5: Metodo di pagamento
    [Required(ErrorMessage = "Metodo di pagamento richiesto")]
    public PaymentMethodModel PaymentMethod { get; set; } = new();
    
    // Codici promozionali
    public string? PromoCode { get; set; }
    public decimal DiscountAmount { get; set; }
    
    // Note ordine
    [StringLength(500, ErrorMessage = "Le note non possono superare i 500 caratteri")]
    public string? OrderNotes { get; set; }
    
    // Totali
    public decimal Subtotal => Cart.Subtotal;
    public decimal ShippingCost => SelectedShippingOption.Cost;
    public decimal TaxAmount { get; set; }
    public decimal Total => Subtotal + ShippingCost + TaxAmount - DiscountAmount;
    
    // Step corrente nel processo di checkout
    public CheckoutStep CurrentStep { get; set; } = CheckoutStep.ReviewCart;
}

/// <summary>
/// Passi del checkout
/// </summary>
public enum CheckoutStep
{
    ReviewCart = 1,
    ShippingAddress = 2,
    BillingAddress = 3,
    ShippingMethod = 4,
    PaymentMethod = 5,
    ReviewOrder = 6,
    OrderConfirmation = 7
}

/// <summary>
/// Modello delle opzioni di spedizione
/// </summary>
public class ShippingOptionModel
{
    public Guid ShippingOptionId { get; set; }
    
    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;
    
    [Display(Name = "Descrizione")]
    public string Description { get; set; } = string.Empty;
    
    [Display(Name = "Costo")]
    [DataType(DataType.Currency)]
    public decimal Cost { get; set; }
    
    [Display(Name = "Giorni di Consegna")]
    public int DeliveryDays { get; set; }
    
    [Display(Name = "Data Consegna Stimata")]
    public DateTime EstimatedDeliveryDate => DateTime.Now.AddDays(DeliveryDays);
    
    [Display(Name = "Spedizione Express")]
    public bool IsExpress { get; set; }
    
    [Display(Name = "Tracking Incluso")]
    public bool IncludesTracking { get; set; }
    
    [Display(Name = "Assicurazione")]
    public bool IncludesInsurance { get; set; }
}

/// <summary>
/// Modello del riepilogo ordine
/// </summary>
public class OrderSummaryModel
{
    public OrderModel Order { get; set; } = new();
    public bool IsGuestCheckout { get; set; }
    public string? ConfirmationMessage { get; set; }
    public List<string> NextSteps { get; set; } = new();
}

/// <summary>
/// Modello per l'applicazione del codice promozionale
/// </summary>
public class PromoCodeModel
{
    [Required(ErrorMessage = "Codice promozionale richiesto")]
    [Display(Name = "Codice Promozionale")]
    public string Code { get; set; } = string.Empty;
    
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountDescription { get; set; }
}

/// <summary>
/// Modello per la cronologia degli ordini
/// </summary>
public class OrderHistoryModel
{
    public List<OrderModel> Orders { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public OrderHistoryFilter Filter { get; set; } = new();
    
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

/// <summary>
/// Filtri per la cronologia ordini
/// </summary>
public class OrderHistoryFilter
{
    [Display(Name = "Stato Ordine")]
    public OrderStatus? Status { get; set; }
    
    [Display(Name = "Data Da")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }
    
    [Display(Name = "Data A")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }
    
    [Display(Name = "Numero Ordine")]
    public string? OrderNumber { get; set; }
}