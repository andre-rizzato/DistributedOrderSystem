namespace GatewayBff.Queries;

using System.Net.Http.Json;
using GatewayBff.Contracts;
using MediatR;

public record GetCartQuery(string SessionId) : IRequest<CartDto?>;

public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDto?>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<GetCartQueryHandler> _logger;

    public GetCartQueryHandler(IHttpClientFactory clients, ILogger<GetCartQueryHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<CartDto?> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var client = _clients.CreateClient();
            var response = await client.GetAsync(
                $"http://localhost:5009/api/cart/{request.SessionId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get cart for session {SessionId}: {StatusCode}",
                    request.SessionId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CartDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart for session {SessionId}", request.SessionId);
            return null;
        }
    }
}
