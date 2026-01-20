namespace GatewayBff.Commands;

using System.Net.Http.Json;
using GatewayBff.Contracts;
using MediatR;

public record AddToWishlistCommand(Guid UserId, Guid ProductId) : IRequest<WishlistItemDto?>;

public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, WishlistItemDto?>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<AddToWishlistCommandHandler> _logger;

    public AddToWishlistCommandHandler(IHttpClientFactory clients, ILogger<AddToWishlistCommandHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<WishlistItemDto?> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clients.CreateClient();
            var payload = new { ProductId = request.ProductId };
            
            var response = await client.PostAsJsonAsync(
                $"http://localhost:5009/api/wishlist/{request.UserId}/items",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to add to wishlist: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<WishlistItemDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to wishlist");
            return null;
        }
    }
}
