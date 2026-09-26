namespace GatewayBff.Queries;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record GetFeaturedCatalogQuery(int Count) : IRequest<List<CatalogItemDto>>;

public class GetFeaturedCatalogQueryHandler : IRequestHandler<GetFeaturedCatalogQuery, List<CatalogItemDto>>
{
    private readonly IProductServiceClient _products;
    private readonly IInventoryServiceClient _inventory;
    private readonly ILogger<GetFeaturedCatalogQueryHandler> _logger;

    public GetFeaturedCatalogQueryHandler(IProductServiceClient products, IInventoryServiceClient inventory, ILogger<GetFeaturedCatalogQueryHandler> logger)
    {
        _products = products;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task<List<CatalogItemDto>> Handle(GetFeaturedCatalogQuery request, CancellationToken ct)
    {
        var products = await _products.GetFeaturedProductsAsync(request.Count, ct);
        return await CatalogEnrichment.EnrichAsync(products, _inventory, _logger, ct);
    }
}
