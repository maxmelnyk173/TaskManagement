using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Features.Users.Entities;

namespace TaskManagement.Features.Users.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User[]> GetUsersAsync(int skip, int take, CancellationToken token = default)
    {
        return db.Users.Skip(skip).Take(take).ToArrayAsync(token);
    }

    public Task<HashSet<long>> GetUserIdsAsync(CancellationToken token = default)
    {
        return db.Users.Select(u => u.Id).ToHashSetAsync(token);
    }

    public async Task CreateUserAsync(User user, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await db.Users.AddAsync(user, token);
        await db.SaveChangesAsync(token);
    }

    public Task<bool> UserExistsByNameAsync(string name, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return db.Users.AnyAsync(u => u.Name.ToLower() == name.ToLower(), token);
    }
}