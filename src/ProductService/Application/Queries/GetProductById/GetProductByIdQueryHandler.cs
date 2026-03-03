namespace ProductService.Application.Queries.GetProductById;

using MediatR;
using ProductService.Application.Common.Interfaces;
using ProductService.Application.DTOs;
using ProductService.Domain.Interfaces;

/// <summary>
/// Handler per GetProductByIdQuery.
/// Implementa il pattern cache-aside: controlla prima la cache, poi il database.
/// </summary>
public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _repository;
    private readonly IProductCache _cache;

    public GetProductByIdQueryHandler(IProductRepository repository, IProductCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        // Read-through cache
        var cached = await _cache.GetProductByIdAsync(request.Id, ct);
        if (cached is not null)
            return new ProductDto(cached.Id, cached.Name, cached.Price, cached.Description, cached.IsActive);

        var product = await _repository.GetByIdAsync(request.Id, ct);
        if (product is null) return null;

        await _cache.SetProductAsync(product, ct);
        return new ProductDto(product.Id, product.Name, product.Price, product.Description, product.IsActive);
    }
}
