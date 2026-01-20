namespace CustomerService.Storage;

using CustomerService.Models;

public interface IWishlistStorage
{
    Task<Wishlist> GetWishlistAsync(Guid userId, CancellationToken ct = default);
    Task SaveWishlistAsync(Wishlist wishlist, CancellationToken ct = default);
    Task DeleteWishlistAsync(Guid userId, CancellationToken ct = default);
}
