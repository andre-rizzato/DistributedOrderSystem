namespace ProductService.Application.Commands.DeleteProduct;

using MediatR;

/// <summary>
/// CQRS Command: elimina un prodotto per ID.
/// </summary>
public record DeleteProductCommand(Guid Id) : IRequest<bool>;
