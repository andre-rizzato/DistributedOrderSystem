namespace GatewayBff.Queries;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record GetRelatedCatalogQuery(Guid ProductId, int Count) : IRequest<List<CatalogItemDto>?>;

public class GetRelatedCatalogQueryHandler : IRequestHandler<GetRelatedCatalogQuery, List<CatalogItemDto>?>
{
    private readonly IProductServiceClient _products;
    private readonly IInventoryServiceClient _inventory;
    private readonly ILogger<GetRelatedCatalogQueryHandler> _logger;

    public GetRelatedCatalogQueryHandler(IProductServiceClient products, IInventoryServiceClient inventory, ILogger<GetRelatedCatalogQueryHandler> logger)
    {
        _products = products;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task<List<CatalogItemDto>?> Handle(GetRelatedCatalogQuery request, CancellationToken ct)
    {
        var products = await _products.GetRelatedProductsAsync(request.ProductId, request.Count, ct);
        if (products is null)
            return null;

        return await CatalogEnrichment.EnrichAsync(products, _inventory, _logger, ct);
    }
}
