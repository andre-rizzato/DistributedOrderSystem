namespace GatewayBff.Commands;

using System.Net.Http.Json;
using MediatR;

public record SetInventoryCommand(int ProductId, int Quantity) : IRequest<bool>;

public class SetInventoryCommandHandler : IRequestHandler<SetInventoryCommand, bool>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<SetInventoryCommandHandler> _logger;

    public SetInventoryCommandHandler(IHttpClientFactory clients, ILogger<SetInventoryCommandHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<bool> Handle(SetInventoryCommand request, CancellationToken ct)
    {
        var client = _clients.CreateClient("InventoryService");

        var payload = new[]
        {
            new
            {
                productId = request.ProductId,
                quantity = request.Quantity
            }
        };

        _logger.LogInformation("Setting inventory for product {ProductId} to {Quantity}", request.ProductId, request.Quantity);

        var response = await client.PostAsJsonAsync("api/inventory/seed", payload, ct);
        
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Inventory set for product {ProductId}", request.ProductId);
            return true;
        }

        _logger.LogWarning("Failed to set inventory for product {ProductId}, Status: {StatusCode}", 
            request.ProductId, response.StatusCode);
        
        return false;
    }
}
