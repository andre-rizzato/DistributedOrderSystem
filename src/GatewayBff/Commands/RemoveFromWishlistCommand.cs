namespace GatewayBff.Commands;

using MediatR;

public record RemoveFromWishlistCommand(Guid UserId, Guid ProductId) : IRequest<bool>;

public class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand, bool>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<RemoveFromWishlistCommandHandler> _logger;

    public RemoveFromWishlistCommandHandler(IHttpClientFactory clients, ILogger<RemoveFromWishlistCommandHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<bool> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clients.CreateClient();
            var response = await client.DeleteAsync(
                $"http://localhost:5009/api/wishlist/{request.UserId}/items/{request.ProductId}",
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from wishlist");
            return false;
        }
    }
}
