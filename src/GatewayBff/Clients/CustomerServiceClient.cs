namespace GatewayBff.Clients;

using System.Net.Http.Json;
using GatewayBff.Contracts;

public class CustomerServiceClient : ICustomerServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<CustomerServiceClient> _logger;

    public CustomerServiceClient(HttpClient http, ILogger<CustomerServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<CartDto?> GetCartAsync(string sessionId, CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync($"api/cart/{sessionId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get cart for session {SessionId}: {StatusCode}", sessionId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CartDto>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart for session {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<CartItemDto?> AddCartItemAsync(string sessionId, Guid productId, int quantity, decimal unitPrice, CancellationToken ct)
    {
        try
        {
            var payload = new { ProductId = productId, Quantity = quantity, UnitPrice = unitPrice };
            var response = await _http.PostAsJsonAsync($"api/cart/{sessionId}/items", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to add to cart: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CartItemDto>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart");
            return null;
        }
    }

    public async Task<CartItemDto?> UpdateCartItemAsync(string sessionId, Guid cartItemId, int quantity, CancellationToken ct)
    {
        try
        {
            var payload = new { Quantity = quantity };
            var response = await _http.PutAsJsonAsync($"api/cart/{sessionId}/items/{cartItemId}", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to update cart item: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CartItemDto>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item");
            return null;
        }
    }

    public async Task<bool> RemoveCartItemAsync(string sessionId, Guid cartItemId, CancellationToken ct)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/cart/{sessionId}/items/{cartItemId}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cart");
            return false;
        }
    }

    public async Task<bool> ClearCartAsync(string sessionId, CancellationToken ct)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/cart/{sessionId}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return false;
        }
    }

    public async Task<WishlistDto?> GetWishlistAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync($"api/wishlist/{userId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get wishlist for user {UserId}: {StatusCode}", userId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<WishlistDto>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wishlist for user {UserId}", userId);
            return null;
        }
    }

    public async Task<WishlistItemDto?> AddWishlistItemAsync(Guid userId, Guid productId, CancellationToken ct)
    {
        try
        {
            var payload = new { ProductId = productId };
            var response = await _http.PostAsJsonAsync($"api/wishlist/{userId}/items", payload, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to add to wishlist: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<WishlistItemDto>(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to wishlist");
            return null;
        }
    }

    public async Task<bool> RemoveWishlistItemAsync(Guid userId, Guid productId, CancellationToken ct)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/wishlist/{userId}/items/{productId}", ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from wishlist");
            return false;
        }
    }

    public async Task<bool> MoveWishlistItemToCartAsync(Guid userId, Guid productId, string sessionId, CancellationToken ct)
    {
        try
        {
            var payload = new { SessionId = sessionId };
            var response = await _http.PostAsJsonAsync($"api/wishlist/{userId}/items/{productId}/move-to-cart", payload, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving wishlist item to cart");
            return false;
        }
    }
}
