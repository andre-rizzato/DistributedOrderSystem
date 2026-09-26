namespace GatewayBff.Commands;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record UpdateCartItemCommand(string SessionId, Guid CartItemId, int Quantity) : IRequest<CartItemDto?>;

public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, CartItemDto?>
{
    private readonly ICustomerServiceClient _customer;

    public UpdateCartItemCommandHandler(ICustomerServiceClient customer)
    {
        _customer = customer;
    }

    public Task<CartItemDto?> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken) =>
        _customer.UpdateCartItemAsync(request.SessionId, request.CartItemId, request.Quantity, cancellationToken);
}
