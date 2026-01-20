namespace GatewayBff.Commands;

using MediatR;

public record DeleteProductCommand(Guid Id) : IRequest<bool>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(IHttpClientFactory clients, ILogger<DeleteProductCommandHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var client = _clients.CreateClient("ProductService");

        _logger.LogInformation("Eliminazione prodotto: {ProductId}", request.Id);

        var response = await client.DeleteAsync($"api/products/{request.Id}", ct);
        
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Prodotto eliminato: {ProductId}", request.Id);
            return true;
        }

        _logger.LogWarning("Fallito eliminare prodotto: {ProductId}, Status: {StatusCode}", 
            request.Id, response.StatusCode);
        
        return false;
    }
}
