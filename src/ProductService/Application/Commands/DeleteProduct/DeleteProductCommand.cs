namespace ProductService.Application.Commands.DeleteProduct;

using MediatR;

/// <summary>
/// CQRS Command: deletes a product by ID.
/// </summary>
public record DeleteProductCommand(Guid Id) : IRequest<bool>;
