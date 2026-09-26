namespace GatewayBff.Queries;

using GatewayBff.Clients;
using MediatR;

public record GetCategoriesQuery() : IRequest<List<string>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<string>>
{
    private readonly IProductServiceClient _products;

    public GetCategoriesQueryHandler(IProductServiceClient products) => _products = products;

    public Task<List<string>> Handle(GetCategoriesQuery request, CancellationToken ct) =>
        _products.GetCategoriesAsync(ct);
}
