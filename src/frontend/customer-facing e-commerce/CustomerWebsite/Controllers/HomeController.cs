using Microsoft.AspNetCore.Mvc;
using CustomerWebsite.Models;
using CustomerWebsite.Services;

namespace CustomerWebsite.Controllers;

/// <summary>
/// Main controller for the homepage and ShopVerse's base functionality.
/// Handles the main page display, product search, categories, and basic navigation.
/// </summary>
public class HomeController : Controller
{
    // Dependency-injected services for data access and business functionality
    private readonly IProductService _productService;      // Service for product catalog management
    private readonly IShoppingCartService _cartService;    // Service for shopping cart management
    private readonly ILogger<HomeController> _logger;      // Logger for diagnostics and monitoring

    /// <summary>
    /// Constructor with dependency injection to initialize the required services
    /// </summary>
    /// <param name="productService">Service for product operations (catalog, search, categories)</param>
    /// <param name="cartService">Service for cart and wishlist management</param>
    /// <param name="logger">Logger for event and error tracking</param>
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
    /// Main action for the ShopVerse homepage.
    /// Loads and displays: featured products, categories, promotional banners, and cart data.
    /// </summary>
    /// <returns>Homepage view with the full data model for the e-commerce experience</returns>
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

            // Add the number of items in the cart
            var sessionId = HttpContext.Session.Id;
            ViewBag.CartItemCount = await _cartService.GetCartItemCountAsync(sessionId);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading the homepage");
            return View(new HomePageViewModel());
        }
    }

    /// <summary>
    /// Product search page
    /// </summary>
    public async Task<IActionResult> Search(ProductSearchModel searchModel)
    {
        try
        {
            if (searchModel == null)
                searchModel = new ProductSearchModel();

            var results = await _productService.SearchProductsAsync(searchModel);

            // Load the filter options
            ViewBag.Categories = await _productService.GetCategoriesAsync();
            ViewBag.Brands = await _productService.GetBrandsAsync();
            ViewBag.CartItemCount = await _cartService.GetCartItemCountAsync(HttpContext.Session.Id);

            return View(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return View(new ProductSearchResultModel { SearchCriteria = searchModel });
        }
    }

    /// <summary>
    /// Categories page
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
            _logger.LogError(ex, "Error loading categories");
            return View(new List<CategoryModel>());
        }
    }

    /// <summary>
    /// Products-by-category page
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
            _logger.LogError(ex, "Error loading category {CategoryName}", categoryName);
            return RedirectToAction(nameof(Categories));
        }
    }

    /// <summary>
    /// About page
    /// </summary>
    public IActionResult About()
    {
        return View();
    }

    /// <summary>
    /// Contact page
    /// </summary>
    public IActionResult Contact()
    {
        return View();
    }

    /// <summary>
    /// Customer service page
    /// </summary>
    public IActionResult CustomerService()
    {
        return View();
    }

    /// <summary>
    /// Error handling
    /// </summary>
    [Route("Error/{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        var model = new ErrorViewModel
        {
            StatusCode = statusCode,
            Message = statusCode switch
            {
                404 => "Page not found",
                500 => "Internal server error",
                _ => "An error occurred"
            }
        };

        return View("Error", model);
    }

    /// <summary>
    /// Autocomplete search endpoint
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
            _logger.LogError(ex, "Error during autocomplete");
            return Json(new List<string>());
        }
    }
}

/// <summary>
/// ViewModel for the homepage
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
/// ViewModel for the category page
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
/// Model for promotional banners
/// </summary>
public class BannerModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string LinkUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
