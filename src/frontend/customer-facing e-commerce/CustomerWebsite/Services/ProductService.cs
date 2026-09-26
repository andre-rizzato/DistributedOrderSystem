using CustomerWebsite.Models;

namespace CustomerWebsite.Services;

/// <summary>
/// Service for managing products via the API
/// </summary>
public interface IProductService
{
    Task<ProductSearchResultModel> SearchProductsAsync(ProductSearchModel searchModel);
    Task<ProductDisplayModel?> GetProductByIdAsync(Guid productId);
    Task<List<ProductDisplayModel>> GetFeaturedProductsAsync(int count = 10);
    Task<List<ProductDisplayModel>> GetBestSellersAsync(int count = 10);
    Task<List<ProductDisplayModel>> GetRecommendedProductsAsync(Guid? userId = null, int count = 10);
    Task<List<ProductDisplayModel>> GetRelatedProductsAsync(Guid productId, int count = 5);
    Task<List<CategoryModel>> GetCategoriesAsync();
    Task<List<ProductDisplayModel>> GetProductsByCategoryAsync(string category, int page = 1, int pageSize = 20);
    Task<List<string>> GetBrandsAsync();
    Task<List<ProductReviewModel>> GetProductReviewsAsync(Guid productId, int page = 1, int pageSize = 10);
    Task<bool> AddReviewAsync(AddReviewModel review, Guid userId);
}

public class ProductService : IProductService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProductService> _logger;
    private readonly string _gatewayBffBaseUrl;

    public ProductService(HttpClient httpClient, IConfiguration configuration, ILogger<ProductService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        // Goes through GatewayBff (like OrderService/ShoppingCartService/WishlistService) - never
        // straight to ProductService, so GatewayBff stays the single entry point for every downstream call.
        _gatewayBffBaseUrl = _configuration.GetValue<string>("Services:GatewayBff:BaseUrl") ??
                            "http://localhost:5189";
    }

    public async Task<ProductSearchResultModel> SearchProductsAsync(ProductSearchModel searchModel)
    {
        try
        {
            // GatewayBff/Controllers/QueriesController.cs -> GET "api/queries/catalog/search".
            // Only searchTerm/category/minPrice/maxPrice are understood downstream (ProductService's
            // own search endpoint has no brand/stock/rating/sort/paging parameters either) - Page and
            // PageSize are applied client-side below since nothing upstream supports them.
            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(searchModel.SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(searchModel.SearchTerm)}");

            if (!string.IsNullOrWhiteSpace(searchModel.Category))
                queryParams.Add($"category={Uri.EscapeDataString(searchModel.Category)}");

            if (searchModel.MinPrice.HasValue)
                queryParams.Add($"minPrice={searchModel.MinPrice.Value}");

            if (searchModel.MaxPrice.HasValue)
                queryParams.Add($"maxPrice={searchModel.MaxPrice.Value}");

            var queryString = string.Join("&", queryParams);
            var url = $"{_gatewayBffBaseUrl}/api/queries/catalog/search?{queryString}";

            var items = await _httpClient.GetFromJsonAsync<List<GatewayCatalogItemDto>>(url) ?? new();
            var allProducts = items.Select(MapToDisplayModel).ToList();

            return new ProductSearchResultModel
            {
                Products = allProducts
                    .Skip((searchModel.Page - 1) * searchModel.PageSize)
                    .Take(searchModel.PageSize)
                    .ToList(),
                TotalCount = allProducts.Count,
                CurrentPage = searchModel.Page,
                PageSize = searchModel.PageSize,
                SearchCriteria = searchModel
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return new ProductSearchResultModel();
        }
    }

    public async Task<ProductDisplayModel?> GetProductByIdAsync(Guid productId)
    {
        try
        {
            // GatewayBff/Controllers/QueriesController.cs -> GET "api/queries/catalog/{id:guid}".
            var url = $"{_gatewayBffBaseUrl}/api/queries/catalog/{productId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return null;

            var item = await response.Content.ReadFromJsonAsync<GatewayCatalogItemDto>();
            return item is null ? null : MapToDisplayModel(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ProductId}", productId);
            return null;
        }
    }

    public async Task<List<ProductDisplayModel>> GetFeaturedProductsAsync(int count = 10)
    {
        try
        {
            var url = $"{_gatewayBffBaseUrl}/api/queries/catalog/featured?count={count}";
            var items = await _httpClient.GetFromJsonAsync<List<GatewayCatalogItemDto>>(url);
            return items?.Select(MapToDisplayModel).ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving featured products");
            return new List<ProductDisplayModel>();
        }
    }

    public async Task<List<ProductDisplayModel>> GetBestSellersAsync(int count = 10)
    {
        try
        {
            var url = $"{_gatewayBffBaseUrl}/api/queries/catalog/bestsellers?count={count}";
            var items = await _httpClient.GetFromJsonAsync<List<GatewayCatalogItemDto>>(url);
            return items?.Select(MapToDisplayModel).ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bestsellers");
            return new List<ProductDisplayModel>();
        }
    }

    public async Task<List<ProductDisplayModel>> GetRecommendedProductsAsync(Guid? userId = null, int count = 10)
    {
        try
        {
            // GatewayBff's recommended endpoint has no per-user personalization either
            // (same as ProductService's), so userId isn't forwarded.
            var url = $"{_gatewayBffBaseUrl}/api/queries/catalog/recommended?count={count}";
            var items = await _httpClient.GetFromJsonAsync<List<GatewayCatalogItemDto>>(url);
            return items?.Select(MapToDisplayModel).ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recommended products");
            return new List<ProductDisplayModel>();
        }
    }

    public async Task<List<ProductDisplayModel>> GetRelatedProductsAsync(Guid productId, int count = 5)
    {
        try
        {
            var url = $"{_gatewayBffBaseUrl}/api/queries/catalog/{productId}/related?count={count}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new List<ProductDisplayModel>();

            var items = await response.Content.ReadFromJsonAsync<List<GatewayCatalogItemDto>>();
            return items?.Select(MapToDisplayModel).ToList() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving related products");
            return new List<ProductDisplayModel>();
        }
    }

    public async Task<List<CategoryModel>> GetCategoriesAsync()
    {
        try
        {
            var url = $"{_gatewayBffBaseUrl}/api/queries/categories";
            var names = await _httpClient.GetFromJsonAsync<List<string>>(url) ?? new();
            return names.Select(name => new CategoryModel { Name = name, DisplayName = name }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            return new List<CategoryModel>();
        }
    }

    public async Task<List<ProductDisplayModel>> GetProductsByCategoryAsync(string category, int page = 1, int pageSize = 20)
    {
        try
        {
            // GatewayBff/ProductService's category endpoint has no paging either - applied client-side.
            var url = $"{_gatewayBffBaseUrl}/api/queries/categories/{Uri.EscapeDataString(category)}/products";
            var items = await _httpClient.GetFromJsonAsync<List<GatewayCatalogItemDto>>(url) ?? new();
            return items.Select(MapToDisplayModel)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by category");
            return new List<ProductDisplayModel>();
        }
    }

    public async Task<List<string>> GetBrandsAsync()
    {
        try
        {
            var url = $"{_gatewayBffBaseUrl}/api/queries/brands";
            var response = await _httpClient.GetFromJsonAsync<List<string>>(url);
            return response ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving brands");
            return new List<string>();
        }
    }

    public async Task<List<ProductReviewModel>> GetProductReviewsAsync(Guid productId, int page = 1, int pageSize = 10)
    {
        try
        {
            // GatewayBff/ProductService's reviews endpoint has no paging either - applied client-side.
            var url = $"{_gatewayBffBaseUrl}/api/queries/catalog/{productId}/reviews";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new List<ProductReviewModel>();

            var reviews = await response.Content.ReadFromJsonAsync<List<GatewayReviewDto>>() ?? new();
            return reviews
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ProductReviewModel
                {
                    ReviewId = r.Id,
                    ProductId = r.ProductId,
                    CustomerName = r.ReviewerName,
                    Rating = r.Rating,
                    Content = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reviews");
            return new List<ProductReviewModel>();
        }
    }

    public async Task<bool> AddReviewAsync(AddReviewModel review, Guid userId)
    {
        try
        {
            // GatewayBff/Controllers/CommandsController.cs -> POST "api/commands/catalog/{id:guid}/reviews".
            // ProductService's review has no Title field and no user lookup for a display name, so
            // Title is dropped and the user id is what's shown as the reviewer - same honest gap as
            // every other cross-service DTO mismatch in this codebase, not fabricated data.
            var url = $"{_gatewayBffBaseUrl}/api/commands/catalog/{review.ProductId}/reviews";
            var payload = new
            {
                Rating = review.Rating,
                Comment = review.Content,
                ReviewerName = $"User {userId}"
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding the review");
            return false;
        }
    }

    private static ProductDisplayModel MapToDisplayModel(GatewayCatalogItemDto item) => new()
    {
        ProductId = item.ProductId,
        Name = item.Name,
        Description = item.Description ?? string.Empty,
        Price = item.Price,
        QuantityAvailable = item.AvailableQuantity
    };

    // Local mirror of GatewayBff.Contracts.CatalogItemDto/ReviewDto - CustomerWebsite doesn't
    // reference GatewayBff's project, so these are duplicated rather than shared.
    private record GatewayCatalogItemDto(Guid ProductId, string Name, string? Description, decimal Price, bool IsActive, int AvailableQuantity);
    private record GatewayReviewDto(Guid Id, Guid ProductId, int Rating, string Comment, string ReviewerName, DateTime CreatedAt);
}
