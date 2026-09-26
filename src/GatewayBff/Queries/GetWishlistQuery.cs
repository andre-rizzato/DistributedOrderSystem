namespace GatewayBff.Queries;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record GetWishlistQuery(Guid UserId) : IRequest<WishlistDto?>;

public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, WishlistDto?>
{
    private readonly ICustomerServiceClient _customer;

    public GetWishlistQueryHandler(ICustomerServiceClient customer)
    {
        _customer = customer;
    }

    public Task<WishlistDto?> Handle(GetWishlistQuery request, CancellationToken cancellationToken) =>
        _customer.GetWishlistAsync(request.UserId, cancellationToken);
}
