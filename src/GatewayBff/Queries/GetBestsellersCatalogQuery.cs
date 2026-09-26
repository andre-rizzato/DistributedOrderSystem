namespace GatewayBff.Queries;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record GetBestsellersCatalogQuery(int Count) : IRequest<List<CatalogItemDto>>;

public class GetBestsellersCatalogQueryHandler : IRequestHandler<GetBestsellersCatalogQuery, List<CatalogItemDto>>
{
    private readonly IProductServiceClient _products;
    private readonly IInventoryServiceClient _inventory;
    private readonly ILogger<GetBestsellersCatalogQueryHandler> _logger;

    public GetBestsellersCatalogQueryHandler(IProductServiceClient products, IInventoryServiceClient inventory, ILogger<GetBestsellersCatalogQueryHandler> logger)
    {
        _products = products;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task<List<CatalogItemDto>> Handle(GetBestsellersCatalogQuery request, CancellationToken ct)
    {
        var products = await _products.GetBestsellersAsync(request.Count, ct);
        return await CatalogEnrichment.EnrichAsync(products, _inventory, _logger, ct);
    }
}
