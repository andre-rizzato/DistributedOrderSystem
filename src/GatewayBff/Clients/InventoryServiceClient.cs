namespace GatewayBff.Clients;

using System.Net.Http.Json;
using GatewayBff.Contracts;

public class InventoryServiceClient : IInventoryServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<InventoryServiceClient> _logger;

    public InventoryServiceClient(HttpClient http, ILogger<InventoryServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Task<InventoryDto?> GetInventoryAsync(Guid productId, CancellationToken ct) =>
        _http.GetFromJsonAsync<InventoryDto>($"api/inventory/{productId}", ct);

    public async Task<bool> AdjustInventoryAsync(Guid productId, int delta, CancellationToken ct)
    {
        var payload = new { productId, delta };

        _logger.LogInformation("Adjusting inventory for product {ProductId} by {Delta}", productId, delta);

        var response = await _http.PostAsJsonAsync("api/inventory/adjust", payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to adjust inventory for product {ProductId}, Status: {StatusCode}",
                productId, response.StatusCode);
        }

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> SetInventoryAsync(Guid productId, int quantity, CancellationToken ct)
    {
        var payload = new[] { new { productId, quantity } };

        _logger.LogInformation("Setting inventory for product {ProductId} to {Quantity}", productId, quantity);

        var response = await _http.PostAsJsonAsync("api/inventory/seed", payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to set inventory for product {ProductId}, Status: {StatusCode}",
                productId, response.StatusCode);
        }

        return response.IsSuccessStatusCode;
    }
}
