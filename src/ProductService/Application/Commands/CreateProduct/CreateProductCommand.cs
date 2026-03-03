namespace ProductService.Application.Commands.CreateProduct;

using MediatR;
using ProductService.Application.DTOs;

/// <summary>
/// CQRS Command: crea un nuovo prodotto.
/// I Command rappresentano intenzioni di modifica (write-side).
/// </summary>
public record CreateProductCommand(
    string Name,
    decimal Price,
    string? Description) : IRequest<ProductDto>;
