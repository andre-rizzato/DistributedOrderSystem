namespace GatewayBff.Commands;

using GatewayBff.Clients;
using MediatR;

public record DeleteProductCommand(Guid Id) : IRequest<bool>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductServiceClient _products;

    public DeleteProductCommandHandler(IProductServiceClient products)
    {
        _products = products;
    }

    public Task<bool> Handle(DeleteProductCommand request, CancellationToken ct) =>
        _products.DeleteProductAsync(request.Id, ct);
}
