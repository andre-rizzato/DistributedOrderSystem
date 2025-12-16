namespace ChatbotService.Services;

using ChatbotService.Services.Interfaces;
using ChatbotService.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

public class ServiceIntegrationService : IServiceIntegration
{
    private readonly HttpClient _httpClient;
    private readonly ServiceUrlsSettings _serviceUrls;
    private readonly ILogger<ServiceIntegrationService> _logger;

    public ServiceIntegrationService(
        HttpClient httpClient,
        IOptions<ServiceUrlsSettings> serviceUrls,
        ILogger<ServiceIntegrationService> logger)
    {
        _httpClient = httpClient;
        _serviceUrls = serviceUrls.Value;
        _logger = logger;
    }

    public async Task<dynamic?> GetOrderByIdAsync(int orderId, string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_serviceUrls.GatewayBff}/api/queries/orders/{orderId}");
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var order = JsonSerializer.Deserialize<dynamic>(content);
            
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching order {OrderId} for user {UserId}", orderId, userId);
            return null;
        }
    }

    public async Task<List<dynamic>> GetUserOrdersAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_serviceUrls.GatewayBff}/api/queries/orders");
            
            if (!response.IsSuccessStatusCode)
                return new List<dynamic>();

            var content = await response.Content.ReadAsStringAsync();
            var orders = JsonSerializer.Deserialize<List<dynamic>>(content) ?? new List<dynamic>();
            
            return orders.Take(10).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching orders for user {UserId}", userId);
            return new List<dynamic>();
        }
    }

    public async Task<List<dynamic>> SearchProductsAsync(string searchQuery)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_serviceUrls.GatewayBff}/api/queries/catalog");
            
            if (!response.IsSuccessStatusCode)
                return new List<dynamic>();

            var content = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<dynamic>>(content) ?? new List<dynamic>();
            
            // Simple client-side filtering
            var filtered = products.Where(p => 
            {
                try
                {
                    var name = p.GetProperty("name").GetString() ?? "";
                    var description = p.GetProperty("description").GetString() ?? "";
                    return name.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                           description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }).ToList();
            
            return filtered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with query: {SearchQuery}", searchQuery);
            return new List<dynamic>();
        }
    }

    public async Task<bool> CancelOrderAsync(int orderId, string userId)
    {
        try
        {
            var response = await _httpClient.PutAsync(
                $"{_serviceUrls.GatewayBff}/api/commands/orders/{orderId}/cancel",
                new StringContent("", System.Text.Encoding.UTF8, "application/json"));

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId} for user {UserId}", orderId, userId);
            return false;
        }
    }

    public async Task<dynamic?> GetInventoryAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_serviceUrls.InventoryService}/api/inventory/{productId}");
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<dynamic>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching inventory for product {ProductId}", productId);
            return null;
        }
    }

    public async Task<dynamic?> GetPaymentInfoAsync(int orderId, string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_serviceUrls.GatewayBff}/api/queries/payments/order/{orderId}");
            
            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<dynamic>(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment info for order {OrderId}", orderId);
            return null;
        }
    }

    public async Task<bool> CreateOrderAsync(dynamic orderData, string userId)
    {
        try
        {
            var jsonContent = JsonSerializer.Serialize(orderData);
            var response = await _httpClient.PostAsync(
                $"{_serviceUrls.GatewayBff}/api/commands/orders",
                new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json"));

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for user {UserId}", userId);
            return false;
        }
    }
}