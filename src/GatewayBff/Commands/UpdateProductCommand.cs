namespace GatewayBff.Commands;

using System.Net.Http.Json;
using GatewayBff.Contracts;
using MediatR;

public record UpdateProductCommand(Guid Id, string Name, decimal Price, string? Description, bool IsActive) : IRequest<ProductDto>;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(IHttpClientFactory clients, ILogger<UpdateProductCommandHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var client = _clients.CreateClient("ProductService");

        var payload = new
        {
            name = request.Name,
            price = request.Price,
            description = request.Description ?? string.Empty,
            isActive = request.IsActive
        };

        _logger.LogInformation("Aggiornamento prodotto: {ProductId}", request.Id);

        var response = await client.PutAsJsonAsync($"api/products/{request.Id}", payload, ct);
        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<ProductDto>(ct);
        
        _logger.LogInformation("Prodotto aggiornato: {ProductId}", product?.Id);
        
        return product ?? throw new InvalidOperationException("Failed to update product");
    }
}
