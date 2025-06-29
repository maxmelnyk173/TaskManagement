using Moq;
using TaskManagement.Common;
using TaskManagement.Features.Users.Contracts;
using TaskManagement.Features.Users.Entities;
using TaskManagement.Features.Users.Repositories;
using TaskManagement.Features.Users.Services;

namespace TaskManagement.Test;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<IUserCacheService> _mockUserCache;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockUserCache = new Mock<IUserCacheService>();
        _userService = new UserService(_mockRepository.Object, _mockUserCache.Object);
    }

    [Fact]
    public async Task GetUsersAsync_ReturnsUserResponses()
    {
        var users = new[]
        {
            new User { Id = 1, Name = "John Doe" },
            new User { Id = 2, Name = "Jane Smith" }
        };

        _mockRepository.Setup(r => r.GetUsersAsync(0, 10, default)).ReturnsAsync(users);

        var result = await _userService.GetUsersAsync(0, 10, default);

        Assert.Equal(2, result.Length);
        Assert.Equal("John Doe", result[0].Name);
        Assert.Equal("Jane Smith", result[1].Name);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidRequest_ReturnsUserResponse()
    {
        var request = new CreateUserRequest("New User");

        _mockRepository.Setup(r => r.UserExistsByNameAsync("New User", default)).ReturnsAsync(false);
        _mockRepository.Setup(r => r.CreateUserAsync(It.IsAny<User>(), default))
                      .Callback<User, CancellationToken>((user, _) => user.Id = 1)
                      .Returns(Task.CompletedTask);

        var result = await _userService.CreateUserAsync(request, default);

        Assert.Equal(1, result.Id);
        Assert.Equal("New User", result.Name);
        _mockUserCache.Verify(c => c.AddUserAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateUserAsync_WithExistingName_ThrowsConflictException()
    {
        var request = new CreateUserRequest("Existing User");

        _mockRepository.Setup(r => r.UserExistsByNameAsync("Existing User", default)).ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _userService.CreateUserAsync(request, default));
        Assert.Contains("User with name 'Existing User' already exists", exception.Message);
    }

    [Fact]
    public async Task CreateUserAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _userService.CreateUserAsync(null, default));
    }
}