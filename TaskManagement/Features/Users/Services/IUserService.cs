using TaskManagement.Features.Users.Contracts;

namespace TaskManagement.Features.Users.Services;

public interface IUserService
{
    Task<UserResponse[]> GetUsersAsync(int skip, int take, CancellationToken token);

    Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken token);
}
