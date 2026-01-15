using Microsoft.AspNetCore.Mvc;
using CustomerWebsite.Models;
using CustomerWebsite.Services;

namespace CustomerWebsite.Controllers;

/// <summary>
/// Controller principale per la gestione dell'homepage e delle funzionalità base di ShopVerse
/// Gestisce la visualizzazione della pagina principale, ricerca prodotti, categorie e navigazione base
/// </summary>
public class HomeController : Controller
{
    // Servizi dependency injected per accesso ai dati e funzionalità business
    private readonly IProductService _productService;      // Servizio per gestione catalogo prodotti
    private readonly IShoppingCartService _cartService;    // Servizio per gestione carrello della spesa
    private readonly ILogger<HomeController> _logger;      // Logger per diagnostica e monitoraggio

    /// <summary>
    /// Costruttore con dependency injection per inizializzare i servizi necessari
    /// </summary>
    /// <param name="productService">Servizio per operazioni sui prodotti (catalogo, ricerca, categorie)</param>
    /// <param name="cartService">Servizio per gestione carrello e wishlist</param>
    /// <param name="logger">Logger per tracciamento eventi e errori</param>
    public HomeController(
        IProductService productService, 
        IShoppingCartService cartService,
        ILogger<HomeController> logger)
    {
        _productService = productService;
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// Action principale per la homepage di ShopVerse
    /// Carica e visualizza: prodotti in evidenza, categorie, banner promozionali e dati carrello
    /// </summary>
    /// <returns>Vista homepage con modello dati completo per l'esperienza e-commerce</returns>
    public async Task<IActionResult> Index()
    {
        try
        {
            var model = new HomePageViewModel
            {
                FeaturedProducts = await _productService.GetFeaturedProductsAsync(8),
                BestSellers = await _productService.GetBestSellersAsync(6),
                Categories = await _productService.GetCategoriesAsync(),
                RecommendedProducts = await _productService.GetRecommendedProductsAsync(count: 10)
            };

            // Aggiungi il numero di elementi nel carrello
            var sessionId = HttpContext.Session.Id;
            ViewBag.CartItemCount = await _cartService.GetCartItemCountAsync(sessionId);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il caricamento della homepage");
            return View(new HomePageViewModel());
        }
    }

    /// <summary>
    /// Pagina di ricerca prodotti
    /// </summary>
    public async Task<IActionResult> Search(ProductSearchModel searchModel)
    {
        try
        {
            if (searchModel == null)
                searchModel = new ProductSearchModel();

            var results = await _productService.SearchProductsAsync(searchModel);
            
            // Carica le opzioni per i filtri
            ViewBag.Categories = await _productService.GetCategoriesAsync();
            ViewBag.Brands = await _productService.GetBrandsAsync();
            ViewBag.CartItemCount = await _cartService.GetCartItemCountAsync(HttpContext.Session.Id);

            return View(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la ricerca prodotti");
            return View(new ProductSearchResultModel { SearchCriteria = searchModel });
        }
    }

    /// <summary>
    /// Pagina delle categorie
    /// </summary>
    public async Task<IActionResult> Categories()
    {
        try
        {
            var categories = await _productService.GetCategoriesAsync();
            ViewBag.CartItemCount = await _cartService.GetCartItemCountAsync(HttpContext.Session.Id);
            
            return View(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il caricamento delle categorie");
            return View(new List<CategoryModel>());
        }
    }

    /// <summary>
    /// Pagina dei prodotti per categoria
    /// </summary>
    public async Task<IActionResult> Category(string categoryName, int page = 1)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return RedirectToAction(nameof(Categories));

            var products = await _productService.GetProductsByCategoryAsync(categoryName, page);
            var categories = await _productService.GetCategoriesAsync();
            var currentCategory = categories.FirstOrDefault(c => 
                string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));

            var model = new CategoryPageViewModel
            {
                Category = currentCategory ?? new CategoryModel { Name = categoryName, DisplayName = categoryName },
                Products = products,
                CurrentPage = page,
                AllCategories = categories
            };

            ViewBag.CartItemCount = await _cartService.GetCartItemCountAsync(HttpContext.Session.Id);
            
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il caricamento della categoria {CategoryName}", categoryName);
            return RedirectToAction(nameof(Categories));
        }
    }

    /// <summary>
    /// Pagina di informazioni
    /// </summary>
    public IActionResult About()
    {
        return View();
    }

    /// <summary>
    /// Pagina dei contatti
    /// </summary>
    public IActionResult Contact()
    {
        return View();
    }

    /// <summary>
    /// Pagina dell'assistenza clienti
    /// </summary>
    public IActionResult CustomerService()
    {
        return View();
    }

    /// <summary>
    /// Gestione degli errori
    /// </summary>
    [Route("Error/{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        var model = new ErrorViewModel
        {
            StatusCode = statusCode,
            Message = statusCode switch
            {
                404 => "Pagina non trovata",
                500 => "Errore interno del server",
                _ => "Si è verificato un errore"
            }
        };

        return View("Error", model);
    }

    /// <summary>
    /// Endpoint per la ricerca autocomplete
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Autocomplete(string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return Json(new List<string>());

            var searchModel = new ProductSearchModel
            {
                SearchTerm = term,
                PageSize = 5
            };

            var results = await _productService.SearchProductsAsync(searchModel);
            var suggestions = results.Products
                .Select(p => new { label = p.Name, value = p.Name, id = p.ProductId })
                .ToList();

            return Json(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'autocomplete");
            return Json(new List<string>());
        }
    }
}

/// <summary>
/// ViewModel per la homepage
/// </summary>
public class HomePageViewModel
{
    public List<ProductDisplayModel> FeaturedProducts { get; set; } = new();
    public List<ProductDisplayModel> BestSellers { get; set; } = new();
    public List<ProductDisplayModel> RecommendedProducts { get; set; } = new();
    public List<CategoryModel> Categories { get; set; } = new();
    public List<BannerModel> PromotionalBanners { get; set; } = new();
}

/// <summary>
/// ViewModel per la pagina categoria
/// </summary>
public class CategoryPageViewModel
{
    public CategoryModel Category { get; set; } = new();
    public List<ProductDisplayModel> Products { get; set; } = new();
    public List<CategoryModel> AllCategories { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}

/// <summary>
/// Modello per i banner promozionali
/// </summary>
public class BannerModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
