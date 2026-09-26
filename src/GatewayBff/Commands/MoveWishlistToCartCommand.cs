namespace GatewayBff.Commands;

using GatewayBff.Clients;
using MediatR;

public record MoveWishlistToCartCommand(Guid UserId, Guid ProductId, string SessionId) : IRequest<bool>;

public class MoveWishlistToCartCommandHandler : IRequestHandler<MoveWishlistToCartCommand, bool>
{
    private readonly ICustomerServiceClient _customer;

    public MoveWishlistToCartCommandHandler(ICustomerServiceClient customer)
    {
        _customer = customer;
    }

    public Task<bool> Handle(MoveWishlistToCartCommand request, CancellationToken cancellationToken) =>
        _customer.MoveWishlistItemToCartAsync(request.UserId, request.ProductId, request.SessionId, cancellationToken);
}
