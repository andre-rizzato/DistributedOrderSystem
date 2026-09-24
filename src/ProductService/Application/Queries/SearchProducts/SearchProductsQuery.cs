namespace ProductService.Application.Queries.SearchProducts;

using MediatR;
using ProductService.Application.DTOs;

/// <summary>
/// CQRS Query: searches products using multiple filters.
/// </summary>
public record SearchProductsQuery(
    string? SearchTerm = null,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? IsActive = true) : IRequest<IEnumerable<ProductDto>>;
