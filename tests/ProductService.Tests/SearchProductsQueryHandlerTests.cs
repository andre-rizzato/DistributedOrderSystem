using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProductService.Application.Queries.SearchProducts;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using Xunit;

namespace ProductService.Tests;

public class SearchProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _repository = new();
    private readonly SearchProductsQueryHandler _sut;

    public SearchProductsQueryHandlerTests()
    {
        _sut = new SearchProductsQueryHandler(_repository.Object, NullLogger<SearchProductsQueryHandler>.Instance);
    }

    private void SetupRepository(params Product[] products)
        => _repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

    [Fact]
    public async Task Handle_SearchTermMatchesNameOrDescription_IsCaseInsensitive()
    {
        SetupRepository(
            new Product { Id = Guid.NewGuid(), Name = "Mechanical Keyboard", Description = "", Price = 10m, IsActive = true },
            new Product { Id = Guid.NewGuid(), Name = "Mouse", Description = "ergonomic KEYBOARD stand", Price = 10m, IsActive = true },
            new Product { Id = Guid.NewGuid(), Name = "Monitor", Description = "", Price = 10m, IsActive = true });

        var result = await _sut.Handle(new SearchProductsQuery(SearchTerm: "keyboard", IsActive: null), CancellationToken.None);

        result.Select(p => p.Name).Should().BeEquivalentTo("Mechanical Keyboard", "Mouse");
    }

    [Fact]
    public async Task Handle_FiltersByPriceRange()
    {
        SetupRepository(
            new Product { Id = Guid.NewGuid(), Name = "Cheap", Description = "", Price = 5m, IsActive = true },
            new Product { Id = Guid.NewGuid(), Name = "Mid", Description = "", Price = 50m, IsActive = true },
            new Product { Id = Guid.NewGuid(), Name = "Expensive", Description = "", Price = 500m, IsActive = true });

        var result = await _sut.Handle(
            new SearchProductsQuery(MinPrice: 10m, MaxPrice: 100m, IsActive: null), CancellationToken.None);

        result.Select(p => p.Name).Should().BeEquivalentTo("Mid");
    }

    [Fact]
    public async Task Handle_FiltersByIsActive()
    {
        SetupRepository(
            new Product { Id = Guid.NewGuid(), Name = "Active", Description = "", Price = 10m, IsActive = true },
            new Product { Id = Guid.NewGuid(), Name = "Inactive", Description = "", Price = 10m, IsActive = false });

        var result = await _sut.Handle(new SearchProductsQuery(IsActive: true), CancellationToken.None);

        result.Select(p => p.Name).Should().BeEquivalentTo("Active");
    }

    [Fact]
    public async Task Handle_DefaultQuery_OnlyReturnsActiveProducts()
    {
        SetupRepository(
            new Product { Id = Guid.NewGuid(), Name = "Active", Description = "", Price = 10m, IsActive = true },
            new Product { Id = Guid.NewGuid(), Name = "Inactive", Description = "", Price = 10m, IsActive = false });

        var result = await _sut.Handle(new SearchProductsQuery(), CancellationToken.None);

        result.Select(p => p.Name).Should().BeEquivalentTo("Active");
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsEverythingMappedToDto()
    {
        SetupRepository(
            new Product { Id = Guid.NewGuid(), Name = "A", Description = "", Price = 10m, IsActive = true },
            new Product { Id = Guid.NewGuid(), Name = "B", Description = "", Price = 10m, IsActive = false });

        var result = await _sut.Handle(new SearchProductsQuery(IsActive: null), CancellationToken.None);

        result.Should().HaveCount(2);
    }
}
