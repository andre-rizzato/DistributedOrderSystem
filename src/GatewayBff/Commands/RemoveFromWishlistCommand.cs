namespace GatewayBff.Commands;

using GatewayBff.Clients;
using MediatR;

public record RemoveFromWishlistCommand(Guid UserId, Guid ProductId) : IRequest<bool>;

public class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand, bool>
{
    private readonly ICustomerServiceClient _customer;

    public RemoveFromWishlistCommandHandler(ICustomerServiceClient customer)
    {
        _customer = customer;
    }

    public Task<bool> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken) =>
        _customer.RemoveWishlistItemAsync(request.UserId, request.ProductId, cancellationToken);
}
