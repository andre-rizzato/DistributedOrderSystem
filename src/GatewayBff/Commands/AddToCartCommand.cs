namespace GatewayBff.Commands;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record AddToCartCommand(string SessionId, Guid ProductId, int Quantity, decimal UnitPrice) : IRequest<CartItemDto?>;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, CartItemDto?>
{
    private readonly ICustomerServiceClient _customer;

    public AddToCartCommandHandler(ICustomerServiceClient customer)
    {
        _customer = customer;
    }

    public Task<CartItemDto?> Handle(AddToCartCommand request, CancellationToken cancellationToken) =>
        _customer.AddCartItemAsync(request.SessionId, request.ProductId, request.Quantity, request.UnitPrice, cancellationToken);
}
