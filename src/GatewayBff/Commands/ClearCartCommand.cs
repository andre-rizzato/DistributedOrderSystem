namespace GatewayBff.Commands;

using GatewayBff.Clients;
using MediatR;

public record ClearCartCommand(string SessionId) : IRequest<bool>;

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, bool>
{
    private readonly ICustomerServiceClient _customer;

    public ClearCartCommandHandler(ICustomerServiceClient customer)
    {
        _customer = customer;
    }

    public Task<bool> Handle(ClearCartCommand request, CancellationToken cancellationToken) =>
        _customer.ClearCartAsync(request.SessionId, cancellationToken);
}
