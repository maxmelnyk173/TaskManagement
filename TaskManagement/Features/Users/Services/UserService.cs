using TaskManagement.Common;
using TaskManagement.Features.Users.Contracts;
using TaskManagement.Features.Users.Entities;
using TaskManagement.Features.Users.Repositories;

namespace TaskManagement.Features.Users.Services;

public class UserService(IUserRepository repository, IUserCacheService userCache) : IUserService
{
    public async Task<UserResponse[]> GetUsersAsync(int skip, int take, CancellationToken token)
    {
        var users = await repository.GetUsersAsync(skip, take, token);
        return [.. users.Select(u => new UserResponse(u.Id, u.Name))];
    }

    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await repository.UserExistsByNameAsync(request.Name, token))
        {
            throw new ConflictException($"User with name '{request.Name}' already exists");
        }

        var newUser = new User { Name = request.Name };
        await repository.CreateUserAsync(newUser, token);

        await userCache.AddUserAsync(newUser.Id);

        return new UserResponse(newUser.Id, newUser.Name);
    }
}