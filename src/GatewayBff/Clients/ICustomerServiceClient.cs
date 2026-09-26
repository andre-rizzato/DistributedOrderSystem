namespace GatewayBff.Clients;

using GatewayBff.Contracts;

public interface ICustomerServiceClient
{
    Task<CartDto?> GetCartAsync(string sessionId, CancellationToken ct);
    Task<CartItemDto?> AddCartItemAsync(string sessionId, Guid productId, int quantity, decimal unitPrice, CancellationToken ct);
    Task<CartItemDto?> UpdateCartItemAsync(string sessionId, Guid cartItemId, int quantity, CancellationToken ct);
    Task<bool> RemoveCartItemAsync(string sessionId, Guid cartItemId, CancellationToken ct);
    Task<bool> ClearCartAsync(string sessionId, CancellationToken ct);

    Task<WishlistDto?> GetWishlistAsync(Guid userId, CancellationToken ct);
    Task<WishlistItemDto?> AddWishlistItemAsync(Guid userId, Guid productId, CancellationToken ct);
    Task<bool> RemoveWishlistItemAsync(Guid userId, Guid productId, CancellationToken ct);
    Task<bool> MoveWishlistItemToCartAsync(Guid userId, Guid productId, string sessionId, CancellationToken ct);
}
