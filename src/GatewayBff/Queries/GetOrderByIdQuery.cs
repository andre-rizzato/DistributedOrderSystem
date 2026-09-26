namespace GatewayBff.Queries;

using MediatR;
using GatewayBff.Clients;
using GatewayBff.Contracts;

public record GetOrderByIdQuery(int OrderId) : IRequest<OrderDto?>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderServiceClient _orders;

    public GetOrderByIdQueryHandler(IOrderServiceClient orders)
    {
        _orders = orders;
    }

    public Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken) =>
        _orders.GetOrderAsync(request.OrderId, cancellationToken);
}
