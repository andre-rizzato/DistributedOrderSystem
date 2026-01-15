using Microsoft.AspNetCore.Mvc;
using CustomerWebsite.Models;
using CustomerWebsite.Services;

namespace CustomerWebsite.Controllers;

/// <summary>
/// Controller per la gestione dei prodotti
/// </summary>
public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly IShoppingCartService _cartService;
    private readonly IWishlistService _wishlistService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(
        IProductService productService,
        IShoppingCartService cartService,
        IWishlistService wishlistService,
        ILogger<ProductController> logger)
    {
        _productService = productService;
        _cartService = cartService;
        _wishlistService = wishlistService;
        _logger = logger;
    }

    /// <summary>
    /// Pagina di dettaglio del prodotto
    /// </summary>
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = await _productService.GetRelatedProductsAsync(id),
                Reviews = await _productService.GetProductReviewsAsync(id, 1, 5)
            };

            ViewBag.CartItemCount = await _cartService.GetCartItemCountAsync(HttpContext.Session.Id);
            
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il caricamento del prodotto {ProductId}", id);
            return NotFound();
        }
    }

    /// <summary>
    /// Aggiunge un prodotto al carrello
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartModel model)
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
                return Json(new { success = true, cartItemCount = cartItemCount });
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
    /// Aggiunge un prodotto alla wishlist
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddToWishlist(Guid productId, string? notes = null)
    {
        try
        {
            // In un'implementazione reale, dovresti ottenere l'ID utente dal sistema di autenticazione
            var userId = Guid.NewGuid(); // Placeholder per l'ID utente
            
            var success = await _wishlistService.AddToWishlistAsync(userId, productId, notes);

            if (success)
            {
                return Json(new { success = true, message = "Prodotto aggiunto alla wishlist" });
            }

            return Json(new { success = false, message = "Errore durante l'aggiunta alla wishlist" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiunta alla wishlist");
            return Json(new { success = false, message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Carica le recensioni di un prodotto via AJAX
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> LoadReviews(Guid productId, int page = 1)
    {
        try
        {
            var reviews = await _productService.GetProductReviewsAsync(productId, page, 5);
            return PartialView("_ProductReviews", reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il caricamento delle recensioni");
            return PartialView("_ProductReviews", new List<ProductReviewModel>());
        }
    }

    /// <summary>
    /// Aggiunge una recensione
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddReview([FromBody] AddReviewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // In un'implementazione reale, dovresti ottenere l'ID utente dal sistema di autenticazione
            var userId = Guid.NewGuid(); // Placeholder per l'ID utente
            
            var success = await _productService.AddReviewAsync(model, userId);

            if (success)
            {
                return Json(new { success = true, message = "Recensione aggiunta con successo" });
            }

            return Json(new { success = false, message = "Errore durante l'aggiunta della recensione" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiunta della recensione");
            return Json(new { success = false, message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Confronta prodotti
    /// </summary>
    public async Task<IActionResult> Compare(List<Guid> productIds)
    {
        try
        {
            if (productIds == null || productIds.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            var products = new List<ProductDisplayModel>();
            foreach (var productId in productIds.Take(4)) // Massimo 4 prodotti
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product != null)
                {
                    products.Add(product);
                }
            }

            var model = new ProductComparisonModel
            {
                Products = products
            };

            ViewBag.CartItemCount = await _cartService.GetCartItemCountAsync(HttpContext.Session.Id);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il confronto prodotti");
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// Ricerca rapida prodotti (autocomplete)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> QuickSearch(string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Json(new List<object>());
            }

            var searchModel = new ProductSearchModel
            {
                SearchTerm = term,
                PageSize = 8
            };

            var results = await _productService.SearchProductsAsync(searchModel);
            var suggestions = results.Products.Select(p => new
            {
                id = p.ProductId,
                name = p.Name,
                price = p.FinalPrice,
                image = p.Images.FirstOrDefault()?.Url ?? "/images/no-image.png",
                category = p.Category
            }).ToList();

            return Json(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la ricerca rapida");
            return Json(new List<object>());
        }
    }
}

/// <summary>
/// ViewModel per la pagina di dettaglio del prodotto
/// </summary>
public class ProductDetailViewModel
{
    public ProductDisplayModel Product { get; set; } = new();
    public List<ProductDisplayModel> RelatedProducts { get; set; } = new();
    public List<ProductReviewModel> Reviews { get; set; } = new();
    public AddToCartModel AddToCartModel { get; set; } = new();
    public AddReviewModel ReviewModel { get; set; } = new();
    public bool IsInWishlist { get; set; }
    public int SelectedQuantity { get; set; } = 1;
}