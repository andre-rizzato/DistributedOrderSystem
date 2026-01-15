using CustomerWebsite.Models;

namespace CustomerWebsite.Services;

/// <summary>
/// Servizio per la gestione dei prodotti tramite API
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
    private readonly string _productServiceBaseUrl;

    public ProductService(HttpClient httpClient, IConfiguration configuration, ILogger<ProductService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _productServiceBaseUrl = _configuration.GetValue<string>("Services:ProductService:BaseUrl") ?? 
                                "https://localhost:5003";
    }

    public async Task<ProductSearchResultModel> SearchProductsAsync(ProductSearchModel searchModel)
    {
        try
        {
            var queryParams = new List<string>();
            
            if (!string.IsNullOrWhiteSpace(searchModel.SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(searchModel.SearchTerm)}");
            
            if (!string.IsNullOrWhiteSpace(searchModel.Category))
                queryParams.Add($"category={Uri.EscapeDataString(searchModel.Category)}");
            
            if (!string.IsNullOrWhiteSpace(searchModel.Brand))
                queryParams.Add($"brand={Uri.EscapeDataString(searchModel.Brand)}");
            
            if (searchModel.MinPrice.HasValue)
                queryParams.Add($"minPrice={searchModel.MinPrice.Value}");
            
            if (searchModel.MaxPrice.HasValue)
                queryParams.Add($"maxPrice={searchModel.MaxPrice.Value}");
            
            queryParams.Add($"onlyInStock={searchModel.OnlyInStock}");
            queryParams.Add($"onlyPrimeEligible={searchModel.OnlyPrimeEligible}");
            queryParams.Add($"sortOrder={searchModel.SortOrder}");
            queryParams.Add($"page={searchModel.Page}");
            queryParams.Add($"pageSize={searchModel.PageSize}");

            var queryString = string.Join("&", queryParams);
            var url = $"{_productServiceBaseUrl}/api/products/search?{queryString}";

            var response = await _httpClient.GetFromJsonAsync<ProductSearchResultModel>(url);
            return response ?? new ProductSearchResultModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la ricerca prodotti");
            return new ProductSearchResultModel();
        }
    }

    public async Task<ProductDisplayModel?> GetProductByIdAsync(Guid productId)
    {
        try
        {
            var url = $"{_productServiceBaseUrl}/api/products/{productId}";
            return await _httpClient.GetFromJsonAsync<ProductDisplayModel>(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero del prodotto {ProductId}", productId);
            return null;
        }
    }

    public async Task<List<ProductDisplayModel>> GetFeaturedProductsAsync(int count = 10)
    {
        try
        {
            var url = $"{_productServiceBaseUrl}/api/products/featured?count={count}";
            var response = await _httpClient.GetFromJsonAsync<List<ProductDisplayModel>>(url);
            return response ?? new List<ProductDisplayModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei prodotti in evidenza");
            return new List<ProductDisplayModel>();
        }
    }

    public async Task<List<ProductDisplayModel>> GetBestSellersAsync(int count = 10)
    {
        try
        {
            var url = $"{_productServiceBaseUrl}/api/products/bestsellers?count={count}";
            var response = await _httpClient.GetFromJsonAsync<List<ProductDisplayModel>>(url);
            return response ?? new List<ProductDisplayModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei bestseller");
            return new List<ProductDisplayModel>();
        }
    }

    public async Task<List<ProductDisplayModel>> GetRecommendedProductsAsync(Guid? userId = null, int count = 10)
    {
        try
        {
            var url = $"{_productServiceBaseUrl}/api/products/recommended?count={count}";
            if (userId.HasValue)
                url += $"&userId={userId.Value}";
            
            var response = await _httpClient.GetFromJsonAsync<List<ProductDisplayModel>>(url);
            return response ?? new List<ProductDisplayModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei prodotti raccomandati");
            return new List<ProductDisplayModel>();
        }
    }

    public async Task<List<ProductDisplayModel>> GetRelatedProductsAsync(Guid productId, int count = 5)
    {
        try
        {
            var url = $"{_productServiceBaseUrl}/api/products/{productId}/related?count={count}";
            var response = await _httpClient.GetFromJsonAsync<List<ProductDisplayModel>>(url);
            return response ?? new List<ProductDisplayModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei prodotti correlati");
            return new List<ProductDisplayModel>();
        }
    }

    public async Task<List<CategoryModel>> GetCategoriesAsync()
    {
        try
        {
            var url = $"{_productServiceBaseUrl}/api/categories";
            var response = await _httpClient.GetFromJsonAsync<List<CategoryModel>>(url);
            return response ?? new List<CategoryModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero delle categorie");
            return new List<CategoryModel>();
        }
    }

    public async Task<List<ProductDisplayModel>> GetProductsByCategoryAsync(string category, int page = 1, int pageSize = 20)
    {
        try
        {
            var url = $"{_productServiceBaseUrl}/api/categories/{Uri.EscapeDataString(category)}/products?page={page}&pageSize={pageSize}";
            var response = await _httpClient.GetFromJsonAsync<List<ProductDisplayModel>>(url);
            return response ?? new List<ProductDisplayModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei prodotti per categoria");
            return new List<ProductDisplayModel>();
        }
    }

    public async Task<List<string>> GetBrandsAsync()
    {
        try
        {
            var url = $"{_productServiceBaseUrl}/api/products/brands";
            var response = await _httpClient.GetFromJsonAsync<List<string>>(url);
            return response ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dei brand");
            return new List<string>();
        }
    }

    public async Task<List<ProductReviewModel>> GetProductReviewsAsync(Guid productId, int page = 1, int pageSize = 10)
    {
        try
        {
            var url = $"{_productServiceBaseUrl}/api/products/{productId}/reviews?page={page}&pageSize={pageSize}";
            var response = await _httpClient.GetFromJsonAsync<List<ProductReviewModel>>(url);
            return response ?? new List<ProductReviewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero delle recensioni");
            return new List<ProductReviewModel>();
        }
    }

    public async Task<bool> AddReviewAsync(AddReviewModel review, Guid userId)
    {
        try
        {
            var url = $"{_productServiceBaseUrl}/api/products/{review.ProductId}/reviews";
            var reviewData = new
            {
                review.Rating,
                review.Title,
                review.Content,
                UserId = userId
            };
            
            var response = await _httpClient.PostAsJsonAsync(url, reviewData);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'aggiunta della recensione");
            return false;
        }
    }
}