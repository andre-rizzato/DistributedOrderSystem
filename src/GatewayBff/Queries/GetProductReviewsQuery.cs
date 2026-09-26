namespace GatewayBff.Queries;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record GetProductReviewsQuery(Guid ProductId) : IRequest<List<ReviewDto>?>;

public class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, List<ReviewDto>?>
{
    private readonly IProductServiceClient _products;

    public GetProductReviewsQueryHandler(IProductServiceClient products) => _products = products;

    public Task<List<ReviewDto>?> Handle(GetProductReviewsQuery request, CancellationToken ct) =>
        _products.GetProductReviewsAsync(request.ProductId, ct);
}
