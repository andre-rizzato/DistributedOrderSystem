namespace GatewayBff.Commands;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record AddToWishlistCommand(Guid UserId, Guid ProductId) : IRequest<WishlistItemDto?>;

public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, WishlistItemDto?>
{
    private readonly ICustomerServiceClient _customer;

    public AddToWishlistCommandHandler(ICustomerServiceClient customer)
    {
        _customer = customer;
    }

    public Task<WishlistItemDto?> Handle(AddToWishlistCommand request, CancellationToken cancellationToken) =>
        _customer.AddWishlistItemAsync(request.UserId, request.ProductId, cancellationToken);
}
