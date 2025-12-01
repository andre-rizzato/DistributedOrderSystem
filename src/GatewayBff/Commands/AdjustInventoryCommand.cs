namespace GatewayBff.Commands;

using System.Net.Http.Json;
using MediatR;

public record AdjustInventoryCommand(int ProductId, int Delta) : IRequest<bool>;

public class AdjustInventoryCommandHandler : IRequestHandler<AdjustInventoryCommand, bool>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<AdjustInventoryCommandHandler> _logger;

    public AdjustInventoryCommandHandler(IHttpClientFactory clients, ILogger<AdjustInventoryCommandHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<bool> Handle(AdjustInventoryCommand request, CancellationToken ct)
    {
        var client = _clients.CreateClient("InventoryService");

        var payload = new
        {
            productId = request.ProductId,
            delta = request.Delta
        };

        _logger.LogInformation("Regolazione inventario per il prodotto {ProductId} di {Delta}", request.ProductId, request.Delta);

        var response = await client.PostAsJsonAsync("api/inventory/adjust", payload, ct);
        
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Inventario regolato per il prodotto {ProductId}", request.ProductId);
            return true;
        }

        _logger.LogWarning("Fallito regolare inventario per il prodotto {ProductId}, Status: {StatusCode}", 
            request.ProductId, response.StatusCode);
        
        return false;
    }
}
