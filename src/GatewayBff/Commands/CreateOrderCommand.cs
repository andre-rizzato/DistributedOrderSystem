namespace GatewayBff.Commands;

using GatewayBff.Contracts;
using System.Net;
using System.Net.Http.Json;
using MediatR;

public record CreateOrderCommand(List<OrderItemDto> Items) : IRequest<CreateOrderResponse>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResponse>
{
    private readonly IHttpClientFactory _clients;

    public CreateOrderCommandHandler(IHttpClientFactory clients)
    {
        _clients = clients;
    }

    public async Task<CreateOrderResponse> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var productClient = _clients.CreateClient("ProductService");
        var inventoryClient = _clients.CreateClient("InventoryService");
        var orderClient = _clients.CreateClient("OrderService");

        // 1) Validate products exist/active
        var productIds = request.Items.Select(i => i.ProductId).Distinct();
        foreach (var id in productIds)
        {
            var product = await productClient.GetFromJsonAsync<ProductDto>($"api/products/{id}", ct);
            if (product is null || !product.IsActive)
                throw new InvalidOperationException($"Product {id} is invalid or inactive.");
        }

        // 2) Get product prices and prepare order items with prices
        var orderItems = new List<OrderItemDto>();
        foreach (var item in request.Items)
        {
            var product = await productClient.GetFromJsonAsync<ProductDto>($"api/products/{item.ProductId}", ct);
            if (product is null)
                throw new InvalidOperationException($"Product {item.ProductId} not found.");
            
            var inv = await inventoryClient.GetFromJsonAsync<InventoryDto>($"api/inventory/{item.ProductId}", ct);
            if (inv is null || inv.AvailableQuantity < item.Quantity)
            {
                throw new InvalidOperationException($"Not enough inventory for product {item.ProductId}.");
            }
            
            orderItems.Add(new OrderItemDto(item.ProductId, item.Quantity, product.Price));
        }

        // 3) Forward command to OrderService with prices
        var orderResp = await orderClient.PostAsJsonAsync("api/commands/orders", new CreateOrderRequest(orderItems), ct);
        orderResp.EnsureSuccessStatusCode();

        var created = await orderResp.Content.ReadFromJsonAsync<CreateOrderResponse>(cancellationToken: ct)
                     ?? throw new InvalidOperationException("OrderService returned an invalid response");

        return created;
    }

    private record ProductDto(Guid Id, string Name, string? Description, decimal Price, bool IsActive);
    private record InventoryDto(Guid ProductId, int AvailableQuantity, int ReservedQuantity);
}
