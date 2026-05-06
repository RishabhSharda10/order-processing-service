using FluentAssertions;
using OrderProcessingService.Api.Domain;
using OrderProcessingService.Api.Services;

namespace OrderProcessingService.Tests;

public class OrderStatusTransitionsTests
{
    [Fact]
    public void Pending_to_Confirmed_is_allowed()
    {
        OrderStatusTransitions.CanTransition(OrderStatus.Pending, OrderStatus.Confirmed, out var error).Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void Pending_to_Processing_is_rejected()
    {
        OrderStatusTransitions.CanTransition(OrderStatus.Pending, OrderStatus.Processing, out var error).Should().BeFalse();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Shipped_to_Delivered_is_allowed_and_cancel_is_not()
    {
        OrderStatusTransitions.CanTransition(OrderStatus.Shipped, OrderStatus.Delivered, out _).Should().BeTrue();
        OrderStatusTransitions.CanTransition(OrderStatus.Shipped, OrderStatus.Cancelled, out var err).Should().BeFalse();
        err.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Delivered_is_terminal()
    {
        OrderStatusTransitions.TryGetNextValidStatuses(OrderStatus.Delivered, out var allowed);
        allowed.Should().BeEmpty();
    }
}
