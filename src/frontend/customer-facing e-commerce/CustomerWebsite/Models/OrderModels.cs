using System.ComponentModel.DataAnnotations;

namespace CustomerWebsite.Models;

/// <summary>
/// Order model for checkout
/// </summary>
public class OrderModel
{
    // OrderService's real primary key is an int (EF Core identity), not a Guid -
    // this has to match GatewayBff's contract for order ids to be usable at all.
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }

    // Customer information
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;

    // Addresses
    public AddressModel ShippingAddress { get; set; } = new();
    public AddressModel BillingAddress { get; set; } = new();
    public bool UseSameAddressForBilling { get; set; } = true;

    // Order items
    public List<OrderItemModel> Items { get; set; } = new();

    // Payment information
    public PaymentMethodModel PaymentMethod { get; set; } = new();

    // Shipping
    public ShippingOptionModel ShippingOption { get; set; } = new();

    // Totals
    public decimal Subtotal => Items.Sum(item => item.TotalPrice);
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    // Additional information
    public string? PromoCode { get; set; }
    public string? OrderNotes { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
}

/// <summary>
/// Order item model
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
/// Order statuses
/// </summary>
public enum OrderStatus
{
    [Display(Name = "Pending")]
    Pending,

    [Display(Name = "Confirmed")]
    Confirmed,

    [Display(Name = "Processing")]
    Processing,

    [Display(Name = "Shipped")]
    Shipped,

    [Display(Name = "Delivered")]
    Delivered,

    [Display(Name = "Cancelled")]
    Cancelled,

    [Display(Name = "Returned")]
    Returned
}

/// <summary>
/// Checkout model
/// </summary>
public class CheckoutModel
{
    // Step 1: Cart review
    public ShoppingCartModel Cart { get; set; } = new();

    // Step 2: Shipping address
    [Required(ErrorMessage = "Shipping address is required")]
    public AddressModel ShippingAddress { get; set; } = new();

    // Step 3: Billing address
    public AddressModel? BillingAddress { get; set; }
    public bool UseSameAddressForBilling { get; set; } = true;

    // Step 4: Shipping method
    [Required(ErrorMessage = "Shipping method is required")]
    public ShippingOptionModel SelectedShippingOption { get; set; } = new();
    public List<ShippingOptionModel> AvailableShippingOptions { get; set; } = new();

    // Step 5: Payment method
    [Required(ErrorMessage = "Payment method is required")]
    public PaymentMethodModel PaymentMethod { get; set; } = new();

    // Promo codes
    public string? PromoCode { get; set; }
    public decimal DiscountAmount { get; set; }

    // Order notes
    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? OrderNotes { get; set; }

    // Totals
    public decimal Subtotal => Cart.Subtotal;
    public decimal ShippingCost => SelectedShippingOption.Cost;
    public decimal TaxAmount { get; set; }
    public decimal Total => Subtotal + ShippingCost + TaxAmount - DiscountAmount;

    // Current step in the checkout process
    public CheckoutStep CurrentStep { get; set; } = CheckoutStep.ReviewCart;
}

/// <summary>
/// Checkout steps
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
/// Shipping options model
/// </summary>
public class ShippingOptionModel
{
    public Guid ShippingOptionId { get; set; }

    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Cost")]
    [DataType(DataType.Currency)]
    public decimal Cost { get; set; }

    [Display(Name = "Delivery Days")]
    public int DeliveryDays { get; set; }

    [Display(Name = "Estimated Delivery Date")]
    public DateTime EstimatedDeliveryDate => DateTime.Now.AddDays(DeliveryDays);

    [Display(Name = "Express Shipping")]
    public bool IsExpress { get; set; }

    [Display(Name = "Tracking Included")]
    public bool IncludesTracking { get; set; }

    [Display(Name = "Insurance")]
    public bool IncludesInsurance { get; set; }
}

/// <summary>
/// Order summary model
/// </summary>
public class OrderSummaryModel
{
    public OrderModel Order { get; set; } = new();
    public bool IsGuestCheckout { get; set; }
    public string? ConfirmationMessage { get; set; }
    public List<string> NextSteps { get; set; } = new();
}

/// <summary>
/// Model for applying a promo code
/// </summary>
public class PromoCodeModel
{
    [Required(ErrorMessage = "Promo code is required")]
    [Display(Name = "Promo Code")]
    public string Code { get; set; } = string.Empty;

    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal DiscountAmount { get; set; }
    public string? DiscountDescription { get; set; }
}

/// <summary>
/// Model for order history
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
/// Filters for order history
/// </summary>
public class OrderHistoryFilter
{
    [Display(Name = "Order Status")]
    public OrderStatus? Status { get; set; }

    [Display(Name = "From Date")]
    [DataType(DataType.Date)]
    public DateTime? StartDate { get; set; }

    [Display(Name = "To Date")]
    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Display(Name = "Order Number")]
    public string? OrderNumber { get; set; }
}
