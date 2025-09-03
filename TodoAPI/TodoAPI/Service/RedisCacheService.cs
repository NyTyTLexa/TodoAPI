using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
namespace TodoAPI.Service;


public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string cacheKey)
    {
        var cachedData = await _cache.GetStringAsync(cacheKey);
        if (string.IsNullOrEmpty(cachedData))
            return default;

        return JsonSerializer.Deserialize<T>(cachedData);
    }

    public async Task SetAsync<T>(string cacheKey, T value,
        TimeSpan? absoluteExpirationRelativeToNow = null)
    {
        var serialized = JsonSerializer.Serialize(value);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow ??
            TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(cacheKey, serialized, options);
    }

    public async Task RemoveAsync(string cacheKey)
    {
        await _cache.RemoveAsync(cacheKey);
    }
}
