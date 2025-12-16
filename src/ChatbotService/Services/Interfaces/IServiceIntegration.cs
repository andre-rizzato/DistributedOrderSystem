namespace ChatbotService.Services.Interfaces;

public interface IServiceIntegration
{
    Task<dynamic?> GetOrderByIdAsync(int orderId, string userId);
    Task<List<dynamic>> GetUserOrdersAsync(string userId);
    Task<List<dynamic>> SearchProductsAsync(string searchQuery);
    Task<bool> CancelOrderAsync(int orderId, string userId);
    Task<dynamic?> GetInventoryAsync(int productId);
    Task<dynamic?> GetPaymentInfoAsync(int orderId, string userId);
    Task<bool> CreateOrderAsync(dynamic orderData, string userId);
}