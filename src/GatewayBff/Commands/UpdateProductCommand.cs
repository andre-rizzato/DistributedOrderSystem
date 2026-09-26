namespace GatewayBff.Commands;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record UpdateProductCommand(Guid Id, string Name, decimal Price, string? Description, bool IsActive) : IRequest<ProductDto>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductServiceClient _products;

    public UpdateProductCommandHandler(IProductServiceClient products)
    {
        _products = products;
    }

    public Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken ct) =>
        _products.UpdateProductAsync(request.Id, request.Name, request.Price, request.Description, request.IsActive, ct);
}
