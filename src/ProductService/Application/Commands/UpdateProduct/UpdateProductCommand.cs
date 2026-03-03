namespace ProductService.Application.Commands.UpdateProduct;

using MediatR;
using ProductService.Application.DTOs;

/// <summary>
/// CQRS Command: aggiorna un prodotto esistente.
/// </summary>
public record UpdateProductCommand(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    bool IsActive) : IRequest<ProductDto?>;
