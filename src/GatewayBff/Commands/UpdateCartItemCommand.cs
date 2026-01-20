namespace GatewayBff.Commands;

using System.Net.Http.Json;
using GatewayBff.Contracts;
using MediatR;

public record UpdateCartItemCommand(string SessionId, Guid CartItemId, int Quantity) : IRequest<CartItemDto?>;

public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, CartItemDto?>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<UpdateCartItemCommandHandler> _logger;

    public UpdateCartItemCommandHandler(IHttpClientFactory clients, ILogger<UpdateCartItemCommandHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<CartItemDto?> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clients.CreateClient();
            var payload = new { Quantity = request.Quantity };
            
            var response = await client.PutAsJsonAsync(
                $"http://localhost:5009/api/cart/{request.SessionId}/items/{request.CartItemId}",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to update cart item: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CartItemDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item");
            return null;
        }
    }
}
