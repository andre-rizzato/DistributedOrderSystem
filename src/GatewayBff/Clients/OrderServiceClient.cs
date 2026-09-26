namespace GatewayBff.Clients;

using System.Net.Http.Json;
using GatewayBff.Contracts;

public class OrderServiceClient : IOrderServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<OrderServiceClient> _logger;

    public OrderServiceClient(HttpClient http, ILogger<OrderServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<OrderDto?> GetOrderAsync(int id, CancellationToken ct)
    {
        _logger.LogInformation("Retrieving order {OrderId}", id);

        var response = await _http.GetAsync($"api/orders/{id}", ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Order {OrderId} not found", id);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<OrderDto>(ct);
    }

    public async Task<List<OrderDto>> GetOrdersAsync(CancellationToken ct)
    {
        _logger.LogInformation("Retrieving all orders");

        var response = await _http.GetAsync("api/orders", ct);
        response.EnsureSuccessStatusCode();

        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>(ct);
        return orders ?? new List<OrderDto>();
    }

    public async Task<CreateOrderResponse> CreateOrderAsync(List<OrderItemDto> items, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("api/commands/orders", new CreateOrderRequest(items), ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CreateOrderResponse>(ct)
            ?? throw new InvalidOperationException("OrderService returned an invalid response");
    }

    public async Task<bool> UpdateOrderStatusAsync(int id, string status, CancellationToken ct)
    {
        var response = await _http.PutAsJsonAsync($"api/commands/orders/{id}/status", new { status }, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to update order {OrderId} to status {Status}, Status: {StatusCode}",
                id, status, response.StatusCode);
        }

        return response.IsSuccessStatusCode;
    }

    public async Task<CancelOrderResponse?> CancelOrderAsync(int id, CancellationToken ct)
    {
        var response = await _http.PutAsync($"api/commands/orders/{id}/cancel", content: null, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Order {OrderId} not found for cancellation", id);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to cancel order {OrderId}, Status: {StatusCode}", id, response.StatusCode);
            return new CancelOrderResponse(id, IsCanceled: false);
        }

        return await response.Content.ReadFromJsonAsync<CancelOrderResponse>(ct)
            ?? new CancelOrderResponse(id, IsCanceled: true);
    }
}
