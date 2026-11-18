using FluentAssertions;
using Moq;
using ProductService.Application.Common.Interfaces;
using ProductService.Application.Queries.GetProductById;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using Xunit;

namespace ProductService.Tests;

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IProductCache> _cache = new();
    private readonly GetProductByIdQueryHandler _sut;

    public GetProductByIdQueryHandlerTests()
    {
        _sut = new GetProductByIdQueryHandler(_repository.Object, _cache.Object);
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedProductWithoutHittingRepository()
    {
        var id = Guid.NewGuid();
        var cached = new Product { Id = id, Name = "Cached", Price = 10m, Description = "d", IsActive = true };
        _cache.Setup(c => c.GetProductByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(cached);

        var result = await _sut.Handle(new GetProductByIdQuery(id), CancellationToken.None);

        result!.Name.Should().Be("Cached");
        _repository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CacheMiss_FallsBackToRepositoryAndPopulatesCache()
    {
        var id = Guid.NewGuid();
        var fromDb = new Product { Id = id, Name = "FromDb", Price = 20m, Description = "d", IsActive = true };
        _cache.Setup(c => c.GetProductByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(fromDb);

        var result = await _sut.Handle(new GetProductByIdQuery(id), CancellationToken.None);

        result!.Name.Should().Be("FromDb");
        _cache.Verify(c => c.SetProductAsync(
            It.Is<Product>(p => p.Id == id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFoundAnywhere_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _cache.Setup(c => c.GetProductByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var result = await _sut.Handle(new GetProductByIdQuery(id), CancellationToken.None);

        result.Should().BeNull();
        _cache.Verify(c => c.SetProductAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
