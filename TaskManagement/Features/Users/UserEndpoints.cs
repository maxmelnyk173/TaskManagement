using Microsoft.AspNetCore.Mvc;
using TaskManagement.Common;
using TaskManagement.Features.Users.Contracts;
using TaskManagement.Features.Users.Services;

namespace TaskManagement.Features.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/users")
            .WithTags("Users")
            .WithOpenApi();

        group.MapGet("/", GetUsers)
            .WithName(nameof(GetUsers))
            .WithSummary("Get all users")
            .Produces<UserResponse[]>(StatusCodes.Status200OK);

        group.MapPost("/", CreateUser)
            .WithName(nameof(CreateUser))
            .WithSummary("Create a new user")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces<ConflictException>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetUsers(
        [FromServices] IUserService service,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        CancellationToken token = default
    )
    {
        take = Math.Clamp(take, 0, 50);

        var result = await service.GetUsersAsync(skip, take, token);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateUser(
        [FromServices] IUserService service,
        [FromBody] CreateUserRequest request,
        CancellationToken token = default
    )
    {
        var result = await service.CreateUserAsync(request, token);
        return Results.Ok(result);
    }
}
