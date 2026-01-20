namespace GatewayBff.Queries;

using System.Net.Http.Json;
using GatewayBff.Contracts;
using MediatR;

public record GetWishlistQuery(Guid UserId) : IRequest<WishlistDto?>;

public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, WishlistDto?>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<GetWishlistQueryHandler> _logger;

    public GetWishlistQueryHandler(IHttpClientFactory clients, ILogger<GetWishlistQueryHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<WishlistDto?> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clients.CreateClient();
            var response = await client.GetAsync(
                $"http://localhost:5009/api/wishlist/{request.UserId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get wishlist for user {UserId}: {StatusCode}",
                    request.UserId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<WishlistDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wishlist for user {UserId}", request.UserId);
            return null;
        }
    }
}
