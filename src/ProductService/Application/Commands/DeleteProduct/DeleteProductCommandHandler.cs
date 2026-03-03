namespace ProductService.Application.Commands.DeleteProduct;

using MediatR;
using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Interfaces;

/// <summary>
/// Handler per DeleteProductCommand.
/// </summary>
public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductRepository _repository;
    private readonly IProductCache _cache;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        IProductRepository repository,
        IProductCache cache,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken ct)
    {
        var deleted = await _repository.DeleteAsync(request.Id, ct);
        if (deleted)
        {
            await _cache.RemoveProductAsync(request.Id, ct);
            _logger.LogInformation("Prodotto eliminato: {ProductId}", request.Id);
        }
        return deleted;
    }
}
