namespace GatewayBff.Commands;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record CreateProductCommand(string Name, decimal Price, string? Description) : IRequest<ProductDto>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductServiceClient _products;

    public CreateProductCommandHandler(IProductServiceClient products)
    {
        _products = products;
    }

    public Task<ProductDto> Handle(CreateProductCommand request, CancellationToken ct) =>
        _products.CreateProductAsync(request.Name, request.Price, request.Description, ct);
}
