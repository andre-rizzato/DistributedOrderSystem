using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductService.Application.Commands.CreateProduct;
using ProductService.Application.Common.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using Xunit;

namespace ProductService.Tests;

public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly Mock<IProductCache> _cache = new();
    private readonly CreateProductCommandHandler _sut;

    public CreateProductCommandHandlerTests()
    {
        _sut = new CreateProductCommandHandler(
            _repository.Object, _cache.Object, NullLogger<CreateProductCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PersistsProductAsActiveAndPopulatesCache()
    {
        var command = new CreateProductCommand("Keyboard", 99.90m, "Mechanical");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Name.Should().Be("Keyboard");
        result.Price.Should().Be(99.90m);
        result.Description.Should().Be("Mechanical");
        result.IsActive.Should().BeTrue();
        _repository.Verify(r => r.AddAsync(
            It.Is<Product>(p => p.Name == "Keyboard" && p.IsActive),
            It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.SetProductAsync(
            It.Is<Product>(p => p.Name == "Keyboard"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullDescription_DefaultsToEmptyString()
    {
        var command = new CreateProductCommand("Mouse", 49.90m, null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Description.Should().Be(string.Empty);
    }

}
