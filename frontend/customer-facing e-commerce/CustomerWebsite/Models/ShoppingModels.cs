using System.ComponentModel.DataAnnotations;

namespace CustomerWebsite.Models;

/// <summary>
/// Modello del carrello della spesa
/// </summary>
public class ShoppingCartModel
{
    public List<CartItemModel> Items { get; set; } = new();
    public decimal Subtotal => Items.Sum(item => item.TotalPrice);
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total => Subtotal + ShippingCost + TaxAmount - DiscountAmount;
    public int TotalItems => Items.Sum(item => item.Quantity);
    public string? PromoCode { get; set; }
    public bool IsEligibleForFreeShipping => Subtotal >= 25.00m || Items.Any(i => i.Product.IsPrimeEligible);
}

/// <summary>
/// Modello degli elementi del carrello
/// </summary>
public class CartItemModel
{
    public Guid CartItemId { get; set; }
    public ProductDisplayModel Product { get; set; } = new();
    
    [Range(1, 99, ErrorMessage = "La quantità deve essere tra 1 e 99")]
    public int Quantity { get; set; } = 1;
    
    public decimal UnitPrice => Product.FinalPrice;
    public decimal TotalPrice => UnitPrice * Quantity;
    public DateTime AddedAt { get; set; }
    public string? SelectedVariant { get; set; }
}

/// <summary>
/// Modello per aggiungere al carrello
/// </summary>
public class AddToCartModel
{
    [Required(ErrorMessage = "ID prodotto richiesto")]
    public Guid ProductId { get; set; }
    
    [Range(1, 99, ErrorMessage = "La quantità deve essere tra 1 e 99")]
    public int Quantity { get; set; } = 1;
    
    public string? VariantId { get; set; }
}

/// <summary>
/// Modello per aggiornare la quantità nel carrello
/// </summary>
public class UpdateCartItemModel
{
    [Required(ErrorMessage = "ID elemento carrello richiesto")]
    public Guid CartItemId { get; set; }
    
    [Range(0, 99, ErrorMessage = "La quantità deve essere tra 0 e 99")]
    public int Quantity { get; set; }
}

/// <summary>
/// Modello della wishlist
/// </summary>
public class WishlistModel
{
    public List<WishlistItemModel> Items { get; set; } = new();
    public int TotalItems => Items.Count;
}

/// <summary>
/// Modello degli elementi della wishlist
/// </summary>
public class WishlistItemModel
{
    public Guid WishlistItemId { get; set; }
    public ProductDisplayModel Product { get; set; } = new();
    public DateTime AddedAt { get; set; }
    public string? Notes { get; set; }
    public bool IsAvailable => Product.IsInStock;
    public bool PriceChanged { get; set; }
}

/// <summary>
/// Modello per il confronto prodotti
/// </summary>
public class ProductComparisonModel
{
    public List<ProductDisplayModel> Products { get; set; } = new();
    public Dictionary<string, List<string>> ComparisonAttributes { get; set; } = new();
    public int MaxProducts { get; } = 4;
    public bool CanAddMore => Products.Count < MaxProducts;
}

/// <summary>
/// Modello per le recensioni dei prodotti
/// </summary>
public class ProductReviewModel
{
    public Guid ReviewId { get; set; }
    public Guid ProductId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    
    [Range(1, 5, ErrorMessage = "Il rating deve essere tra 1 e 5")]
    public int Rating { get; set; }
    
    [StringLength(100, ErrorMessage = "Il titolo non può superare i 100 caratteri")]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(2000, ErrorMessage = "La recensione non può superare i 2000 caratteri")]
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public int HelpfulVotes { get; set; }
    public int TotalVotes { get; set; }
    public List<string> Images { get; set; } = new();
}

/// <summary>
/// Modello per aggiungere una recensione
/// </summary>
public class AddReviewModel
{
    [Required(ErrorMessage = "ID prodotto richiesto")]
    public Guid ProductId { get; set; }
    
    [Required(ErrorMessage = "Rating richiesto")]
    [Range(1, 5, ErrorMessage = "Il rating deve essere tra 1 e 5")]
    public int Rating { get; set; }
    
    [Required(ErrorMessage = "Titolo richiesto")]
    [StringLength(100, ErrorMessage = "Il titolo non può superare i 100 caratteri")]
    public string Title { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Contenuto richiesto")]
    [StringLength(2000, ErrorMessage = "La recensione non può superare i 2000 caratteri")]
    public string Content { get; set; } = string.Empty;
}