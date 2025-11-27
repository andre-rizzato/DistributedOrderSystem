namespace GatewayBff.Commands;

using System.Net.Http.Json;
using GatewayBff.Contracts;
using MediatR;

public record CreateProductCommand(string Name, decimal Price, string? Description) : IRequest<ProductDto>;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(IHttpClientFactory clients, ILogger<CreateProductCommandHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var client = _clients.CreateClient("ProductService");

        var payload = new
        {
            name = request.Name,
            price = request.Price,
            description = request.Description ?? string.Empty
        };

        _logger.LogInformation("Creating product: {ProductName}", request.Name);

        var response = await client.PostAsJsonAsync("api/products", payload, ct);
        response.EnsureSuccessStatusCode();

        var product = await response.Content.ReadFromJsonAsync<ProductDto>(ct);
        
        _logger.LogInformation("Product created with ID: {ProductId}", product?.Id);
        
        return product ?? throw new InvalidOperationException("Failed to create product");
    }
}
