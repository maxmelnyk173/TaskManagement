using TaskManagement.Features.Users.Entities;

namespace TaskManagement.Features.Users.Repositories;

public interface IUserRepository
{
    Task<User[]> GetUsersAsync(int skip, int take, CancellationToken token = default);

    Task<HashSet<long>> GetUserIdsAsync(CancellationToken token = default);

    Task<bool> UserExistsByNameAsync(string name, CancellationToken token = default);

    Task CreateUserAsync(User user, CancellationToken token = default);
}