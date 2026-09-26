namespace GatewayBff.Queries;

using GatewayBff.Clients;
using GatewayBff.Contracts;

/// <summary>
/// Shared by every query that turns ProductService's ProductDto list into the
/// CatalogItemDto shape (adds AvailableQuantity from InventoryService), so this
/// lookup-per-product loop isn't duplicated across every catalog query handler.
/// </summary>
internal static class CatalogEnrichment
{
    public static async Task<List<CatalogItemDto>> EnrichAsync(
        IEnumerable<ProductDto> products,
        IInventoryServiceClient inventory,
        ILogger logger,
        CancellationToken ct)
    {
        var result = new List<CatalogItemDto>();
        foreach (var p in products)
        {
            var availableQuantity = 0;
            try
            {
                var inv = await inventory.GetInventoryAsync(p.Id, ct);
                availableQuantity = inv?.AvailableQuantity ?? 0;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Inventory lookup failed for product {ProductId}", p.Id);
            }

            result.Add(new CatalogItemDto(p.Id, p.Name, p.Description, p.Price, p.IsActive, availableQuantity));
        }

        return result;
    }
}
