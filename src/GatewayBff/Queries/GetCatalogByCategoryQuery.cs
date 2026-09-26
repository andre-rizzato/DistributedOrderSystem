namespace GatewayBff.Queries;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record GetCatalogByCategoryQuery(string Category) : IRequest<List<CatalogItemDto>>;

public class GetCatalogByCategoryQueryHandler : IRequestHandler<GetCatalogByCategoryQuery, List<CatalogItemDto>>
{
    private readonly IProductServiceClient _products;
    private readonly IInventoryServiceClient _inventory;
    private readonly ILogger<GetCatalogByCategoryQueryHandler> _logger;

    public GetCatalogByCategoryQueryHandler(IProductServiceClient products, IInventoryServiceClient inventory, ILogger<GetCatalogByCategoryQueryHandler> logger)
    {
        _products = products;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task<List<CatalogItemDto>> Handle(GetCatalogByCategoryQuery request, CancellationToken ct)
    {
        var products = await _products.GetProductsByCategoryAsync(request.Category, ct);
        return await CatalogEnrichment.EnrichAsync(products, _inventory, _logger, ct);
    }
}
