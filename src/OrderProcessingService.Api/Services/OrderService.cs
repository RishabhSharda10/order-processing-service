using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using OrderProcessingService.Api.Abstractions;
using OrderProcessingService.Api.Configuration;
using OrderProcessingService.Api.Contracts;
using OrderProcessingService.Api.Domain;

namespace OrderProcessingService.Api.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ICacheService _cache;
    private readonly IOrderEventPublisher _publisher;
    private readonly RedisSettings _redis;

    public OrderService(
        IOrderRepository orders,
        IProductRepository products,
        ICacheService cache,
        IOrderEventPublisher publisher,
        IOptions<RedisSettings> redis)
    {
        _orders = orders;
        _products = products;
        _cache = cache;
        _publisher = publisher;
        _redis = redis.Value;
    }

    public async Task<OrderOperationResult> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var merged = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new OrderLineItemRequest(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        var reserved = new List<(string ProductId, int Qty)>();
        var builtLines = new List<OrderLineItem>();

        foreach (var line in merged)
        {
            var product = await _products.GetByIdAsync(line.ProductId, cancellationToken);
            if (product is null)
            {
                await ReleaseReservationsAsync(reserved, cancellationToken);
                return new OrderOperationResult(false, null, StatusCodes.Status400BadRequest,
                    $"Product '{line.ProductId}' was not found.");
            }

            var reservedOk = await _products.TryDecrementStockAsync(line.ProductId, line.Quantity, cancellationToken);
            if (!reservedOk)
            {
                await ReleaseReservationsAsync(reserved, cancellationToken);
                return new OrderOperationResult(false, null, StatusCodes.Status409Conflict,
                    $"Insufficient stock for product '{product.Name}' ({line.ProductId}).");
            }

            reserved.Add((line.ProductId, line.Quantity));
            builtLines.Add(new OrderLineItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = line.Quantity,
                UnitPrice = product.Price
            });
        }

        var total = builtLines.Sum(l => l.UnitPrice * l.Quantity);
        var now = DateTime.UtcNow;
        var order = new Order
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Items = builtLines,
            Status = OrderStatus.Pending,
            TotalAmount = total,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _orders.InsertAsync(order, cancellationToken);

        var response = Map(order);
        await _publisher.PublishOrderCreatedAsync(
            new OrderCreatedEvent(
                response.Id,
                response.Status,
                response.TotalAmount,
                response.CreatedAtUtc,
                response.Items),
            cancellationToken);

        await InvalidateProductCachesAsync(builtLines.Select(l => l.ProductId), cancellationToken);
        await _cache.SetAsync(
            CacheKeys.Order(order.Id),
            response,
            TimeSpan.FromSeconds(_redis.OrderCacheTtlSeconds),
            cancellationToken);

        return new OrderOperationResult(true, response, StatusCodes.Status201Created, null);
    }

    public async Task<OrderResponse?> GetByIdAsync(string orderId, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Order(orderId);
        var cached = await _cache.GetAsync<OrderResponse>(key, cancellationToken);
        if (cached is not null)
            return cached;

        var entity = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (entity is null)
            return null;

        var dto = Map(entity);
        await _cache.SetAsync(key, dto, TimeSpan.FromSeconds(_redis.OrderCacheTtlSeconds), cancellationToken);
        return dto;
    }

    public async Task<OrderOperationResult> PatchStatusAsync(
        string orderId,
        PatchOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (existing is null)
            return new OrderOperationResult(false, null, StatusCodes.Status404NotFound, "Order not found.");

        if (!OrderStatusTransitions.CanTransition(existing.Status, request.Status, out var error))
            return new OrderOperationResult(false, null, StatusCodes.Status409Conflict, error);

        if (OrderStatusTransitions.IsCancellation(request.Status))
        {
            foreach (var line in existing.Items)
                await _products.IncrementStockAsync(line.ProductId, line.Quantity, cancellationToken);

            await InvalidateProductCachesAsync(existing.Items.Select(i => i.ProductId), cancellationToken);
        }

        existing.Status = request.Status;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        var replaced = await _orders.ReplaceAsync(existing, cancellationToken);
        if (!replaced)
            return new OrderOperationResult(false, null, StatusCodes.Status409Conflict, "Order could not be updated.");

        await _cache.RemoveAsync(CacheKeys.Order(orderId), cancellationToken);

        var dto = Map(existing);
        await _cache.SetAsync(
            CacheKeys.Order(orderId),
            dto,
            TimeSpan.FromSeconds(_redis.OrderCacheTtlSeconds),
            cancellationToken);

        return new OrderOperationResult(true, dto, StatusCodes.Status200OK, null);
    }

    private async Task ReleaseReservationsAsync(List<(string ProductId, int Qty)> reserved, CancellationToken ct)
    {
        foreach (var (productId, qty) in reserved)
            await _products.IncrementStockAsync(productId, qty, ct);
    }

    private async Task InvalidateProductCachesAsync(IEnumerable<string> productIds, CancellationToken ct)
    {
        await _cache.RemoveAsync(CacheKeys.ProductsAll, ct);
        foreach (var id in productIds.Distinct())
            await _cache.RemoveAsync(CacheKeys.Product(id), ct);
    }

    private static OrderResponse Map(Order order) =>
        new(
            order.Id,
            order.Items.Select(i => new OrderLineItemResponse(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
                .ToList(),
            order.Status,
            order.TotalAmount,
            order.CreatedAtUtc,
            order.UpdatedAtUtc);
}
