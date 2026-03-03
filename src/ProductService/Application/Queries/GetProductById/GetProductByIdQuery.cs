namespace ProductService.Application.Queries.GetProductById;

using MediatR;
using ProductService.Application.DTOs;

/// <summary>
/// CQRS Query: recupera un prodotto per ID.
/// Utilizza il cache (read-through) per ottimizzare le letture.
/// </summary>
public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
