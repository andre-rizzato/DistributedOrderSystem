namespace ProductService.Application.Queries.GetAllProducts;

using MediatR;
using ProductService.Application.DTOs;

/// <summary>
/// CQRS Query: recupera tutti i prodotti.
/// Le Query rappresentano richieste di lettura (read-side) e restituiscono DTO, non entità.
/// </summary>
public record GetAllProductsQuery : IRequest<IEnumerable<ProductDto>>;
