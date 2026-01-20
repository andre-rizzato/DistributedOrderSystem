namespace CustomerService.Services;

using CustomerService.Models;
using CustomerService.Services.Interfaces;
using CustomerService.Storage;

public class WishlistService : IWishlistService
{
    private readonly IWishlistStorage _storage;
    private readonly ICartService _cartService;
    private readonly ILogger<WishlistService> _logger;

    public WishlistService(IWishlistStorage storage, ICartService cartService, ILogger<WishlistService> logger)
    {
        _storage = storage;
        _cartService = cartService;
        _logger = logger;
    }

    public async Task<Wishlist> GetWishlistAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Recupero wishlist per utente {UserId}", userId);
        return await _storage.GetWishlistAsync(userId, ct);
    }

    public async Task<WishlistItem> AddToWishlistAsync(Guid userId, Guid productId, CancellationToken ct = default)
    {
        _logger.LogInformation("Aggiunta prodotto {ProductId} alla wishlist (Utente: {UserId})", productId, userId);
        
        var wishlist = await _storage.GetWishlistAsync(userId, ct);
        
        // Check if product already exists
        var existingItem = wishlist.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            return existingItem;
        }
        
        var newItem = new WishlistItem
        {
            UserId = userId,
            ProductId = productId
        };
        
        wishlist.Items.Add(newItem);
        wishlist.UpdatedAt = DateTime.UtcNow;
        await _storage.SaveWishlistAsync(wishlist, ct);
        
        return newItem;
    }

    public async Task<bool> RemoveFromWishlistAsync(Guid userId, Guid productId, CancellationToken ct = default)
    {
        _logger.LogInformation("Rimozione prodotto {ProductId} dalla wishlist (Utente: {UserId})", productId, userId);
        
        var wishlist = await _storage.GetWishlistAsync(userId, ct);
        var item = wishlist.Items.FirstOrDefault(i => i.ProductId == productId);
        
        if (item == null)
        {
            return false;
        }
        
        wishlist.Items.Remove(item);
        wishlist.UpdatedAt = DateTime.UtcNow;
        await _storage.SaveWishlistAsync(wishlist, ct);
        
        return true;
    }

    public async Task<bool> MoveToCartAsync(Guid userId, Guid productId, string sessionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Spostamento prodotto {ProductId} dalla wishlist al carrello", productId);
        
        var wishlist = await _storage.GetWishlistAsync(userId, ct);
        var item = wishlist.Items.FirstOrDefault(i => i.ProductId == productId);
        
        if (item == null)
        {
            return false;
        }
        
        // Add to cart (with default price 0, should be enriched by caller)
        await _cartService.AddToCartAsync(sessionId, productId, 1, 0, ct);
        
        // Remove from wishlist
        wishlist.Items.Remove(item);
        wishlist.UpdatedAt = DateTime.UtcNow;
        await _storage.SaveWishlistAsync(wishlist, ct);
        
        return true;
    }
}
