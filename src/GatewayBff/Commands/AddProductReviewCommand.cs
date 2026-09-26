namespace GatewayBff.Commands;

using GatewayBff.Clients;
using GatewayBff.Contracts;
using MediatR;

public record AddProductReviewCommand(Guid ProductId, int Rating, string Comment, string ReviewerName) : IRequest<ReviewDto?>;

public class AddProductReviewCommandHandler : IRequestHandler<AddProductReviewCommand, ReviewDto?>
{
    private readonly IProductServiceClient _products;

    public AddProductReviewCommandHandler(IProductServiceClient products) => _products = products;

    public Task<ReviewDto?> Handle(AddProductReviewCommand request, CancellationToken ct) =>
        _products.AddProductReviewAsync(request.ProductId, request.Rating, request.Comment, request.ReviewerName, ct);
}
