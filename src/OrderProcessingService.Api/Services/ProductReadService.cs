using Microsoft.Extensions.Options;
using OrderProcessingService.Api.Abstractions;
using OrderProcessingService.Api.Configuration;
using OrderProcessingService.Api.Contracts;
using OrderProcessingService.Api.Domain;

namespace OrderProcessingService.Api.Services;

public class ProductReadService : IProductReadService
{
    private readonly IProductRepository _products;
    private readonly ICacheService _cache;
    private readonly RedisSettings _redis;

    public ProductReadService(
        IProductRepository products,
        ICacheService cache,
        IOptions<RedisSettings> redis)
    {
        _products = products;
        _cache = cache;
        _redis = redis.Value;
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync<List<ProductResponse>>(CacheKeys.ProductsAll, cancellationToken);
        if (cached is not null)
            return cached;

        var entities = await _products.GetAllAsync(cancellationToken);
        var mapped = entities.Select(Map).ToList();
        await _cache.SetAsync(
            CacheKeys.ProductsAll,
            mapped,
            TimeSpan.FromSeconds(_redis.ProductCacheTtlSeconds),
            cancellationToken);

        return mapped;
    }

    public async Task<ProductResponse?> GetByIdAsync(string productId, CancellationToken cancellationToken)
    {
        var key = CacheKeys.Product(productId);
        var cached = await _cache.GetAsync<ProductResponse>(key, cancellationToken);
        if (cached is not null)
            return cached;

        var entity = await _products.GetByIdAsync(productId, cancellationToken);
        if (entity is null)
            return null;

        var dto = Map(entity);
        await _cache.SetAsync(key, dto, TimeSpan.FromSeconds(_redis.ProductCacheTtlSeconds), cancellationToken);
        return dto;
    }

    private static ProductResponse Map(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.StockQuantity);
}
