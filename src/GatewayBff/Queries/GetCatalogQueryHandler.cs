namespace GatewayBff.Queries;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public class GetCatalogQueryHandler : IRequestHandler<GetCatalogQuery, List<CatalogItemDto>>
{
    private readonly IProductServiceClient _products;
    private readonly IInventoryServiceClient _inventory;
    private readonly ILogger<GetCatalogQueryHandler> _logger;

    public GetCatalogQueryHandler(IProductServiceClient products, IInventoryServiceClient inventory, ILogger<GetCatalogQueryHandler> logger)
    {
        _products = products;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task<List<CatalogItemDto>> Handle(GetCatalogQuery request, CancellationToken ct)
    {
        var products = await _products.GetProductsAsync(ct);

        var inventory = new Dictionary<Guid, int>();
        foreach (var p in products)
        {
            try
            {
                var inv = await _inventory.GetInventoryAsync(p.Id, ct);
                inventory[p.Id] = inv?.AvailableQuantity ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Inventory lookup failed for product {ProductId}", p.Id);
                inventory[p.Id] = 0;
            }
        }

        return products.Select(p =>
            new CatalogItemDto(p.Id, p.Name, p.Description, p.Price, p.IsActive, inventory.GetValueOrDefault(p.Id, 0))
        ).ToList();
    }
}
