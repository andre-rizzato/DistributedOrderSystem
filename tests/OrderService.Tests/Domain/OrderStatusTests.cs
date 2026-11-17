using FluentAssertions;
using OrderService.Domain.Exceptions;
using OrderService.Domain.ValueObjects;
using Xunit;

namespace OrderService.Tests.Domain;

public class OrderStatusTests
{
    [Theory]
    [InlineData("Pending")]
    [InlineData("Confirmed")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    public void From_WithValidValue_ReturnsStatusWithThatValue(string value)
    {
        var status = OrderStatus.From(value);

        status.Value.Should().Be(value);
    }

    [Fact]
    public void From_WithInvalidValue_ThrowsOrderDomainException()
    {
        var act = () => OrderStatus.From("NotARealStatus");

        act.Should().Throw<OrderDomainException>()
            .WithMessage("*Invalid order status*");
    }

    [Theory]
    [InlineData("Pending", "Confirmed")]
    [InlineData("Pending", "Cancelled")]
    [InlineData("Confirmed", "Shipped")]
    [InlineData("Confirmed", "Cancelled")]
    [InlineData("Shipped", "Delivered")]
    public void TransitionTo_WithAllowedTransition_ReturnsNewStatus(string from, string to)
    {
        var status = OrderStatus.From(from);

        var result = status.TransitionTo(to);

        result.Value.Should().Be(to);
    }

    [Theory]
    [InlineData("Pending", "Shipped")]
    [InlineData("Pending", "Delivered")]
    [InlineData("Confirmed", "Pending")]
    [InlineData("Confirmed", "Delivered")]
    [InlineData("Shipped", "Pending")]
    [InlineData("Shipped", "Cancelled")]
    [InlineData("Delivered", "Cancelled")]
    [InlineData("Delivered", "Pending")]
    [InlineData("Cancelled", "Pending")]
    [InlineData("Cancelled", "Confirmed")]
    public void TransitionTo_WithDisallowedTransition_ThrowsOrderDomainException(string from, string to)
    {
        var status = OrderStatus.From(from);

        var act = () => status.TransitionTo(to);

        act.Should().Throw<OrderDomainException>()
            .WithMessage($"Transition not allowed from '{from}' to '{to}'.");
    }

    [Fact]
    public void Equality_IsBasedOnValue_NotReferenceIdentity()
    {
        var a = OrderStatus.From("Confirmed");
        var b = OrderStatus.From("Confirmed");

        (a == b).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void Equality_WithDifferentValues_IsFalse()
    {
        var a = OrderStatus.Pending;
        var b = OrderStatus.Confirmed;

        (a == b).Should().BeFalse();
    }
}
