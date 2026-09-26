using CustomerWebsite.Models;

namespace CustomerWebsite.Services;

/// <summary>
/// Service for order management
/// </summary>
public interface IOrderService
{
    Task<OrderModel?> CreateOrderAsync(CheckoutModel checkout, Guid? userId = null);
    Task<OrderModel?> GetOrderByIdAsync(int orderId);
    Task<OrderHistoryModel> GetOrderHistoryAsync(Guid userId, OrderHistoryFilter filter, int page = 1, int pageSize = 10);
    Task<bool> CancelOrderAsync(int orderId);
    Task<List<ShippingOptionModel>> GetShippingOptionsAsync(AddressModel address, List<CartItemModel> items);
    Task<bool> ValidatePromoCodeAsync(string promoCode, decimal subtotal);
    Task<decimal> CalculatePromoCodeDiscountAsync(string promoCode, decimal subtotal);
}

public class OrderService : IOrderService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderService> _logger;
    private readonly string _gatewayBffBaseUrl;

    public OrderService(HttpClient httpClient, IConfiguration configuration, ILogger<OrderService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        // Goes through GatewayBff (like ShoppingCartService/WishlistService), not straight to
        // OrderService - GatewayBff's CreateOrderCommand is what validates the product/stock and
        // resolves the authoritative price, none of which OrderService does on its own.
        _gatewayBffBaseUrl = _configuration.GetValue<string>("Services:GatewayBff:BaseUrl") ??
                             "http://localhost:5189";
    }

    public async Task<OrderModel?> CreateOrderAsync(CheckoutModel checkout, Guid? userId = null)
    {
        try
        {
            // GatewayBff/Controllers/CommandsController.cs -> POST "orders" only wants
            // {Items:[{ProductId,Quantity,UnitPrice}]} - it looks up the authoritative price
            // itself and ignores whatever UnitPrice is sent here. Addresses, payment method,
            // shipping option and promo code have no home on the backend today, so they're
            // only echoed back into the confirmation model from what the user just submitted,
            // not persisted anywhere.
            var orderRequest = new
            {
                Items = checkout.Cart.Items.Select(item => new
                {
                    ProductId = item.Product.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList()
            };

            var url = $"{_gatewayBffBaseUrl}/api/commands/orders";
            var response = await _httpClient.PostAsJsonAsync(url, orderRequest);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error creating the order: {StatusCode}", response.StatusCode);
                return null;
            }

            var created = await response.Content.ReadFromJsonAsync<GatewayCreateOrderResponse>();
            if (created is null)
            {
                return null;
            }

            return new OrderModel
            {
                OrderId = created.OrderId,
                OrderDate = DateTime.UtcNow,
                Status = ParseStatus(created.Status),
                CustomerId = userId ?? Guid.Empty,
                ShippingAddress = checkout.ShippingAddress,
                BillingAddress = checkout.UseSameAddressForBilling ? checkout.ShippingAddress : checkout.BillingAddress ?? new(),
                UseSameAddressForBilling = checkout.UseSameAddressForBilling,
                Items = checkout.Cart.Items.Select(item => new OrderItemModel
                {
                    ProductId = item.Product.ProductId,
                    ProductName = item.Product.Name,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    VariantInfo = item.SelectedVariant
                }).ToList(),
                PaymentMethod = checkout.PaymentMethod,
                ShippingOption = checkout.SelectedShippingOption,
                ShippingCost = checkout.ShippingCost,
                TaxAmount = checkout.TaxAmount,
                DiscountAmount = checkout.DiscountAmount,
                Total = created.Total,
                PromoCode = checkout.PromoCode,
                OrderNotes = checkout.OrderNotes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating the order");
            return null;
        }
    }

    public async Task<OrderModel?> GetOrderByIdAsync(int orderId)
    {
        try
        {
            // GatewayBff/Queries/GetOrderByIdQuery.cs -> GET "api/queries/orders/{id}".
            var url = $"{_gatewayBffBaseUrl}/api/queries/orders/{orderId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var order = await response.Content.ReadFromJsonAsync<GatewayOrderDto>();
            return order is null ? null : MapToOrderModel(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
            return null;
        }
    }

    public async Task<OrderHistoryModel> GetOrderHistoryAsync(Guid userId, OrderHistoryFilter filter, int page = 1, int pageSize = 10)
    {
        var orderHistory = new OrderHistoryModel
        {
            CurrentPage = page,
            PageSize = pageSize,
            Filter = filter
        };

        try
        {
            // GatewayBff has no per-user order query yet (GET "api/queries/orders" returns every
            // order in the system) and Order has no "order number" concept, only an int Id - so
            // this filters/paginates client-side on what's available, and matches OrderNumber
            // against the Id itself as the closest equivalent. userId filtering is a no-op until
            // GatewayBff exposes a scoped endpoint.
            var url = $"{_gatewayBffBaseUrl}/api/queries/orders";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return orderHistory;
            }

            var orders = await response.Content.ReadFromJsonAsync<List<GatewayOrderDto>>() ?? new();

            IEnumerable<GatewayOrderDto> filtered = orders;
            if (filter.Status.HasValue)
            {
                filtered = filtered.Where(o => ParseStatus(o.Status) == filter.Status.Value);
            }
            if (filter.StartDate.HasValue)
            {
                filtered = filtered.Where(o => o.CreatedAt >= filter.StartDate.Value);
            }
            if (filter.EndDate.HasValue)
            {
                filtered = filtered.Where(o => o.CreatedAt <= filter.EndDate.Value);
            }
            if (!string.IsNullOrWhiteSpace(filter.OrderNumber))
            {
                filtered = filtered.Where(o => o.Id.ToString() == filter.OrderNumber);
            }

            var filteredList = filtered.ToList();
            orderHistory.TotalCount = filteredList.Count;
            orderHistory.Orders = filteredList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(MapToOrderModel)
                .ToList();

            return orderHistory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order history for user {UserId}", userId);
            return orderHistory;
        }
    }

    public async Task<bool> CancelOrderAsync(int orderId)
    {
        try
        {
            // GatewayBff/Controllers/CommandsController.cs -> PUT "orders/cancel/{id:int}".
            var url = $"{_gatewayBffBaseUrl}/api/commands/orders/cancel/{orderId}";
            var response = await _httpClient.PutAsync(url, null);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling order {OrderId}", orderId);
            return false;
        }
    }

    public Task<List<ShippingOptionModel>> GetShippingOptionsAsync(AddressModel address, List<CartItemModel> items) =>
        // No shipping-rate backend exists anywhere in the system - this always returns the
        // same local defaults rather than pretending to call a real quoting service.
        Task.FromResult(GetDefaultShippingOptions());

    public Task<bool> ValidatePromoCodeAsync(string promoCode, decimal subtotal) =>
        // No promo-code service/table exists anywhere in the backend - report "no valid code"
        // honestly instead of calling a URL that doesn't exist.
        Task.FromResult(false);

    public Task<decimal> CalculatePromoCodeDiscountAsync(string promoCode, decimal subtotal) =>
        Task.FromResult(0m);

    private static OrderModel MapToOrderModel(GatewayOrderDto order) => new()
    {
        OrderId = order.Id,
        OrderDate = order.CreatedAt,
        Status = ParseStatus(order.Status),
        Total = order.Total,
        Items = order.Items.Select(i => new OrderItemModel
        {
            ProductId = i.ProductId,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }).ToList()
    };

    private static OrderStatus ParseStatus(string status) =>
        Enum.TryParse<OrderStatus>(status, ignoreCase: true, out var parsed) ? parsed : OrderStatus.Pending;

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

    // Local mirrors of GatewayBff's response shapes (GatewayBff.Contracts.OrderDtos) -
    // CustomerWebsite doesn't reference GatewayBff's project, so these are duplicated
    // rather than shared, same as every other cross-service DTO in this codebase.
    private record GatewayOrderDto(int Id, DateTime CreatedAt, string Status, decimal Total, List<GatewayOrderItemDto> Items);
    private record GatewayOrderItemDto(int Id, Guid ProductId, int Quantity, decimal UnitPrice);
    private record GatewayCreateOrderResponse(int OrderId, string Status, decimal Total);
}
