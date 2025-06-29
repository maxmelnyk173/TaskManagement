using TaskManagement.Features.Tasks.Entities;

namespace TaskManagement.Features.Tasks.Services;

public interface ITaskAssignmentService
{
    Task AssignInitialTaskAsync(TaskItem task, CancellationToken token);

    Task ReassignTasksAsync(CancellationToken token);
}