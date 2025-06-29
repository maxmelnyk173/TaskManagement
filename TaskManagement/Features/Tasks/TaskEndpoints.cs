using Microsoft.AspNetCore.Mvc;
using TaskManagement.Common;
using TaskManagement.Features.Tasks.Contracts;
using TaskManagement.Features.Tasks.Services;

namespace TaskManagement.Features.Tasks;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/tasks")
            .WithTags("Tasks")
            .WithOpenApi();

        group.MapGet("/", GetTasks)
            .WithName(nameof(GetTasks))
            .WithSummary("Get all tasks")
            .Produces<TaskResponse[]>(StatusCodes.Status200OK);

        group.MapGet("/{id:long}", GetTaskById)
            .WithName(nameof(GetTaskById))
            .WithSummary("Get a task by ID")
            .Produces<TaskWithAssignmentsResponse>(StatusCodes.Status200OK)
            .Produces<NotFoundException>(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateTask)
            .WithName(nameof(CreateTask))
            .WithSummary("Create a new task")
            .Produces<TaskResponse>(StatusCodes.Status200OK)
            .Produces<ConflictException>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetTasks(
        [FromServices] ITaskService service,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        CancellationToken token = default
    )
    {
        take = Math.Clamp(take, 0, 50);

        var result = await service.GetTasksAsync(skip, take, token);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetTaskById(
        long id,
        [FromServices] ITaskService service,
        CancellationToken token = default
    )
    {
        var result = await service.GetTaskByIdAsync(id, token);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateTask(
        [FromServices] ITaskService service,
        [FromBody] CreateTaskRequest request,
        CancellationToken token = default
    )
    {
        var result = await service.CreateTaskAsync(request, token);
        return Results.Ok(result);
    }
}
