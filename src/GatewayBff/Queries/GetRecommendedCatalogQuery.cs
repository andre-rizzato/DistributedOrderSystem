namespace GatewayBff.Queries;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record GetRecommendedCatalogQuery(int Count) : IRequest<List<CatalogItemDto>>;

public class GetRecommendedCatalogQueryHandler : IRequestHandler<GetRecommendedCatalogQuery, List<CatalogItemDto>>
{
    private readonly IProductServiceClient _products;
    private readonly IInventoryServiceClient _inventory;
    private readonly ILogger<GetRecommendedCatalogQueryHandler> _logger;

    public GetRecommendedCatalogQueryHandler(IProductServiceClient products, IInventoryServiceClient inventory, ILogger<GetRecommendedCatalogQueryHandler> logger)
    {
        _products = products;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task<List<CatalogItemDto>> Handle(GetRecommendedCatalogQuery request, CancellationToken ct)
    {
        var products = await _products.GetRecommendedProductsAsync(request.Count, ct);
        return await CatalogEnrichment.EnrichAsync(products, _inventory, _logger, ct);
    }
}
