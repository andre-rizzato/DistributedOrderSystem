namespace GatewayBff.Commands;

using MediatR;

public record ClearCartCommand(string SessionId) : IRequest<bool>;

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, bool>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<ClearCartCommandHandler> _logger;

    public ClearCartCommandHandler(IHttpClientFactory clients, ILogger<ClearCartCommandHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<bool> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clients.CreateClient();
            var response = await client.DeleteAsync(
                $"http://localhost:5009/api/cart/{request.SessionId}",
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return false;
        }
    }
}
