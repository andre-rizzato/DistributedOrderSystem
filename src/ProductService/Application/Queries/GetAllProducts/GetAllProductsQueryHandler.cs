namespace ProductService.Application.Queries.GetAllProducts;

using MediatR;
using ProductService.Application.DTOs;
using ProductService.Domain.Interfaces;

/// <summary>
/// Handler per GetAllProductsQuery.
/// Legge direttamente dal repository (read-side ottimizzato).
/// </summary>
public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, IEnumerable<ProductDto>>
{
    private readonly IProductRepository _repository;

    public GetAllProductsQueryHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetAllProductsQuery request, CancellationToken ct)
    {
        var products = await _repository.GetAllAsync(ct);
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Description, p.IsActive));
    }
}
