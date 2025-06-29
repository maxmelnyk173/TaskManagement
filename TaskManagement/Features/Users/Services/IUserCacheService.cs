namespace TaskManagement.Features.Users.Services;

public interface IUserCacheService
{
    Task<HashSet<long>> GetAvailableUserIdsAsync(CancellationToken token = default);

    Task AddUserAsync(long userId);

    Task InvalidateCacheAsync();
}