using FluentAssertions;
using InventoryService.Cache.Interfaces;
using InventoryService.Data;
using InventoryService.Models;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace InventoryService.Tests;

public class InventoryWorkerServiceTests
{
    private static InventoryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InventoryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InventoryContext(options);
    }

    private static InventoryWorkerService CreateSut(
        InventoryContext context, Mock<IInventoryCache> cache)
        => new(context, cache.Object, NullLogger<InventoryWorkerService>.Instance);

    [Fact]
    public async Task AdjustInventoryQuantityAsync_ItemNotFound_ReturnsFalse()
    {
        using var context = CreateContext();
        var cache = new Mock<IInventoryCache>();
        var sut = CreateSut(context, cache);

        var result = await sut.AdjustInventoryQuantityAsync(Guid.NewGuid(), -1);

        result.Should().BeFalse();
        cache.Verify(c => c.SetInventoryItemAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdjustInventoryQuantityAsync_SufficientStock_DecrementsAndUpdatesCache()
    {
        using var context = CreateContext();
        var productId = Guid.NewGuid();
        context.InventoryItems.Add(new InventoryItem { ProductId = productId, AvailableQuantity = 10, ReservedQuantity = 0 });
        await context.SaveChangesAsync();
        var cache = new Mock<IInventoryCache>();
        var sut = CreateSut(context, cache);

        var result = await sut.AdjustInventoryQuantityAsync(productId, -4);

        result.Should().BeTrue();
        var item = await context.InventoryItems.FirstAsync(i => i.ProductId == productId);
        item.AvailableQuantity.Should().Be(6);
        cache.Verify(c => c.SetInventoryItemAsync(
            It.Is<InventoryItem>(i => i.ProductId == productId && i.AvailableQuantity == 6),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AdjustInventoryQuantityAsync_InsufficientStock_ReturnsFalseAndLeavesQuantityUnchanged()
    {
        using var context = CreateContext();
        var productId = Guid.NewGuid();
        context.InventoryItems.Add(new InventoryItem { ProductId = productId, AvailableQuantity = 2, ReservedQuantity = 0 });
        await context.SaveChangesAsync();
        var cache = new Mock<IInventoryCache>();
        var sut = CreateSut(context, cache);

        var result = await sut.AdjustInventoryQuantityAsync(productId, -5);

        result.Should().BeFalse();
        var item = await context.InventoryItems.FirstAsync(i => i.ProductId == productId);
        item.AvailableQuantity.Should().Be(2);
        cache.Verify(c => c.SetInventoryItemAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdjustInventoryQuantityAsync_PositiveDelta_Increments()
    {
        using var context = CreateContext();
        var productId = Guid.NewGuid();
        context.InventoryItems.Add(new InventoryItem { ProductId = productId, AvailableQuantity = 3, ReservedQuantity = 0 });
        await context.SaveChangesAsync();
        var cache = new Mock<IInventoryCache>();
        var sut = CreateSut(context, cache);

        var result = await sut.AdjustInventoryQuantityAsync(productId, 7);

        result.Should().BeTrue();
        var item = await context.InventoryItems.FirstAsync(i => i.ProductId == productId);
        item.AvailableQuantity.Should().Be(10);
    }

    [Fact]
    public async Task GetInventoryByProductIdAsync_CacheHit_ReturnsCachedItemWithoutTouchingCacheAgain()
    {
        using var context = CreateContext();
        var productId = Guid.NewGuid();
        var cachedItem = new InventoryItem { ProductId = productId, AvailableQuantity = 42 };
        var cache = new Mock<IInventoryCache>();
        cache.Setup(c => c.GetInventoryItemByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedItem);
        var sut = CreateSut(context, cache);

        var result = await sut.GetInventoryByProductIdAsync(productId);

        result.Should().BeSameAs(cachedItem);
        cache.Verify(c => c.SetInventoryItemAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetInventoryByProductIdAsync_CacheMiss_FallsBackToDbAndPopulatesCache()
    {
        using var context = CreateContext();
        var productId = Guid.NewGuid();
        context.InventoryItems.Add(new InventoryItem { ProductId = productId, AvailableQuantity = 15 });
        await context.SaveChangesAsync();
        var cache = new Mock<IInventoryCache>();
        cache.Setup(c => c.GetInventoryItemByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);
        var sut = CreateSut(context, cache);

        var result = await sut.GetInventoryByProductIdAsync(productId);

        result.Should().NotBeNull();
        result!.AvailableQuantity.Should().Be(15);
        cache.Verify(c => c.SetInventoryItemAsync(
            It.Is<InventoryItem>(i => i.ProductId == productId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInventoryByProductIdAsync_NotFoundAnywhere_ReturnsNull()
    {
        using var context = CreateContext();
        var productId = Guid.NewGuid();
        var cache = new Mock<IInventoryCache>();
        cache.Setup(c => c.GetInventoryItemByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);
        var sut = CreateSut(context, cache);

        var result = await sut.GetInventoryByProductIdAsync(productId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetInventoryQuantityAsync_ItemDoesNotExist_CreatesNewItem()
    {
        using var context = CreateContext();
        var productId = Guid.NewGuid();
        var cache = new Mock<IInventoryCache>();
        var sut = CreateSut(context, cache);

        var result = await sut.SetInventoryQuantityAsync(productId, 25);

        result.Should().NotBeNull();
        result!.ProductId.Should().Be(productId);
        result.AvailableQuantity.Should().Be(25);
        (await context.InventoryItems.CountAsync()).Should().Be(1);
        cache.Verify(c => c.SetInventoryItemAsync(
            It.Is<InventoryItem>(i => i.ProductId == productId && i.AvailableQuantity == 25),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetInventoryQuantityAsync_ItemExists_OverwritesQuantity()
    {
        using var context = CreateContext();
        var productId = Guid.NewGuid();
        context.InventoryItems.Add(new InventoryItem { ProductId = productId, AvailableQuantity = 5, ReservedQuantity = 2 });
        await context.SaveChangesAsync();
        var cache = new Mock<IInventoryCache>();
        var sut = CreateSut(context, cache);

        var result = await sut.SetInventoryQuantityAsync(productId, 99);

        result!.AvailableQuantity.Should().Be(99);
        (await context.InventoryItems.CountAsync()).Should().Be(1);
    }
}
