using OrderProcessingService.Api.Domain;

namespace OrderProcessingService.Api.Services;

/// <summary>Valid forward/cancel transitions for the order lifecycle.</summary>
public static class OrderStatusTransitions
{
    public static bool TryGetNextValidStatuses(OrderStatus current, out IReadOnlyCollection<OrderStatus> allowed)
    {
        allowed = current switch
        {
            OrderStatus.Pending => new[] { OrderStatus.Confirmed, OrderStatus.Cancelled },
            OrderStatus.Confirmed => new[] { OrderStatus.Processing, OrderStatus.Cancelled },
            OrderStatus.Processing => new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
            OrderStatus.Shipped => new[] { OrderStatus.Delivered },
            OrderStatus.Delivered => Array.Empty<OrderStatus>(),
            OrderStatus.Cancelled => Array.Empty<OrderStatus>(),
            _ => Array.Empty<OrderStatus>()
        };
        return allowed.Count > 0;
    }

    public static bool IsTerminal(OrderStatus status) =>
        status is OrderStatus.Delivered or OrderStatus.Cancelled;

    public static bool IsCancellation(OrderStatus target) => target == OrderStatus.Cancelled;

    /// <summary>Returns false if the transition is not allowed.</summary>
    public static bool CanTransition(OrderStatus from, OrderStatus to, out string? error)
    {
        error = null;
        if (from == to)
        {
            error = "Order is already in the requested status.";
            return false;
        }

        TryGetNextValidStatuses(from, out var allowed);
        if (!allowed.Contains(to))
        {
            error = $"Cannot transition from {from} to {to}.";
            return false;
        }

        return true;
    }
}
