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

        // 2) Check inventory (read-only check; replace with reserve endpoint when available)
        foreach (var item in request.Items)
        {
            var inv = await inventoryClient.GetFromJsonAsync<InventoryDto>($"api/inventory/{item.ProductId}", ct);
            if (inv is null || inv.AvailableQuantity < item.Quantity)
            {
                throw new InvalidOperationException($"Not enough inventory for product {item.ProductId}.");
            }
        }

        // 3) Forward command to OrderService
        var orderResp = await orderClient.PostAsJsonAsync("api/commands/orders", new CreateOrderRequest(request.Items), ct);
        orderResp.EnsureSuccessStatusCode();

        var created = await orderResp.Content.ReadFromJsonAsync<CreateOrderResponse>(cancellationToken: ct)
                     ?? throw new InvalidOperationException("OrderService returned an invalid response");

        return created;
    }

    private record ProductDto(int Id, string Name, string? Description, decimal Price, bool IsActive);
    private record InventoryDto(int ProductId, int AvailableQuantity, int ReservedQuantity);
}
