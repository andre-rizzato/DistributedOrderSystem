namespace ProductService.Application.Commands.CreateProduct;

using MediatR;
using ProductService.Application.DTOs;

/// <summary>
/// CQRS Command: creates a new product.
/// Commands represent write-side intentions to change state.
/// </summary>
public record CreateProductCommand(
    string Name,
    decimal Price,
    string? Description) : IRequest<ProductDto>;
