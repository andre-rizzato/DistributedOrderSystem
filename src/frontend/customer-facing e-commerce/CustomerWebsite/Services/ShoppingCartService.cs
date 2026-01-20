using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using CustomerWebsite.Models;

namespace CustomerWebsite.Services;

/// <summary>
/// Servizio per la gestione del carrello della spesa
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
                             "https://localhost:7000";
    }

    public async Task<ShoppingCartModel> GetCartAsync(string sessionId)
    {
        try
        {
            var url = $"{_cartServiceBaseUrl}/api/cart/{sessionId}";
            var cartData = await _httpClient.GetFromJsonAsync<dynamic>(url);
            
            if (cartData == null)
                return new ShoppingCartModel();

            var cart = new ShoppingCartModel();
            
            // Deserializza gli elementi del carrello e arricchisci con i dati del prodotto
            if (cartData.items != null)
            {
                foreach (var item in cartData.items)
                {
                    var productId = Guid.Parse(item.productId.ToString());
                    var product = await _productService.GetProductByIdAsync(productId);
                    
                    if (product != null)
                    {
                        cart.Items.Add(new CartItemModel
                        {
                            CartItemId = Guid.Parse(item.cartItemId.ToString()),
                            Product = product,
                            Quantity = (int)item.quantity,
                            AddedAt = DateTime.Parse(item.addedAt.ToString())
                        });
                    }
                }
            }

            // Calcola i costi di spedizione e tasse
            cart.ShippingCost = CalculateShippingCost(cart);
            cart.TaxAmount = CalculateTaxAmount(cart);

            return cart;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del carrello per sessione {SessionId}", sessionId);
            return new ShoppingCartModel();
        }
    }

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
            _logger.LogError(ex, "Errore durante l'aggiunta al carrello");
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
            _logger.LogError(ex, "Errore durante l'aggiornamento dell'elemento del carrello");
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
            _logger.LogError(ex, "Errore durante la rimozione dall'elemento del carrello");
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
            _logger.LogError(ex, "Errore durante la pulizia del carrello");
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
        // Spedizione gratuita per ordini superiori a €25 o per prodotti Prime
        if (cart.IsEligibleForFreeShipping)
            return 0;

        // Costo di spedizione standard
        return 4.99m;
    }

    private decimal CalculateTaxAmount(ShoppingCartModel cart)
    {
        // IVA italiana al 22%
        return cart.Subtotal * 0.22m;
    }
}

/// <summary>
/// Servizio per la gestione della wishlist
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
                                  "https://localhost:7000";
    }

    public async Task<WishlistModel> GetWishlistAsync(Guid userId)
    {
        try
        {
            var url = $"{_wishlistServiceBaseUrl}/api/wishlist/{userId}";
            var wishlistData = await _httpClient.GetFromJsonAsync<dynamic>(url);
            
            if (wishlistData == null)
                return new WishlistModel();

            var wishlist = new WishlistModel();
            
            if (wishlistData.items != null)
            {
                foreach (var item in wishlistData.items)
                {
                    var productId = Guid.Parse(item.productId.ToString());
                    var product = await _productService.GetProductByIdAsync(productId);
                    
                    if (product != null)
                    {
                        wishlist.Items.Add(new WishlistItemModel
                        {
                            WishlistItemId = Guid.Parse(item.wishlistItemId.ToString()),
                            Product = product,
                            AddedAt = DateTime.Parse(item.addedAt.ToString()),
                            Notes = item.notes?.ToString()
                        });
                    }
                }
            }

            return wishlist;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero della wishlist per utente {UserId}", userId);
            return new WishlistModel();
        }
    }

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
            _logger.LogError(ex, "Errore durante l'aggiunta alla wishlist");
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
            _logger.LogError(ex, "Errore durante la rimozione dalla wishlist");
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
            _logger.LogError(ex, "Errore durante lo spostamento nel carrello");
            return false;
        }
    }

    public async Task<int> GetWishlistItemCountAsync(Guid userId)
    {
        var wishlist = await GetWishlistAsync(userId);
        return wishlist.TotalItems;
    }
}