using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.Features.Users.Repositories;
using TaskManagement.Features.Users.Services;

namespace TaskManagement.Test;

public class UserCacheServiceTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<IServiceProvider> _mockScopeServiceProvider;
    private readonly Mock<ILogger<UserCacheService>> _mockLogger;
    private readonly IMemoryCache _memoryCache;
    private readonly UserCacheService _userCacheService;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;

    public UserCacheServiceTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopeServiceProvider = new Mock<IServiceProvider>();
        _mockRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserCacheService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _mockScopeFactory = new Mock<IServiceScopeFactory>();

        _mockScope.Setup(s => s.ServiceProvider).Returns(_mockScopeServiceProvider.Object);
        _mockScopeServiceProvider.Setup(sp => sp.GetService(typeof(IUserRepository))).Returns(_mockRepository.Object);
        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(_mockScopeFactory.Object);

        _userCacheService = new UserCacheService(_mockServiceProvider.Object, _memoryCache, _mockLogger.Object);
    }

    [Fact]
    public async Task GetAvailableUserIdsAsync_FirstCall_LoadsFromDatabase()
    {
        var userIds = new HashSet<long> { 1, 2, 3 };

        _mockRepository.Setup(r => r.GetUserIdsAsync(default)).ReturnsAsync(userIds);

        var result = await _userCacheService.GetAvailableUserIdsAsync();

        Assert.Equal(userIds, result);
        _mockRepository.Verify(r => r.GetUserIdsAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetAvailableUserIdsAsync_SubsequentCall_ReturnsFromCache()
    {

        var userIds = new HashSet<long> { 1, 2, 3 };

        _mockRepository.Setup(r => r.GetUserIdsAsync(default)).ReturnsAsync(userIds);

        await _userCacheService.GetAvailableUserIdsAsync();

        var result = await _userCacheService.GetAvailableUserIdsAsync();

        Assert.Equal(userIds, result);
        _mockRepository.Verify(r => r.GetUserIdsAsync(default), Times.Once);
    }

    [Fact]
    public async Task AddUserAsync_WithCachedData_AddsToCache()
    {
        var userIds = new HashSet<long> { 1, 2 };

        _mockRepository.Setup(r => r.GetUserIdsAsync(default)).ReturnsAsync(userIds);

        await _userCacheService.GetAvailableUserIdsAsync();
        await _userCacheService.AddUserAsync(3);

        var result = await _userCacheService.GetAvailableUserIdsAsync();

        Assert.Contains(3, result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task InvalidateCacheAsync_RemovesCachedData()
    {
        var userIds = new HashSet<long> { 1, 2 };

        _mockRepository.Setup(r => r.GetUserIdsAsync(default)).ReturnsAsync(userIds);

        await _userCacheService.GetAvailableUserIdsAsync();
        await _userCacheService.InvalidateCacheAsync();

        await _userCacheService.GetAvailableUserIdsAsync();

        _mockRepository.Verify(r => r.GetUserIdsAsync(default), Times.Exactly(2));
    }
}