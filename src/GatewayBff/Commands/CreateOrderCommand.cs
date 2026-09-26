namespace GatewayBff.Commands;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record CreateOrderCommand(List<OrderItemDto> Items) : IRequest<CreateOrderResponse>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IProductServiceClient _products;
    private readonly IInventoryServiceClient _inventory;
    private readonly IOrderServiceClient _orders;

    public CreateOrderCommandHandler(IProductServiceClient products, IInventoryServiceClient inventory, IOrderServiceClient orders)
    {
        _products = products;
        _inventory = inventory;
        _orders = orders;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // 1) Validate products exist/active
        var productIds = request.Items.Select(i => i.ProductId).Distinct();
        foreach (var id in productIds)
        {
            var product = await _products.GetProductAsync(id, ct);
            if (product is null || !product.IsActive)
                throw new InvalidOperationException($"Product {id} is invalid or inactive.");
        }

        // 2) Get product prices and prepare order items with prices
        var orderItems = new List<OrderItemDto>();
        foreach (var item in request.Items)
        {
            var product = await _products.GetProductAsync(item.ProductId, ct);
            if (product is null)
                throw new InvalidOperationException($"Product {item.ProductId} not found.");

            var inv = await _inventory.GetInventoryAsync(item.ProductId, ct);
            if (inv is null || inv.AvailableQuantity < item.Quantity)
            {
                throw new InvalidOperationException($"Not enough inventory for product {item.ProductId}.");
            }

            orderItems.Add(new OrderItemDto(item.ProductId, item.Quantity, product.Price));
        }

        // 3) Forward command to OrderService with prices
        return await _orders.CreateOrderAsync(orderItems, ct);
    }
}
