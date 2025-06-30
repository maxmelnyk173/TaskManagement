using TaskManagement.Common;
using TaskManagement.Features.Tasks.Contracts;
using TaskManagement.Features.Tasks.Entities;
using TaskManagement.Features.Tasks.Repositories;
using TaskManagement.Features.Users.Contracts;

namespace TaskManagement.Features.Tasks.Services;

public class TaskService(ITaskRepository repository, ITaskAssignmentService assignmentService) : ITaskService
{
    public async Task<TaskResponse[]> GetTasksAsync(int skip, int take, CancellationToken token)
    {
        var tasks = await repository.GetTasksAsync(skip, take, token);
        return [.. tasks.Select(MapToTaskResponse)];
    }

    public async Task<TaskWithAssignmentsResponse> GetTaskByIdAsync(long id, CancellationToken token)
    {
        var task = await repository.GetTaskByIdAsync(id, token)
            ?? throw new NotFoundException($"Task with ID {id} not found");

        return new TaskWithAssignmentsResponse(
            task.Id,
            task.Title,
            task.State.ToString(),
            [.. task.Assignments.Select(a => new TaskUserAssignmentResponse(a.UserId, a.User.Name))]
        );
    }

    public async Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await repository.TaskExistsByTitleAsync(request.Title, token))
        {
            throw new ConflictException($"Task with title '{request.Title}' already exists");
        }

        await using var transaction = await repository.BeginTransactionAsync(token);

        var newTask = new TaskItem { Title = request.Title, State = TaskState.Waiting };
        await repository.CreateTaskAsync(newTask, token);

        await assignmentService.AssignInitialTaskAsync(newTask, token);

        await transaction.CommitAsync(token);

        return MapToTaskResponse(newTask);
    }

    private static TaskResponse MapToTaskResponse(TaskItem task)
    {
        if (task == null)
            return null;

        return new TaskResponse(
            task.Id,
            task.Title,
            task.State.ToString(),
            task.AssignedUser == null ? null : new UserResponse(task.AssignedUser.Id, task.AssignedUser.Name)
        );
    }
}
