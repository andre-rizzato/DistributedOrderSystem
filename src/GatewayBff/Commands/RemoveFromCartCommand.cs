namespace GatewayBff.Commands;

using MediatR;

public record RemoveFromCartCommand(string SessionId, Guid CartItemId) : IRequest<bool>;

public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, bool>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<RemoveFromCartCommandHandler> _logger;

    public RemoveFromCartCommandHandler(IHttpClientFactory clients, ILogger<RemoveFromCartCommandHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<bool> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clients.CreateClient();
            var response = await client.DeleteAsync(
                $"http://localhost:5009/api/cart/{request.SessionId}/items/{request.CartItemId}",
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from cart");
            return false;
        }
    }
}
