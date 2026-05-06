using System.Text.Json;
using OrderProcessingService.Api.Abstractions;
using StackExchange.Redis;

namespace OrderProcessingService.Api.Infrastructure.Redis;

public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _mux;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public RedisCacheService(IConnectionMultiplexer mux)
    {
        _mux = mux;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) where T : class
    {
        var db = _mux.GetDatabase();
        var val = await db.StringGetAsync(key).ConfigureAwait(false);
        if (!val.HasValue)
            return null;

        return JsonSerializer.Deserialize<T>(val!, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        where T : class
    {
        var db = _mux.GetDatabase();
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await db.StringSetAsync(key, json, ttl).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        var db = _mux.GetDatabase();
        await db.KeyDeleteAsync(key).ConfigureAwait(false);
    }
}
