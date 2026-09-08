using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace HRM.EmployeeService.Infrastructure;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

/// <summary>
/// Thin Redis wrapper. Used only where it earns its keep: read-heavy data that changes
/// rarely (employee detail, dashboard stats). Never for lists that change every write.
/// </summary>
public class RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger) : ICacheService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var json = await cache.GetStringAsync(key, ct);
            return json is null ? default : JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            // A cache outage must degrade performance, not availability.
            logger.LogWarning(ex, "Cache read failed for {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            await cache.SetStringAsync(key, JsonSerializer.Serialize(value),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        }
        catch (Exception ex) { logger.LogWarning(ex, "Cache write failed for {Key}", key); }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try { await cache.RemoveAsync(key, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Cache invalidation failed for {Key}", key); }
    }
}

public static class CacheKeys
{
    public static string Employee(Guid id) => $"employee:{id}";
    public const string Stats = "employee:stats";
}
