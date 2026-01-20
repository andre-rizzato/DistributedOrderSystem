namespace GatewayBff.Commands;

using System.Net.Http.Json;
using MediatR;

public record MoveWishlistToCartCommand(Guid UserId, Guid ProductId, string SessionId) : IRequest<bool>;

public class MoveWishlistToCartCommandHandler : IRequestHandler<MoveWishlistToCartCommand, bool>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<MoveWishlistToCartCommandHandler> _logger;

    public MoveWishlistToCartCommandHandler(IHttpClientFactory clients, ILogger<MoveWishlistToCartCommandHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<bool> Handle(MoveWishlistToCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clients.CreateClient();
            var payload = new { SessionId = request.SessionId };
            
            var response = await client.PostAsJsonAsync(
                $"http://localhost:5009/api/wishlist/{request.UserId}/items/{request.ProductId}/move-to-cart",
                payload,
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving wishlist item to cart");
            return false;
        }
    }
}
