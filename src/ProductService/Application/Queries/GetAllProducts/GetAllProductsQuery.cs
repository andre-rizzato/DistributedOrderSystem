namespace ProductService.Application.Queries.GetAllProducts;

using MediatR;
using ProductService.Application.DTOs;

/// <summary>
/// CQRS Query: retrieves all products.
/// Queries represent read-side requests and return DTOs, not entities.
/// </summary>
public record GetAllProductsQuery : IRequest<IEnumerable<ProductDto>>;
