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
}
