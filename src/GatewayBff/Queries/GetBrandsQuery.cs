namespace GatewayBff.Queries;

using GatewayBff.Clients;
using MediatR;

public record GetBrandsQuery() : IRequest<List<string>>;

public class GetBrandsQueryHandler : IRequestHandler<GetBrandsQuery, List<string>>
{
    private readonly IProductServiceClient _products;

    public GetBrandsQueryHandler(IProductServiceClient products) => _products = products;

    public Task<List<string>> Handle(GetBrandsQuery request, CancellationToken ct) =>
        _products.GetBrandsAsync(ct);
}
