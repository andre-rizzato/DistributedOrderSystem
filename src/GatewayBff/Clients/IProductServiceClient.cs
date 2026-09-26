namespace GatewayBff.Clients;

using GatewayBff.Contracts;

public interface IProductServiceClient
{
    Task<ProductDto?> GetProductAsync(Guid id, CancellationToken ct);
    Task<List<ProductDto>> GetProductsAsync(CancellationToken ct);
    Task<ProductDto> CreateProductAsync(string name, decimal price, string? description, CancellationToken ct);
    Task<ProductDto> UpdateProductAsync(Guid id, string name, decimal price, string? description, bool isActive, CancellationToken ct);
    Task<bool> DeleteProductAsync(Guid id, CancellationToken ct);

    Task<List<ProductDto>> SearchProductsAsync(string? searchTerm, string? category, decimal? minPrice, decimal? maxPrice, CancellationToken ct);
    Task<List<ProductDto>> GetFeaturedProductsAsync(int count, CancellationToken ct);
    Task<List<ProductDto>> GetBestsellersAsync(int count, CancellationToken ct);
    Task<List<ProductDto>> GetRecommendedProductsAsync(int count, CancellationToken ct);
    Task<List<ProductDto>?> GetRelatedProductsAsync(Guid productId, int count, CancellationToken ct);
    Task<List<ProductDto>> GetProductsByCategoryAsync(string category, CancellationToken ct);
    Task<List<string>> GetCategoriesAsync(CancellationToken ct);
    Task<List<string>> GetBrandsAsync(CancellationToken ct);
    Task<List<ReviewDto>?> GetProductReviewsAsync(Guid productId, CancellationToken ct);
    Task<ReviewDto?> AddProductReviewAsync(Guid productId, int rating, string comment, string reviewerName, CancellationToken ct);
}
