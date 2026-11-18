using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductService.Application.Commands.DeleteProduct;
using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Interfaces;
using Xunit;

namespace ProductService.Tests;

public class DeleteProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IProductCache> _cache = new();
    private readonly DeleteProductCommandHandler _sut;

    public DeleteProductCommandHandlerTests()
    {
        _sut = new DeleteProductCommandHandler(
            _repository.Object, _cache.Object, NullLogger<DeleteProductCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ProductDeleted_RemovesFromCacheAndReturnsTrue()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _sut.Handle(new DeleteProductCommand(id), CancellationToken.None);

        result.Should().BeTrue();
        _cache.Verify(c => c.RemoveProductAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_DoesNotTouchCacheAndReturnsFalse()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.DeleteAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _sut.Handle(new DeleteProductCommand(id), CancellationToken.None);

        result.Should().BeFalse();
        _cache.Verify(c => c.RemoveProductAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
