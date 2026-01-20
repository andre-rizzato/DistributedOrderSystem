namespace CustomerService.Services.Interfaces;

using CustomerService.Models;

public interface IWishlistService
{
    Task<Wishlist> GetWishlistAsync(Guid userId, CancellationToken ct = default);
    Task<WishlistItem> AddToWishlistAsync(Guid userId, Guid productId, CancellationToken ct = default);
    Task<bool> RemoveFromWishlistAsync(Guid userId, Guid productId, CancellationToken ct = default);
    Task<bool> MoveToCartAsync(Guid userId, Guid productId, string sessionId, CancellationToken ct = default);
}
