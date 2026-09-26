namespace GatewayBff.Clients;

using System.Net.Http.Json;
using GatewayBff.Contracts;

public class ProductServiceClient : IProductServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ProductServiceClient> _logger;

    public ProductServiceClient(HttpClient http, ILogger<ProductServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Task<ProductDto?> GetProductAsync(Guid id, CancellationToken ct) =>
        _http.GetFromJsonAsync<ProductDto>($"api/products/{id}", ct);

    public async Task<List<ProductDto>> GetProductsAsync(CancellationToken ct) =>
        await _http.GetFromJsonAsync<List<ProductDto>>("api/products", ct) ?? new();

    public async Task<ProductDto> CreateProductAsync(string name, decimal price, string? description, CancellationToken ct)
    {
        var payload = new { name, price, description = description ?? string.Empty };

        _logger.LogInformation("Creating product: {ProductName}", name);

        var response = await _http.PostAsJsonAsync("api/products", payload, ct);
        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<ProductDto>(ct);
        return product ?? throw new InvalidOperationException("Failed to create product");
    }

    public async Task<ProductDto> UpdateProductAsync(Guid id, string name, decimal price, string? description, bool isActive, CancellationToken ct)
    {
        var payload = new { name, price, description = description ?? string.Empty, isActive };

        _logger.LogInformation("Updating product: {ProductId}", id);

        var response = await _http.PutAsJsonAsync($"api/products/{id}", payload, ct);
        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<ProductDto>(ct);
        return product ?? throw new InvalidOperationException("Failed to update product");
    }

    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken ct)
    {
        _logger.LogInformation("Deleting product: {ProductId}", id);

        var response = await _http.DeleteAsync($"api/products/{id}", ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to delete product: {ProductId}, Status: {StatusCode}", id, response.StatusCode);
        }

        return response.IsSuccessStatusCode;
    }

    public async Task<List<ProductDto>> SearchProductsAsync(string? searchTerm, string? category, decimal? minPrice, decimal? maxPrice, CancellationToken ct)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(searchTerm)) query.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
        if (!string.IsNullOrWhiteSpace(category)) query.Add($"category={Uri.EscapeDataString(category)}");
        if (minPrice.HasValue) query.Add($"minPrice={minPrice.Value}");
        if (maxPrice.HasValue) query.Add($"maxPrice={maxPrice.Value}");

        var url = "api/products/search" + (query.Count > 0 ? $"?{string.Join("&", query)}" : string.Empty);
        return await _http.GetFromJsonAsync<List<ProductDto>>(url, ct) ?? new();
    }

    public async Task<List<ProductDto>> GetFeaturedProductsAsync(int count, CancellationToken ct) =>
        await _http.GetFromJsonAsync<List<ProductDto>>($"api/products/featured?count={count}", ct) ?? new();

    public async Task<List<ProductDto>> GetBestsellersAsync(int count, CancellationToken ct) =>
        await _http.GetFromJsonAsync<List<ProductDto>>($"api/products/bestsellers?count={count}", ct) ?? new();

    public async Task<List<ProductDto>> GetRecommendedProductsAsync(int count, CancellationToken ct) =>
        await _http.GetFromJsonAsync<List<ProductDto>>($"api/products/recommended?count={count}", ct) ?? new();

    public async Task<List<ProductDto>?> GetRelatedProductsAsync(Guid productId, int count, CancellationToken ct)
    {
        var response = await _http.GetAsync($"api/products/{productId}/related?count={count}", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ProductDto>>(ct) ?? new();
    }

    public async Task<List<ProductDto>> GetProductsByCategoryAsync(string category, CancellationToken ct) =>
        await _http.GetFromJsonAsync<List<ProductDto>>($"api/categories/{Uri.EscapeDataString(category)}/products", ct) ?? new();

    public async Task<List<string>> GetCategoriesAsync(CancellationToken ct) =>
        await _http.GetFromJsonAsync<List<string>>("api/categories", ct) ?? new();

    public async Task<List<string>> GetBrandsAsync(CancellationToken ct) =>
        await _http.GetFromJsonAsync<List<string>>("api/products/brands", ct) ?? new();

    public async Task<List<ReviewDto>?> GetProductReviewsAsync(Guid productId, CancellationToken ct)
    {
        var response = await _http.GetAsync($"api/products/{productId}/reviews", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ReviewDto>>(ct) ?? new();
    }

    public async Task<ReviewDto?> AddProductReviewAsync(Guid productId, int rating, string comment, string reviewerName, CancellationToken ct)
    {
        var payload = new { Rating = rating, Comment = comment, ReviewerName = reviewerName };
        var response = await _http.PostAsJsonAsync($"api/products/{productId}/reviews", payload, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReviewDto>(ct);
    }
}
