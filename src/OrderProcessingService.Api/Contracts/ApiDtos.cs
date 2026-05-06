using System.ComponentModel.DataAnnotations;
using OrderProcessingService.Api.Domain;

namespace OrderProcessingService.Api.Contracts;

public record OrderLineItemRequest(
    [Required] string ProductId,
    [Range(1, int.MaxValue)] int Quantity);

public record CreateOrderRequest(
    [Required] [MinLength(1)] IReadOnlyList<OrderLineItemRequest> Items);

public record PatchOrderStatusRequest(
    [Required] OrderStatus Status);

public record ProductResponse(
    string Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity);

public record OrderLineItemResponse(
    string ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record OrderResponse(
    string Id,
    IReadOnlyList<OrderLineItemResponse> Items,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record OrderCreatedEvent(
    string OrderId,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderLineItemResponse> Items);
