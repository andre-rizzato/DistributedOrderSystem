using Microsoft.AspNetCore.Mvc;
using CustomerWebsite.Models;
using CustomerWebsite.Services;

namespace CustomerWebsite.Controllers;

/// <summary>
/// Controller per la gestione del carrello della spesa
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
    /// Visualizza il carrello della spesa
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            var cart = await _cartService.GetCartAsync(sessionId);
            
            // Aggiungi prodotti raccomandati basati sul contenuto del carrello
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
            _logger.LogError(ex, "Errore durante il caricamento del carrello");
            return View(new ShoppingCartModel());
        }
    }

    /// <summary>
    /// Aggiunge un elemento al carrello
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
                    message = "Prodotto aggiunto al carrello"
                });
            }

            return Json(new { success = false, message = "Errore durante l'aggiunta al carrello" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiunta al carrello");
            return Json(new { success = false, message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Aggiorna la quantità di un elemento del carrello
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

            return Json(new { success = false, message = "Errore durante l'aggiornamento" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiornamento della quantità");
            return Json(new { success = false, message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Rimuove un elemento dal carrello
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
                    message = "Prodotto rimosso dal carrello"
                });
            }

            return Json(new { success = false, message = "Errore durante la rimozione" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la rimozione dal carrello");
            return Json(new { success = false, message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Svuota il carrello
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
                return Json(new { success = true, message = "Carrello svuotato" });
            }

            return Json(new { success = false, message = "Errore durante lo svuotamento del carrello" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante lo svuotamento del carrello");
            return Json(new { success = false, message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottiene il numero di elementi nel carrello (per AJAX)
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
            _logger.LogError(ex, "Errore durante il recupero del conteggio del carrello");
            return Json(new { count = 0 });
        }
    }

    /// <summary>
    /// Ottiene un riepilogo del carrello (per la mini-cart)
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
            _logger.LogError(ex, "Errore durante il recupero del riepilogo del carrello");
            return Json(new { itemCount = 0, subtotal = 0, total = 0, items = new List<object>() });
        }
    }

    /// <summary>
    /// Procede al checkout
    /// </summary>
    public async Task<IActionResult> Checkout()
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            var cart = await _cartService.GetCartAsync(sessionId);

            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Il tuo carrello è vuoto.";
                return RedirectToAction(nameof(Index));
            }

            // Reindirizza al controller Checkout
            return RedirectToAction("Index", "Checkout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'avvio del checkout");
            TempData["ErrorMessage"] = "Si è verificato un errore. Riprova.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Salva il carrello per dopo (utenti registrati)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveForLater()
    {
        try
        {
            // Implementazione per utenti registrati
            // Per ora ritorna solo un messaggio di successo
            return Json(new { success = true, message = "Carrello salvato per dopo" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il salvataggio del carrello");
            return Json(new { success = false, message = "Errore interno del server" });
        }
    }
}