namespace ProductService.Application.Queries.SearchProducts;

using MediatR;
using ProductService.Application.DTOs;
using ProductService.Domain.Interfaces;

/// <summary>
/// Handler per SearchProductsQuery.
/// Applica i filtri lato server e restituisce DTO read-only.
/// </summary>
public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IProductRepository _repository;
    private readonly ILogger<SearchProductsQueryHandler> _logger;

    public SearchProductsQueryHandler(IProductRepository repository, ILogger<SearchProductsQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> Handle(SearchProductsQuery request, CancellationToken ct)
    {
        var products = await _repository.GetAllAsync(ct);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            products = products.Where(p =>
                p.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            _logger.LogWarning("Filtro per categoria non ancora implementato");
        }

        if (request.MinPrice.HasValue)
            products = products.Where(p => p.Price >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            products = products.Where(p => p.Price <= request.MaxPrice.Value);

        if (request.IsActive.HasValue)
            products = products.Where(p => p.IsActive == request.IsActive.Value);

        return products.Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Description, p.IsActive));
    }
}
