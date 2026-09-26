namespace GatewayBff.Commands;

using GatewayBff.Clients;
using MediatR;

public record RemoveFromCartCommand(string SessionId, Guid CartItemId) : IRequest<bool>;

public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, bool>
{
    private readonly ICustomerServiceClient _customer;

    public RemoveFromCartCommandHandler(ICustomerServiceClient customer)
    {
        _customer = customer;
    }

    public Task<bool> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken) =>
        _customer.RemoveCartItemAsync(request.SessionId, request.CartItemId, cancellationToken);
}
