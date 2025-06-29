using Microsoft.EntityFrameworkCore.Storage;
using TaskManagement.Features.Tasks.Entities;
using TaskManagement.Features.Tasks.Models;

namespace TaskManagement.Features.Tasks.Repositories;

public interface ITaskRepository
{
    Task<TaskItem[]> GetTasksAsync(int skip, int take, CancellationToken token = default);

    Task<TaskItem> GetTaskByIdAsync(long id, CancellationToken token = default);

    Task<TaskReassignmentInfo[]> GetTasksForReassignmentAsync(int skip, int take, int maxHistoryCheck, CancellationToken token = default);

    Task<bool> TaskExistsByTitleAsync(string title, CancellationToken token = default);

    Task CreateTaskAsync(TaskItem task, CancellationToken token = default);

    Task CreateTaskAssignmentAsync(TaskAssignment assignment, CancellationToken token = default);

    Task CreateTaskAssignmentsAsync(List<TaskAssignment> assignments, CancellationToken token = default);

    Task<int> SetTasksToWaitingAsync(CancellationToken token = default);

    Task<int> MarkTasksAsCompletedAsync(HashSet<long> userIds, CancellationToken token = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default);

    Task SaveChangesAsync(CancellationToken token = default);
}