namespace ProductService.Application.Queries.GetProductById;

using MediatR;
using ProductService.Application.DTOs;

/// <summary>
/// CQRS Query: retrieves a product by ID.
/// Uses the cache (read-through) to optimize reads.
/// </summary>
public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
