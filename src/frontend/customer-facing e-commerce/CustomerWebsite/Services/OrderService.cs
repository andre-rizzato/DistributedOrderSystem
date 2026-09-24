using CustomerWebsite.Models;

namespace CustomerWebsite.Services;

/// <summary>
/// Service for order management
/// </summary>
public interface IOrderService
{
    Task<OrderModel?> CreateOrderAsync(CheckoutModel checkout, Guid? userId = null);
    Task<OrderModel?> GetOrderByIdAsync(Guid orderId);
    Task<OrderHistoryModel> GetOrderHistoryAsync(Guid userId, OrderHistoryFilter filter, int page = 1, int pageSize = 10);
    Task<bool> CancelOrderAsync(Guid orderId);
    Task<List<ShippingOptionModel>> GetShippingOptionsAsync(AddressModel address, List<CartItemModel> items);
    Task<bool> ValidatePromoCodeAsync(string promoCode, decimal subtotal);
    Task<decimal> CalculatePromoCodeDiscountAsync(string promoCode, decimal subtotal);
}

public class OrderService : IOrderService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderService> _logger;
    private readonly string _orderServiceBaseUrl;

    public OrderService(HttpClient httpClient, IConfiguration configuration, ILogger<OrderService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _orderServiceBaseUrl = _configuration.GetValue<string>("Services:OrderService:BaseUrl") ??
                               "https://localhost:5001";
    }

    public async Task<OrderModel?> CreateOrderAsync(CheckoutModel checkout, Guid? userId = null)
    {
        try
        {
            var orderRequest = new
            {
                CustomerId = userId,
                CustomerEmail = checkout.ShippingAddress.FullName, // Uses email from the appropriate model
                Items = checkout.Cart.Items.Select(item => new
                {
                    ProductId = item.Product.ProductId,
                    ProductName = item.Product.Name,
                    ProductSku = item.Product.SKU,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    VariantInfo = item.SelectedVariant
                }).ToList(),
                ShippingAddress = checkout.ShippingAddress,
                BillingAddress = checkout.UseSameAddressForBilling ? checkout.ShippingAddress : checkout.BillingAddress,
                PaymentMethod = checkout.PaymentMethod,
                ShippingOption = checkout.SelectedShippingOption,
                PromoCode = checkout.PromoCode,
                OrderNotes = checkout.OrderNotes,
                Subtotal = checkout.Subtotal,
                ShippingCost = checkout.ShippingCost,
                TaxAmount = checkout.TaxAmount,
                DiscountAmount = checkout.DiscountAmount,
                Total = checkout.Total
            };

            var url = $"{_orderServiceBaseUrl}/api/orders";
            var response = await _httpClient.PostAsJsonAsync(url, orderRequest);

            if (response.IsSuccessStatusCode)
            {
                var orderData = await response.Content.ReadFromJsonAsync<dynamic>();
                return MapToOrderModel(orderData);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating the order");
            return null;
        }
    }

    public async Task<OrderModel?> GetOrderByIdAsync(Guid orderId)
    {
        try
        {
            var url = $"{_orderServiceBaseUrl}/api/orders/{orderId}";
            var orderData = await _httpClient.GetFromJsonAsync<dynamic>(url);

            return orderData != null ? MapToOrderModel(orderData) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
            return null;
        }
    }

    public async Task<OrderHistoryModel> GetOrderHistoryAsync(Guid userId, OrderHistoryFilter filter, int page = 1, int pageSize = 10)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"userId={userId}",
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (filter.Status.HasValue)
                queryParams.Add($"status={filter.Status.Value}");

            if (filter.StartDate.HasValue)
                queryParams.Add($"startDate={filter.StartDate.Value:yyyy-MM-dd}");

            if (filter.EndDate.HasValue)
                queryParams.Add($"endDate={filter.EndDate.Value:yyyy-MM-dd}");

            if (!string.IsNullOrWhiteSpace(filter.OrderNumber))
                queryParams.Add($"orderNumber={Uri.EscapeDataString(filter.OrderNumber)}");

            var queryString = string.Join("&", queryParams);
            var url = $"{_orderServiceBaseUrl}/api/orders/history?{queryString}";

            var response = await _httpClient.GetFromJsonAsync<dynamic>(url);

            var orderHistory = new OrderHistoryModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                Filter = filter
            };

            if (response != null)
            {
                orderHistory.TotalCount = (int)(response.totalCount ?? 0);

                if (response.orders != null)
                {
                    foreach (var orderData in response.orders)
                    {
                        var order = MapToOrderModel(orderData);
                        if (order != null)
                            orderHistory.Orders.Add(order);
                    }
                }
            }

            return orderHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order history for user {UserId}", userId);
            return new OrderHistoryModel();
        }
    }

    public async Task<bool> CancelOrderAsync(Guid orderId)
    {
        try
        {
            var url = $"{_orderServiceBaseUrl}/api/orders/{orderId}/cancel";
            var response = await _httpClient.PostAsync(url, null);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling order {OrderId}", orderId);
            return false;
        }
    }

    public async Task<List<ShippingOptionModel>> GetShippingOptionsAsync(AddressModel address, List<CartItemModel> items)
    {
        try
        {
            var request = new
            {
                Address = address,
                Items = items.Select(item => new
                {
                    ProductId = item.Product.ProductId,
                    Quantity = item.Quantity,
                    Weight = 1.0, // Placeholder weight, should come from the product
                    Dimensions = new { Length = 10, Width = 10, Height = 10 } // Placeholder dimensions
                }).ToList()
            };

            var url = $"{_orderServiceBaseUrl}/api/shipping/options";
            var response = await _httpClient.PostAsJsonAsync(url, request);

            if (response.IsSuccessStatusCode)
            {
                var options = await response.Content.ReadFromJsonAsync<List<ShippingOptionModel>>();
                return options ?? GetDefaultShippingOptions();
            }

            return GetDefaultShippingOptions();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shipping options");
            return GetDefaultShippingOptions();
        }
    }

    public async Task<bool> ValidatePromoCodeAsync(string promoCode, decimal subtotal)
    {
        try
        {
            var url = $"{_orderServiceBaseUrl}/api/promocodes/{promoCode}/validate";
            var request = new { Subtotal = subtotal };

            var response = await _httpClient.PostAsJsonAsync(url, request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating promo code {PromoCode}", promoCode);
            return false;
        }
    }

    public async Task<decimal> CalculatePromoCodeDiscountAsync(string promoCode, decimal subtotal)
    {
        try
        {
            var url = $"{_orderServiceBaseUrl}/api/promocodes/{promoCode}/discount";
            var request = new { Subtotal = subtotal };

            var response = await _httpClient.PostAsJsonAsync(url, request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<dynamic>();
                return (decimal)(result?.discountAmount ?? 0);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating discount for promo code {PromoCode}", promoCode);
            return 0;
        }
    }

    private OrderModel? MapToOrderModel(dynamic orderData)
    {
        try
        {
            return new OrderModel
            {
                OrderId = Guid.Parse(orderData.orderId.ToString()),
                OrderNumber = orderData.orderNumber?.ToString() ?? string.Empty,
                OrderDate = DateTime.Parse(orderData.orderDate.ToString()),
                Status = Enum.Parse<OrderStatus>(orderData.status.ToString()),
                CustomerId = orderData.customerId != null ? Guid.Parse(orderData.customerId.ToString()) : Guid.Empty,
                CustomerName = orderData.customerName?.ToString() ?? string.Empty,
                CustomerEmail = orderData.customerEmail?.ToString() ?? string.Empty,
                Total = (decimal)(orderData.total ?? 0),
                // Add other fields as needed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error mapping the order");
            return null;
        }
    }

    private List<ShippingOptionModel> GetDefaultShippingOptions()
    {
        return new List<ShippingOptionModel>
        {
            new()
            {
                ShippingOptionId = Guid.NewGuid(),
                Name = "Standard Shipping",
                Description = "Delivery in 3-5 business days",
                Cost = 4.99m,
                DeliveryDays = 4,
                IncludesTracking = true
            },
            new()
            {
                ShippingOptionId = Guid.NewGuid(),
                Name = "Express Shipping",
                Description = "Delivery in 1-2 business days",
                Cost = 9.99m,
                DeliveryDays = 2,
                IsExpress = true,
                IncludesTracking = true,
                IncludesInsurance = true
            },
            new()
            {
                ShippingOptionId = Guid.NewGuid(),
                Name = "Free Shipping",
                Description = "Delivery in 5-7 business days",
                Cost = 0,
                DeliveryDays = 6,
                IncludesTracking = false
            }
        };
    }
}
