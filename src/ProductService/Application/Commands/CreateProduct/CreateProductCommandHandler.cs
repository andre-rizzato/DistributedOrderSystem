namespace ProductService.Application.Commands.CreateProduct;

using MediatR;
using ProductService.Application.Common.Interfaces;
using ProductService.Application.DTOs;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;

/// <summary>
/// Handler per CreateProductCommand.
/// Ogni Command ha il proprio Handler dedicato (Single Responsibility Principle).
/// </summary>
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly IProductCache _cache;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(
        IProductRepository repository,
        IProductCache cache,
        ILogger<CreateProductCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = new Product
        {
            Name = request.Name,
            Price = request.Price,
            Description = request.Description ?? string.Empty,
            IsActive = true
        };

        await _repository.AddAsync(product, ct);
        await _cache.SetProductAsync(product, ct);

        _logger.LogInformation("Prodotto creato: {ProductId} - {ProductName}", product.Id, product.Name);

        return new ProductDto(product.Id, product.Name, product.Price, product.Description, product.IsActive);
    }
}
