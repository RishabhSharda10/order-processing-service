using OrderProcessingService.Api.Contracts;

namespace OrderProcessingService.Api.Abstractions;

public interface IOrderService
{
    Task<OrderOperationResult> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken);

    Task<OrderResponse?> GetByIdAsync(string orderId, CancellationToken cancellationToken);

    Task<OrderOperationResult> PatchStatusAsync(
        string orderId,
        PatchOrderStatusRequest request,
        CancellationToken cancellationToken);
}

public readonly record struct OrderOperationResult(
    bool Success,
    OrderResponse? Order,
    int StatusCode,
    string? Error);
