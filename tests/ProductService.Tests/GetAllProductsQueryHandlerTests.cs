using FluentAssertions;
using Moq;
using ProductService.Application.Queries.GetAllProducts;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using Xunit;

namespace ProductService.Tests;

public class GetAllProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly GetAllProductsQueryHandler _sut;

    public GetAllProductsQueryHandlerTests()
    {
        _sut = new GetAllProductsQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_MapsEveryProductFromRepositoryToADto()
    {
        var products = new[]
        {
            new Product { Id = Guid.NewGuid(), Name = "A", Price = 1m, Description = "a", IsActive = true },
            new Product { Id = Guid.NewGuid(), Name = "B", Price = 2m, Description = "b", IsActive = false }
        };
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        var result = (await _sut.Handle(new GetAllProductsQuery(), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(d => d.Name == "A" && d.IsActive);
        result.Should().Contain(d => d.Name == "B" && !d.IsActive);
    }

    [Fact]
    public async Task Handle_NoProducts_ReturnsEmptyCollection()
    {
        _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<Product>());

        var result = await _sut.Handle(new GetAllProductsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
