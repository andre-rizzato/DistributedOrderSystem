namespace GatewayBff.Queries;

using MediatR;
using GatewayBff.Clients;
using GatewayBff.Contracts;

public record GetAllOrdersQuery : IRequest<List<OrderDto>>;

public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, List<OrderDto>>
{
    private readonly IOrderServiceClient _orders;

    public GetAllOrdersQueryHandler(IOrderServiceClient orders)
    {
        _orders = orders;
    }

    public Task<List<OrderDto>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken) =>
        _orders.GetOrdersAsync(cancellationToken);
}
