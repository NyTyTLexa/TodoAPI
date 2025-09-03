namespace TodoAPI.Service
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string cacheKey);
        Task SetAsync<T>(string cacheKey, T value,
            TimeSpan? absoluteExpirationRelativeToNow = null);
        Task RemoveAsync(string cacheKey);
    }
}
