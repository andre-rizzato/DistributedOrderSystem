using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using CustomerWebsite.Models;

namespace CustomerWebsite.Services;

/// <summary>
/// Service for managing the shopping cart
/// </summary>
public interface IShoppingCartService
{
    Task<ShoppingCartModel> GetCartAsync(string sessionId);
    Task<bool> AddToCartAsync(string sessionId, AddToCartModel item);
    Task<bool> UpdateCartItemAsync(string sessionId, UpdateCartItemModel item);
    Task<bool> RemoveFromCartAsync(string sessionId, Guid cartItemId);
    Task<bool> ClearCartAsync(string sessionId);
    Task<decimal> GetCartTotalAsync(string sessionId);
    Task<int> GetCartItemCountAsync(string sessionId);
}

public class ShoppingCartService : IShoppingCartService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ShoppingCartService> _logger;
    private readonly IProductService _productService;
    private readonly string _cartServiceBaseUrl;

    public ShoppingCartService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ShoppingCartService> logger,
        IProductService productService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _productService = productService;
        _cartServiceBaseUrl = _configuration.GetValue<string>("Services:GatewayBff:BaseUrl") ??
                             "http://localhost:5189";
    }

    public async Task<ShoppingCartModel> GetCartAsync(string sessionId)
    {
        try
        {
            // GatewayBff.Contracts.CartDto/CartItemDto shape (Id, ProductId, Quantity, UnitPrice -
            // no CartItemId or AddedAt fields, unlike what this used to assume via `dynamic`,
            // which would have thrown a RuntimeBinderException on any successful response since
            // JsonElement doesn't support arbitrary dynamic member access at all).
            var url = $"{_cartServiceBaseUrl}/api/cart/{sessionId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new ShoppingCartModel();

            var cartData = await response.Content.ReadFromJsonAsync<GatewayCartDto>();
            if (cartData == null)
                return new ShoppingCartModel();

            var cart = new ShoppingCartModel();

            // Enrich each line with the product data the cart service doesn't store itself
            foreach (var item in cartData.Items)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);

                if (product != null)
                {
                    cart.Items.Add(new CartItemModel
                    {
                        CartItemId = item.Id,
                        Product = product,
                        Quantity = item.Quantity,
                        AddedAt = cartData.UpdatedAt
                    });
                }
            }

            // Compute shipping cost and taxes
            cart.ShippingCost = CalculateShippingCost(cart);
            cart.TaxAmount = CalculateTaxAmount(cart);

            return cart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cart for session {SessionId}", sessionId);
            return new ShoppingCartModel();
        }
    }

    // Local mirror of GatewayBff.Contracts.CartDto/CartItemDto - CustomerWebsite doesn't
    // reference GatewayBff's project, so this is duplicated rather than shared.
    private record GatewayCartDto(string SessionId, List<GatewayCartItemDto> Items, DateTime CreatedAt, DateTime UpdatedAt, decimal Total, int TotalItems);
    private record GatewayCartItemDto(Guid Id, Guid ProductId, int Quantity, decimal UnitPrice);

    public async Task<bool> AddToCartAsync(string sessionId, AddToCartModel item)
    {
        try
        {
            var url = $"{_cartServiceBaseUrl}/api/cart/{sessionId}/items";
            var response = await _httpClient.PostAsJsonAsync(url, new
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                VariantId = item.VariantId
            });

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart");
            return false;
        }
    }

    public async Task<bool> UpdateCartItemAsync(string sessionId, UpdateCartItemModel item)
    {
        try
        {
            if (item.Quantity <= 0)
            {
                return await RemoveFromCartAsync(sessionId, item.CartItemId);
            }

            var url = $"{_cartServiceBaseUrl}/api/cart/{sessionId}/items/{item.CartItemId}";
            var response = await _httpClient.PutAsJsonAsync(url, new { Quantity = item.Quantity });

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating the cart item");
            return false;
        }
    }

    public async Task<bool> RemoveFromCartAsync(string sessionId, Guid cartItemId)
    {
        try
        {
            var url = $"{_cartServiceBaseUrl}/api/cart/{sessionId}/items/{cartItemId}";
            var response = await _httpClient.DeleteAsync(url);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing the cart item");
            return false;
        }
    }

    public async Task<bool> ClearCartAsync(string sessionId)
    {
        try
        {
            var url = $"{_cartServiceBaseUrl}/api/cart/{sessionId}";
            var response = await _httpClient.DeleteAsync(url);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing the cart");
            return false;
        }
    }

    public async Task<decimal> GetCartTotalAsync(string sessionId)
    {
        var cart = await GetCartAsync(sessionId);
        return cart.Total;
    }

    public async Task<int> GetCartItemCountAsync(string sessionId)
    {
        var cart = await GetCartAsync(sessionId);
        return cart.TotalItems;
    }

    private decimal CalculateShippingCost(ShoppingCartModel cart)
    {
        // Free shipping for orders over €25 or for Prime products
        if (cart.IsEligibleForFreeShipping)
            return 0;

        // Standard shipping cost
        return 4.99m;
    }

    private decimal CalculateTaxAmount(ShoppingCartModel cart)
    {
        // Italian VAT at 22%
        return cart.Subtotal * 0.22m;
    }
}

/// <summary>
/// Service for managing the wishlist
/// </summary>
public interface IWishlistService
{
    Task<WishlistModel> GetWishlistAsync(Guid userId);
    Task<bool> AddToWishlistAsync(Guid userId, Guid productId, string? notes = null);
    Task<bool> RemoveFromWishlistAsync(Guid userId, Guid wishlistItemId);
    Task<bool> MoveToCartAsync(Guid userId, Guid wishlistItemId, string sessionId);
    Task<int> GetWishlistItemCountAsync(Guid userId);
}

public class WishlistService : IWishlistService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WishlistService> _logger;
    private readonly IProductService _productService;
    private readonly string _wishlistServiceBaseUrl;

    public WishlistService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WishlistService> logger,
        IProductService productService)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _productService = productService;
        _wishlistServiceBaseUrl = _configuration.GetValue<string>("Services:GatewayBff:BaseUrl") ??
                                  "http://localhost:5189";
    }

    public async Task<WishlistModel> GetWishlistAsync(Guid userId)
    {
        try
        {
            // GatewayBff.Contracts.WishlistDto/WishlistItemDto shape (Id, ProductId, CreatedAt -
            // no Notes field on the backend at all, unlike what this used to assume via
            // `dynamic`, which would have thrown a RuntimeBinderException on any successful
            // response since JsonElement doesn't support arbitrary dynamic member access).
            var url = $"{_wishlistServiceBaseUrl}/api/wishlist/{userId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return new WishlistModel();

            var wishlistData = await response.Content.ReadFromJsonAsync<GatewayWishlistDto>();
            if (wishlistData == null)
                return new WishlistModel();

            var wishlist = new WishlistModel();

            foreach (var item in wishlistData.Items)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);

                if (product != null)
                {
                    wishlist.Items.Add(new WishlistItemModel
                    {
                        WishlistItemId = item.Id,
                        Product = product,
                        AddedAt = item.CreatedAt
                    });
                }
            }

            return wishlist;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving wishlist for user {UserId}", userId);
            return new WishlistModel();
        }
    }

    // Local mirror of GatewayBff.Contracts.WishlistDto/WishlistItemDto - CustomerWebsite doesn't
    // reference GatewayBff's project, so this is duplicated rather than shared.
    private record GatewayWishlistDto(Guid UserId, List<GatewayWishlistItemDto> Items, DateTime CreatedAt, DateTime UpdatedAt);
    private record GatewayWishlistItemDto(Guid Id, Guid ProductId, DateTime CreatedAt);

    public async Task<bool> AddToWishlistAsync(Guid userId, Guid productId, string? notes = null)
    {
        try
        {
            var url = $"{_wishlistServiceBaseUrl}/api/wishlist/{userId}/items";
            var response = await _httpClient.PostAsJsonAsync(url, new
            {
                ProductId = productId,
                Notes = notes
            });

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to wishlist");
            return false;
        }
    }

    public async Task<bool> RemoveFromWishlistAsync(Guid userId, Guid wishlistItemId)
    {
        try
        {
            var url = $"{_wishlistServiceBaseUrl}/api/wishlist/{userId}/items/{wishlistItemId}";
            var response = await _httpClient.DeleteAsync(url);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from wishlist");
            return false;
        }
    }

    public async Task<bool> MoveToCartAsync(Guid userId, Guid wishlistItemId, string sessionId)
    {
        try
        {
            var url = $"{_wishlistServiceBaseUrl}/api/wishlist/{userId}/items/{wishlistItemId}/move-to-cart";
            var response = await _httpClient.PostAsJsonAsync(url, new { SessionId = sessionId });

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving to cart");
            return false;
        }
    }

    public async Task<int> GetWishlistItemCountAsync(Guid userId)
    {
        var wishlist = await GetWishlistAsync(userId);
        return wishlist.TotalItems;
    }
}
