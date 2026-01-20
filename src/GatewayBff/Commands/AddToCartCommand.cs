namespace GatewayBff.Commands;

using System.Net.Http.Json;
using System.Text.Json;
using GatewayBff.Contracts;
using MediatR;

public record AddToCartCommand(string SessionId, Guid ProductId, int Quantity, decimal UnitPrice) : IRequest<CartItemDto?>;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, CartItemDto?>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<AddToCartCommandHandler> _logger;

    public AddToCartCommandHandler(IHttpClientFactory clients, ILogger<AddToCartCommandHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<CartItemDto?> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clients.CreateClient();
            var payload = new { ProductId = request.ProductId, Quantity = request.Quantity, UnitPrice = request.UnitPrice };
            
            var response = await client.PostAsJsonAsync(
                $"http://localhost:5009/api/cart/{request.SessionId}/items",
                payload,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to add to cart: {StatusCode}", response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CartItemDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to cart");
            return null;
        }
    }
}
