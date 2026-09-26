namespace GatewayBff.Queries;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record GetProductByIdQuery(Guid Id) : IRequest<CatalogItemDto?>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, CatalogItemDto?>
{
    private readonly IProductServiceClient _products;
    private readonly IInventoryServiceClient _inventory;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(IProductServiceClient products, IInventoryServiceClient inventory, ILogger<GetProductByIdQueryHandler> logger)
    {
        _products = products;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task<CatalogItemDto?> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        try
        {
            var product = await _products.GetProductAsync(request.Id, ct);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found", request.Id);
                return null;
            }

            int availableQuantity = 0;
            try
            {
                var inv = await _inventory.GetInventoryAsync(product.Id, ct);
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
            _logger.LogError(ex, "Error retrieving product {ProductId}", request.Id);
            return null;
        }
    }
}
