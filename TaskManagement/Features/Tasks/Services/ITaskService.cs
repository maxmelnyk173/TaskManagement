using TaskManagement.Features.Tasks.Contracts;

namespace TaskManagement.Features.Tasks.Services;

public interface ITaskService
{
    Task<TaskResponse[]> GetTasksAsync(int skip, int take, CancellationToken token);

    Task<TaskWithAssignmentsResponse> GetTaskByIdAsync(long id, CancellationToken token);

    Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken token);
}
