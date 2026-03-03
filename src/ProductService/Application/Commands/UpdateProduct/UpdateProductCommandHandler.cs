namespace ProductService.Application.Commands.UpdateProduct;

using MediatR;
using ProductService.Application.Common.Interfaces;
using ProductService.Application.DTOs;
using ProductService.Domain.Interfaces;

/// <summary>
/// Handler per UpdateProductCommand.
/// </summary>
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto?>
{
    private readonly IProductRepository _repository;
    private readonly IProductCache _cache;
    private readonly ILogger<UpdateProductCommandHandler> _logger;

    public UpdateProductCommandHandler(
        IProductRepository repository,
        IProductCache cache,
        ILogger<UpdateProductCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ProductDto?> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _repository.GetByIdAsync(request.Id, ct);
        if (product is null) return null;

        product.Name = request.Name;
        product.Price = request.Price;
        product.Description = request.Description ?? string.Empty;
        product.IsActive = request.IsActive;

        await _repository.UpdateAsync(product, ct);
        await _cache.SetProductAsync(product, ct);

        _logger.LogInformation("Prodotto aggiornato: {ProductId}", product.Id);

        return new ProductDto(product.Id, product.Name, product.Price, product.Description, product.IsActive);
    }
}
