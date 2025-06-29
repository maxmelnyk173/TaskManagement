using Microsoft.Extensions.Caching.Memory;
using TaskManagement.Features.Users.Repositories;

namespace TaskManagement.Features.Users.Services;

public class UserCacheService(
    IServiceProvider serviceProvider,
    IMemoryCache memoryCache,
    ILogger<UserCacheService> logger
) : IUserCacheService
{
    private const string USER_IDS_CACHE_KEY = "available_user_ids";

    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(10);

    private static readonly TimeSpan AbsoluteExpiry = TimeSpan.FromMinutes(30);

    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<HashSet<long>> GetAvailableUserIdsAsync(CancellationToken token = default)
    {
        if (memoryCache.TryGetValue(USER_IDS_CACHE_KEY, out HashSet<long> cachedUserIds) && cachedUserIds != null)
        {
            logger.LogDebug("Retrieved {Count} user IDs from cache", cachedUserIds.Count);
            return cachedUserIds;
        }

        await _semaphore.WaitAsync(token);
        try
        {
            if (memoryCache.TryGetValue(USER_IDS_CACHE_KEY, out cachedUserIds) && cachedUserIds != null)
            {
                return cachedUserIds;
            }

            using var scope = serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            logger.LogDebug("Cache miss, loading user IDs from database");
            var userIds = await repository.GetUserIdsAsync(token);

            memoryCache.Set(USER_IDS_CACHE_KEY, userIds, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = AbsoluteExpiry,
                SlidingExpiration = CacheExpiry
            });
            logger.LogDebug("Cached {Count} user IDs", userIds.Count);

            return userIds;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task InvalidateCacheAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            memoryCache.Remove(USER_IDS_CACHE_KEY);
            logger.LogDebug("User cache invalidated");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task AddUserAsync(long userId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (memoryCache.TryGetValue(USER_IDS_CACHE_KEY, out HashSet<long> cachedUserIds) && cachedUserIds != null)
            {
                cachedUserIds.Add(userId);
                logger.LogDebug("Added user {UserId} to cache", userId);
            }
            else
            {
                logger.LogDebug("Cache not found, user {UserId} will be included on next cache refresh", userId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }
}