using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

namespace GatewayBff.Commands;

public sealed record CancelOrdersCommand(int OrderId) : IRequest<CancelOrderResponse?>;

public class CreateCancelCommandHandler : IRequestHandler<CancelOrdersCommand, CancelOrderResponse?>
{
    private readonly IOrderServiceClient _orders;

    public CreateCancelCommandHandler(IOrderServiceClient orders)
    {
        _orders = orders;
    }

    public Task<CancelOrderResponse?> Handle(CancelOrdersCommand request, CancellationToken cancellationToken) =>
        _orders.CancelOrderAsync(request.OrderId, cancellationToken);
}
