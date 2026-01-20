namespace CustomerService.Services.Interfaces;

using CustomerService.Models;

public interface ICartService
{
    Task<Cart> GetCartAsync(string sessionId, CancellationToken ct = default);
    Task<CartItem> AddToCartAsync(string sessionId, Guid productId, int quantity, decimal unitPrice, CancellationToken ct = default);
    Task<CartItem?> UpdateCartItemAsync(string sessionId, Guid cartItemId, int quantity, CancellationToken ct = default);
    Task<bool> RemoveFromCartAsync(string sessionId, Guid cartItemId, CancellationToken ct = default);
    Task<bool> ClearCartAsync(string sessionId, CancellationToken ct = default);
}
