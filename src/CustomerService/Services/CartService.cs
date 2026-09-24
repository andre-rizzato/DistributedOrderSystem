namespace CustomerService.Services;

using CustomerService.Models;
using CustomerService.Services.Interfaces;
using CustomerService.Storage;

public class CartService : ICartService
{
    private readonly ICartStorage _storage;
    private readonly ILogger<CartService> _logger;

    public CartService(ICartStorage storage, ILogger<CartService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public async Task<Cart> GetCartAsync(string sessionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Retrieving cart for session {SessionId}", sessionId);
        return await _storage.GetCartAsync(sessionId, ct);
    }

    public async Task<CartItem> AddToCartAsync(string sessionId, Guid productId, int quantity, decimal unitPrice, CancellationToken ct = default)
    {
        _logger.LogInformation("Adding product {ProductId} to cart (Session: {SessionId})", productId, sessionId);
        
        var cart = await _storage.GetCartAsync(sessionId, ct);
        
        // Check if product already exists in cart
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
            existingItem.UpdatedAt = DateTime.UtcNow;
            cart.UpdatedAt = DateTime.UtcNow;
            await _storage.SaveCartAsync(cart, ct);
            return existingItem;
        }
        
        // Add new item
        var newItem = new CartItem
        {
            SessionId = sessionId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
        
        cart.Items.Add(newItem);
        cart.UpdatedAt = DateTime.UtcNow;
        await _storage.SaveCartAsync(cart, ct);
        
        return newItem;
    }

    public async Task<CartItem?> UpdateCartItemAsync(string sessionId, Guid cartItemId, int quantity, CancellationToken ct = default)
    {
        _logger.LogInformation("Updating item {CartItemId} in cart (Session: {SessionId})", cartItemId, sessionId);
        
        var cart = await _storage.GetCartAsync(sessionId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        
        if (item == null)
        {
            _logger.LogWarning("Item {CartItemId} not found in cart", cartItemId);
            return null;
        }
        
        if (quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            item.Quantity = quantity;
            item.UpdatedAt = DateTime.UtcNow;
        }
        
        cart.UpdatedAt = DateTime.UtcNow;
        await _storage.SaveCartAsync(cart, ct);
        
        return item;
    }

    public async Task<bool> RemoveFromCartAsync(string sessionId, Guid cartItemId, CancellationToken ct = default)
    {
        _logger.LogInformation("Removing item {CartItemId} from cart (Session: {SessionId})", cartItemId, sessionId);
        
        var cart = await _storage.GetCartAsync(sessionId, ct);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        
        if (item == null)
        {
            return false;
        }
        
        cart.Items.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await _storage.SaveCartAsync(cart, ct);
        
        return true;
    }

    public async Task<bool> ClearCartAsync(string sessionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Clearing cart (Session: {SessionId})", sessionId);
        
        var cart = new Cart { SessionId = sessionId };
        await _storage.SaveCartAsync(cart, ct);
        
        return true;
    }
}
