using TaskManagement.Features.Users.Repositories;
using TaskManagement.Features.Users.Services;

namespace TaskManagement.Features.Users;

public static class ServiceConfiguration
{
    public static void AddUsers(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IUserCacheService, UserCacheService>();
        services.AddScoped<IUserService, UserService>();
    }
}