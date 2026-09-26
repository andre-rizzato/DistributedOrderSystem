namespace GatewayBff.Queries;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record GetCartQuery(string SessionId) : IRequest<CartDto?>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto?>
{
    private readonly ICustomerServiceClient _customer;

    public GetCartQueryHandler(ICustomerServiceClient customer)
    {
        _customer = customer;
    }

    public Task<CartDto?> Handle(GetCartQuery request, CancellationToken cancellationToken) =>
        _customer.GetCartAsync(request.SessionId, cancellationToken);
}
