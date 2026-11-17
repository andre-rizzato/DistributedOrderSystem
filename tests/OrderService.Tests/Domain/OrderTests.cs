using FluentAssertions;
using OrderService.Domain.Aggregates;
using OrderService.Domain.Events;
using OrderService.Domain.Exceptions;
using OrderService.Domain.ValueObjects;
using Xunit;

namespace OrderService.Tests.Domain;

public class OrderTests
{
    private static (Guid productId, int quantity, decimal unitPrice) Item(
        decimal unitPrice = 10m, int quantity = 1) =>
        (Guid.NewGuid(), quantity, unitPrice);

    [Fact]
    public void Create_WithValidItems_StartsAsPendingAndCalculatesTotal()
    {
        var items = new[] { Item(unitPrice: 10m, quantity: 2), Item(unitPrice: 5m, quantity: 3) };

        var order = Order.Create(items);

        order.Status.Should().Be(OrderStatus.Pending);
        order.Total.Should().Be(new Money(35m));
        order.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Create_WithNoItems_ThrowsOrderDomainException()
    {
        var act = () => Order.Create(Array.Empty<(Guid, int, decimal)>());

        act.Should().Throw<OrderDomainException>()
            .WithMessage("An order must contain at least one item.");
    }

    [Fact]
    public void Create_WithInvalidItem_PropagatesOrderItemValidation()
    {
        var items = new[] { (Guid.NewGuid(), 0, 10m) };

        var act = () => Order.Create(items);

        act.Should().Throw<OrderDomainException>()
            .WithMessage("Quantity must be greater than zero.");
    }

    [Fact]
    public void Create_RaisesOrderCreatedDomainEvent()
    {
        var items = new[] { Item() };

        var order = Order.Create(items);

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCreatedDomainEvent>();
    }

    [Fact]
    public void ChangeStatus_WithAllowedTransition_UpdatesStatusAndRaisesEvent()
    {
        var order = Order.Create(new[] { Item() });
        order.ClearDomainEvents();

        order.ChangeStatus("Confirmed");

        order.Status.Should().Be(OrderStatus.Confirmed);
        var raised = order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderStatusChangedDomainEvent>().Subject;
        raised.OldStatus.Should().Be("Pending");
        raised.NewStatus.Should().Be("Confirmed");
    }

    [Fact]
    public void ChangeStatus_WithDisallowedTransition_ThrowsAndLeavesStatusUnchanged()
    {
        var order = Order.Create(new[] { Item() });

        var act = () => order.ChangeStatus("Shipped");

        act.Should().Throw<OrderDomainException>();
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void FullLifecycle_PendingToConfirmedToShippedToDelivered_Succeeds()
    {
        var order = Order.Create(new[] { Item() });

        order.ChangeStatus("Confirmed");
        order.ChangeStatus("Shipped");
        order.ChangeStatus("Delivered");

        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void ChangeStatus_FromDeliveredToAnything_Throws()
    {
        var order = Order.Create(new[] { Item() });
        order.ChangeStatus("Confirmed");
        order.ChangeStatus("Shipped");
        order.ChangeStatus("Delivered");

        var act = () => order.ChangeStatus("Cancelled");

        act.Should().Throw<OrderDomainException>();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllPendingEvents()
    {
        var order = Order.Create(new[] { Item() });

        order.ClearDomainEvents();

        order.DomainEvents.Should().BeEmpty();
    }
}
