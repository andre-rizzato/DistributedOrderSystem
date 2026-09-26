namespace GatewayBff.Clients;

using GatewayBff.Contracts;

public interface IProductServiceClient
{
    Task<ProductDto?> GetProductAsync(Guid id, CancellationToken ct);
    Task<List<ProductDto>> GetProductsAsync(CancellationToken ct);
    Task<ProductDto> CreateProductAsync(string name, decimal price, string? description, CancellationToken ct);
    Task<ProductDto> UpdateProductAsync(Guid id, string name, decimal price, string? description, bool isActive, CancellationToken ct);
    Task<bool> DeleteProductAsync(Guid id, CancellationToken ct);
}
