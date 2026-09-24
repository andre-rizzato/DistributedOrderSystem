using System.ComponentModel.DataAnnotations;

namespace CustomerWebsite.Models;

/// <summary>
/// Shopping cart model
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
/// Cart item model
/// </summary>
public class CartItemModel
{
    public Guid CartItemId { get; set; }
    public ProductDisplayModel Product { get; set; } = new();

    [Range(1, 99, ErrorMessage = "Quantity must be between 1 and 99")]
    public int Quantity { get; set; } = 1;

    public decimal UnitPrice => Product.FinalPrice;
    public decimal TotalPrice => UnitPrice * Quantity;
    public DateTime AddedAt { get; set; }
    public string? SelectedVariant { get; set; }
}

/// <summary>
/// Model for adding an item to the cart
/// </summary>
public class AddToCartModel
{
    [Required(ErrorMessage = "Product ID is required")]
    public Guid ProductId { get; set; }

    [Range(1, 99, ErrorMessage = "Quantity must be between 1 and 99")]
    public int Quantity { get; set; } = 1;

    public string? VariantId { get; set; }
}

/// <summary>
/// Model for updating a cart item's quantity
/// </summary>
public class UpdateCartItemModel
{
    [Required(ErrorMessage = "Cart item ID is required")]
    public Guid CartItemId { get; set; }

    [Range(0, 99, ErrorMessage = "Quantity must be between 0 and 99")]
    public int Quantity { get; set; }
}

/// <summary>
/// Wishlist model
/// </summary>
public class WishlistModel
{
    public List<WishlistItemModel> Items { get; set; } = new();
    public int TotalItems => Items.Count;
}

/// <summary>
/// Wishlist item model
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
/// Model for product comparison
/// </summary>
public class ProductComparisonModel
{
    public List<ProductDisplayModel> Products { get; set; } = new();
    public Dictionary<string, List<string>> ComparisonAttributes { get; set; } = new();
    public int MaxProducts { get; } = 4;
    public bool CanAddMore => Products.Count < MaxProducts;
}

/// <summary>
/// Model for product reviews
/// </summary>
public class ProductReviewModel
{
    public Guid ReviewId { get; set; }
    public Guid ProductId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Review cannot exceed 2000 characters")]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public int HelpfulVotes { get; set; }
    public int TotalVotes { get; set; }
    public List<string> Images { get; set; } = new();
}

/// <summary>
/// Model for adding a review
/// </summary>
public class AddReviewModel
{
    [Required(ErrorMessage = "Product ID is required")]
    public Guid ProductId { get; set; }

    [Required(ErrorMessage = "Rating is required")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required")]
    [StringLength(2000, ErrorMessage = "Review cannot exceed 2000 characters")]
    public string Content { get; set; } = string.Empty;
}
