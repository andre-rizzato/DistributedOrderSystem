using Microsoft.AspNetCore.Mvc;
using CustomerWebsite.Models;
using CustomerWebsite.Services;

namespace CustomerWebsite.Controllers;

/// <summary>
/// Controller for managing the shopping cart
/// </summary>
public class CartController : Controller
{
    private readonly IShoppingCartService _cartService;
    private readonly IProductService _productService;
    private readonly ILogger<CartController> _logger;

    public CartController(
        IShoppingCartService cartService,
        IProductService productService,
        ILogger<CartController> logger)
    {
        _cartService = cartService;
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Displays the shopping cart
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            var cart = await _cartService.GetCartAsync(sessionId);

            // Add recommended products based on the cart's contents
            if (cart.Items.Any())
            {
                var firstProductId = cart.Items.First().Product.ProductId;
                ViewBag.RecommendedProducts = await _productService.GetRelatedProductsAsync(firstProductId, 4);
            }
            else
            {
                ViewBag.RecommendedProducts = await _productService.GetFeaturedProductsAsync(4);
            }

            return View(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading the cart");
            return View(new ShoppingCartModel());
        }
    }

    /// <summary>
    /// Adds an item to the cart
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddItem([FromBody] AddToCartModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sessionId = HttpContext.Session.Id;
            var success = await _cartService.AddToCartAsync(sessionId, model);

            if (success)
            {
                var cartItemCount = await _cartService.GetCartItemCountAsync(sessionId);
                var cartTotal = await _cartService.GetCartTotalAsync(sessionId);

                return Json(new
                {
                    success = true,
                    cartItemCount = cartItemCount,
                    cartTotal = cartTotal,
                    message = "Product added to cart"
                });
            }

            return Json(new { success = false, message = "Error adding to cart" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart");
            return Json(new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates the quantity of a cart item
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartItemModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var sessionId = HttpContext.Session.Id;
            var success = await _cartService.UpdateCartItemAsync(sessionId, model);

            if (success)
            {
                var cart = await _cartService.GetCartAsync(sessionId);
                var updatedItem = cart.Items.FirstOrDefault(i => i.CartItemId == model.CartItemId);

                return Json(new
                {
                    success = true,
                    itemTotal = updatedItem?.TotalPrice ?? 0,
                    cartSubtotal = cart.Subtotal,
                    cartTotal = cart.Total,
                    cartItemCount = cart.TotalItems
                });
            }

            return Json(new { success = false, message = "Error updating the item" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating the quantity");
            return Json(new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Removes an item from the cart
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RemoveItem(Guid cartItemId)
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            var success = await _cartService.RemoveFromCartAsync(sessionId, cartItemId);

            if (success)
            {
                var cart = await _cartService.GetCartAsync(sessionId);

                return Json(new
                {
                    success = true,
                    cartSubtotal = cart.Subtotal,
                    cartTotal = cart.Total,
                    cartItemCount = cart.TotalItems,
                    message = "Product removed from cart"
                });
            }

            return Json(new { success = false, message = "Error removing the item" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart");
            return Json(new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Empties the cart
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            var success = await _cartService.ClearCartAsync(sessionId);

            if (success)
            {
                return Json(new { success = true, message = "Cart emptied" });
            }

            return Json(new { success = false, message = "Error emptying the cart" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emptying the cart");
            return Json(new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets the number of items in the cart (for AJAX)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCartCount()
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            var count = await _cartService.GetCartItemCountAsync(sessionId);
            return Json(new { count = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving the cart count");
            return Json(new { count = 0 });
        }
    }

    /// <summary>
    /// Gets a cart summary (for the mini-cart)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCartSummary()
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            var cart = await _cartService.GetCartAsync(sessionId);

            var summary = new
            {
                itemCount = cart.TotalItems,
                subtotal = cart.Subtotal,
                total = cart.Total,
                items = cart.Items.Take(5).Select(item => new
                {
                    name = item.Product.Name,
                    quantity = item.Quantity,
                    price = item.UnitPrice,
                    total = item.TotalPrice,
                    image = item.Product.Images.FirstOrDefault()?.Url ?? "/images/no-image.png"
                }).ToList()
            };

            return Json(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving the cart summary");
            return Json(new { itemCount = 0, subtotal = 0, total = 0, items = new List<object>() });
        }
    }

    /// <summary>
    /// Proceeds to checkout
    /// </summary>
    public async Task<IActionResult> Checkout()
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            var cart = await _cartService.GetCartAsync(sessionId);

            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction(nameof(Index));
            }

            // Redirect to the Checkout controller
            return RedirectToAction("Index", "Checkout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting checkout");
            TempData["ErrorMessage"] = "An error occurred. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Saves the cart for later (registered users)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveForLater()
    {
        try
        {
            // Implementation for registered users
            // For now, just return a success message
            return Json(new { success = true, message = "Cart saved for later" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving the cart");
            return Json(new { success = false, message = "Internal server error" });
        }
    }
}
