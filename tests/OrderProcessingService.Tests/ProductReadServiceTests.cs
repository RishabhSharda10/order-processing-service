using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using OrderProcessingService.Api.Abstractions;
using OrderProcessingService.Api.Configuration;
using OrderProcessingService.Api.Contracts;
using OrderProcessingService.Api.Domain;
using OrderProcessingService.Api.Services;

namespace OrderProcessingService.Tests;

public class ProductReadServiceTests
{
    [Fact]
    public async Task GetAll_returns_cached_payload_without_hitting_repository()
    {
        var cached = new List<ProductResponse>
        {
            new("1", "A", "", 1, 1)
        };

        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<List<ProductResponse>>(CacheKeys.ProductsAll, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var repo = new Mock<IProductRepository>();

        var sut = new ProductReadService(
            repo.Object,
            cache.Object,
            Options.Create(new RedisSettings()));

        var result = await sut.GetAllAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        repo.Verify(p => p.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetById_returns_null_when_missing_and_sets_no_cache()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(c => c.GetAsync<ProductResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponse?)null);

        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetByIdAsync("x", It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        var sut = new ProductReadService(repo.Object, cache.Object, Options.Create(new RedisSettings()));

        var result = await sut.GetByIdAsync("x", CancellationToken.None);

        result.Should().BeNull();
        cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<ProductResponse>(), It.IsAny<TimeSpan>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
