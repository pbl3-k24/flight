using API.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace API.Infrastructure.Caching;

public class InMemoryCacheService(IMemoryCache memoryCache) : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        memoryCache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        if (ttl.HasValue)
        {
            memoryCache.Set(key, value, ttl.Value);
        }
        else
        {
            memoryCache.Set(key, value);
        }

        return Task.CompletedTask;
    }
}
