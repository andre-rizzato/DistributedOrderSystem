namespace GatewayBff.Queries;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record SearchCatalogQuery(string? SearchTerm, string? Category, decimal? MinPrice, decimal? MaxPrice) : IRequest<List<CatalogItemDto>>;

public class SearchCatalogQueryHandler : IRequestHandler<SearchCatalogQuery, List<CatalogItemDto>>
{
    private readonly IProductServiceClient _products;
    private readonly IInventoryServiceClient _inventory;
    private readonly ILogger<SearchCatalogQueryHandler> _logger;

    public SearchCatalogQueryHandler(IProductServiceClient products, IInventoryServiceClient inventory, ILogger<SearchCatalogQueryHandler> logger)
    {
        _products = products;
        _inventory = inventory;
        _logger = logger;
    }

    public async Task<List<CatalogItemDto>> Handle(SearchCatalogQuery request, CancellationToken ct)
    {
        var products = await _products.SearchProductsAsync(request.SearchTerm, request.Category, request.MinPrice, request.MaxPrice, ct);
        return await CatalogEnrichment.EnrichAsync(products, _inventory, _logger, ct);
    }
}
