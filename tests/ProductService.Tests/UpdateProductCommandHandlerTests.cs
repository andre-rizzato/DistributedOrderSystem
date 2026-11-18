using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductService.Application.Commands.UpdateProduct;
using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using Xunit;

namespace ProductService.Tests;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IProductCache> _cache = new();
    private readonly UpdateProductCommandHandler _sut;

    public UpdateProductCommandHandlerTests()
    {
        _sut = new UpdateProductCommandHandler(
            _repository.Object, _cache.Object, NullLogger<UpdateProductCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNullAndDoesNotTouchCache()
    {
        var id = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);
        var command = new UpdateProductCommand(id, "Keyboard", 10m, null, true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().BeNull();
        _cache.Verify(c => c.SetProductAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductFound_UpdatesFieldsPersistsAndRefreshesCache()
    {
        var id = Guid.NewGuid();
        var existing = new Product { Id = id, Name = "Old", Price = 1m, Description = "Old desc", IsActive = false };
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var command = new UpdateProductCommand(id, "New", 50m, "New desc", true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("New");
        result.Price.Should().Be(50m);
        result.Description.Should().Be("New desc");
        result.IsActive.Should().BeTrue();
        _repository.Verify(r => r.UpdateAsync(
            It.Is<Product>(p => p.Id == id && p.Name == "New"),
            It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetProductAsync(
            It.Is<Product>(p => p.Id == id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullDescription_DefaultsToEmptyString()
    {
        var id = Guid.NewGuid();
        var existing = new Product { Id = id, Name = "Old", Price = 1m, Description = "Old desc", IsActive = true };
        _repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var command = new UpdateProductCommand(id, "New", 50m, null, true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result!.Description.Should().Be(string.Empty);
    }
}
