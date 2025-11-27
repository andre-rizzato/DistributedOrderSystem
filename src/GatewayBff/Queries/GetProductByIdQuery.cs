namespace GatewayBff.Queries;

using System.Net.Http.Json;
using GatewayBff.Contracts;
using MediatR;

public record GetProductByIdQuery(int Id) : IRequest<CatalogItemDto?>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, CatalogItemDto?>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(IHttpClientFactory clients, ILogger<GetProductByIdQueryHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<CatalogItemDto?> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var productClient = _clients.CreateClient("ProductService");
        var inventoryClient = _clients.CreateClient("InventoryService");

        try
        {
            var product = await productClient.GetFromJsonAsync<ProductDto>($"api/products/{request.Id}", ct);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found", request.Id);
                return null;
            }

            int availableQuantity = 0;
            try
            {
                var inv = await inventoryClient.GetFromJsonAsync<InventoryDto>($"api/inventory/{product.Id}", ct);
                availableQuantity = inv?.AvailableQuantity ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Inventory lookup failed for product {ProductId}", product.Id);
            }

            return new CatalogItemDto(
                product.Id, 
                product.Name, 
                product.Description, 
                product.Price, 
                product.IsActive, 
                availableQuantity
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId}", request.Id);
            return null;
        }
    }

    private record ProductDto(int Id, string Name, string? Description, decimal Price, bool IsActive);
    private record InventoryDto(int ProductId, int AvailableQuantity, int ReservedQuantity);
}
