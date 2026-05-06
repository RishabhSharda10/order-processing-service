using FluentAssertions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Moq;
using OrderProcessingService.Api.Abstractions;
using OrderProcessingService.Api.Configuration;
using OrderProcessingService.Api.Contracts;
using OrderProcessingService.Api.Domain;
using OrderProcessingService.Api.Services;

namespace OrderProcessingService.Tests;

public class OrderServiceTests
{
    private readonly RedisSettings _redis = new()
    {
        ConnectionString = "localhost:6379",
        OrderCacheTtlSeconds = 30,
        ProductCacheTtlSeconds = 60
    };

    [Fact]
    public async Task Create_returns_400_when_product_missing()
    {
        var products = new Mock<IProductRepository>();
        products.Setup(p => p.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var sut = CreateSut(products: products);

        var result = await sut.CreateAsync(
            new CreateOrderRequest(new[] { new OrderLineItemRequest("missing", 1) }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Create_rolls_back_prior_reservations_when_stock_runs_out()
    {
        var p1 = NewProduct("p1", stock: 5);
        var p2 = NewProduct("p2", stock: 1);

        var products = new Mock<IProductRepository>();
        products.Setup(p => p.GetByIdAsync("p1", It.IsAny<CancellationToken>())).ReturnsAsync(p1);
        products.Setup(p => p.GetByIdAsync("p2", It.IsAny<CancellationToken>())).ReturnsAsync(p2);

        products.SetupSequence(p => p.TryDecrementStockAsync("p1", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        products.Setup(p => p.TryDecrementStockAsync("p2", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        products.Setup(p => p.IncrementStockAsync("p1", 2, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var orders = new Mock<IOrderRepository>();

        var sut = CreateSut(products: products, orders: orders);

        var result = await sut.CreateAsync(
            new CreateOrderRequest(new[]
            {
                new OrderLineItemRequest("p1", 2),
                new OrderLineItemRequest("p2", 5)
            }),
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(409);

        products.Verify(p => p.IncrementStockAsync("p1", 2, It.IsAny<CancellationToken>()), Times.Once);
        orders.Verify(o => o.InsertAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_publishes_event_on_success()
    {
        var product = NewProduct("p1", stock: 10);
        var products = new Mock<IProductRepository>();
        products.Setup(p => p.GetByIdAsync("p1", It.IsAny<CancellationToken>())).ReturnsAsync(product);
        products.Setup(p => p.TryDecrementStockAsync("p1", 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var publisher = new Mock<IOrderEventPublisher>();
        publisher.Setup(p => p.PublishOrderCreatedAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut(products: products, publisher: publisher);

        var result = await sut.CreateAsync(
            new CreateOrderRequest(new[] { new OrderLineItemRequest("p1", 1) }),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        publisher.Verify(p => p.PublishOrderCreatedAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Cancel_restores_stock_for_each_line()
    {
        var order = new Order
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Status = OrderStatus.Pending,
            Items = new[]
            {
                new OrderLineItem { ProductId = "a", ProductName = "A", Quantity = 2, UnitPrice = 5 },
                new OrderLineItem { ProductId = "b", ProductName = "B", Quantity = 1, UnitPrice = 3 }
            },
            TotalAmount = 13,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var orders = new Mock<IOrderRepository>();
        orders.Setup(o => o.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        orders.Setup(o => o.ReplaceAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var products = new Mock<IProductRepository>();

        var sut = CreateSut(orders: orders, products: products);

        var result = await sut.PatchStatusAsync(
            order.Id,
            new PatchOrderStatusRequest(OrderStatus.Cancelled),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        products.Verify(p => p.IncrementStockAsync("a", 2, It.IsAny<CancellationToken>()), Times.Once);
        products.Verify(p => p.IncrementStockAsync("b", 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    private OrderService CreateSut(
        Mock<IProductRepository>? products = null,
        Mock<IOrderRepository>? orders = null,
        Mock<ICacheService>? cache = null,
        Mock<IOrderEventPublisher>? publisher = null)
    {
        products ??= new Mock<IProductRepository>();
        orders ??= new Mock<IOrderRepository>();
        cache ??= new Mock<ICacheService>();
        publisher ??= new Mock<IOrderEventPublisher>();

        orders.Setup(o => o.InsertAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<OrderResponse>(), It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        publisher.Setup(p => p.PublishOrderCreatedAsync(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new OrderService(
            orders.Object,
            products.Object,
            cache.Object,
            publisher.Object,
            Options.Create(_redis));
    }

    private static Product NewProduct(string id, int stock) =>
        new()
        {
            Id = id,
            Name = id,
            Description = "",
            Price = 10,
            StockQuantity = stock
        };
}
