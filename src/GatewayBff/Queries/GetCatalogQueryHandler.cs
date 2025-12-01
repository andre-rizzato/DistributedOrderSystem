namespace GatewayBff.Queries;

using System.Net.Http.Json;
using GatewayBff.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

public class GetCatalogQueryHandler : IRequestHandler<GetCatalogQuery, List<CatalogItemDto>>
{
    private readonly IHttpClientFactory _clients;
    private readonly ILogger<GetCatalogQueryHandler> _logger;

    public GetCatalogQueryHandler(IHttpClientFactory clients, ILogger<GetCatalogQueryHandler> logger)
    {
        _clients = clients;
        _logger = logger;
    }

    public async Task<List<CatalogItemDto>> Handle(GetCatalogQuery request, CancellationToken ct)
    {
        var productClient = _clients.CreateClient("ProductService");
        var inventoryClient = _clients.CreateClient("InventoryService");

        var products = await productClient.GetFromJsonAsync<List<ProductDto>>("api/products", ct) ?? new();

        var inventory = new Dictionary<int, int>();
        foreach (var p in products)
        {
            try
            {
                var inv = await inventoryClient.GetFromJsonAsync<InventoryDto>($"api/inventory/{p.Id}", ct);
                inventory[p.Id] = inv?.AvailableQuantity ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ricerca inventario fallita per il prodotto {ProductId}", p.Id);
                inventory[p.Id] = 0;
            }
        }

        return products.Select(p =>
            new CatalogItemDto(p.Id, p.Name, p.Description, p.Price, p.IsActive, inventory.GetValueOrDefault(p.Id, 0))
        ).ToList();
    }

    private record ProductDto(int Id, string Name, string? Description, decimal Price, bool IsActive);
    private record InventoryDto(int ProductId, int AvailableQuantity, int ReservedQuantity);
}
