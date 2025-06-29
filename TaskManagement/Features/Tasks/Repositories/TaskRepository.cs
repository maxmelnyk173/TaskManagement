using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TaskManagement.Data;
using TaskManagement.Features.Tasks.Entities;
using TaskManagement.Features.Tasks.Models;

namespace TaskManagement.Features.Tasks.Repositories;

public class TaskRepository(AppDbContext db) : ITaskRepository
{
    public Task<TaskItem[]> GetTasksAsync(int skip, int take, CancellationToken token = default)
    {
        return db.Tasks.Include(e => e.AssignedUser)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(token);
    }

    public Task<TaskItem> GetTaskByIdAsync(long id, CancellationToken token = default)
    {
        return db.Tasks
            .Include(t => t.AssignedUser)
            .Include(t => t.Assignments)
                .ThenInclude(a => a.User)
            .FirstOrDefaultAsync(t => t.Id == id, token);
    }

    public Task<TaskReassignmentInfo[]> GetTasksForReassignmentAsync(int skip, int take, int maxHistoryCheck, CancellationToken token = default)
    {
        return db.Tasks
            .Where(t => t.State == TaskState.InProgress || t.State == TaskState.Waiting)
            .OrderBy(t => t.Id)
            .Skip(skip)
            .Take(take)
            .Select(t => new TaskReassignmentInfo(t, t.Assignments
                    .OrderByDescending(a => a.Id)
                    .Select(a => a.UserId)
                    .Take(maxHistoryCheck)
                    .ToArray()))
            .ToArrayAsync(token);
    }

    public async Task CreateTaskAsync(TaskItem task, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        await db.Tasks.AddAsync(task, token);
        await db.SaveChangesAsync(token);
    }

    public async Task CreateTaskAssignmentAsync(TaskAssignment assignment, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        await db.Assignments.AddAsync(assignment, token);
        await db.SaveChangesAsync(token);
    }

    public async Task CreateTaskAssignmentsAsync(List<TaskAssignment> assignments, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentOutOfRangeException.ThrowIfZero(assignments.Count, nameof(assignments));

        await db.Assignments.AddRangeAsync(assignments, token);
    }

    public Task<int> MarkTasksAsCompletedAsync(HashSet<long> userIds, CancellationToken token = default)
    {
        return db.Tasks
            .Where(t => t.State == TaskState.InProgress &&
                       t.Assignments.Select(a => a.UserId).Distinct().Count() == userIds.Count)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.State, TaskState.Completed)
                .SetProperty(t => t.AssignedUserId, (long?)null),
                cancellationToken: token);
    }

    public Task<int> SetTasksToWaitingAsync(CancellationToken token = default)
    {
        return db.Tasks
            .Where(t => t.State == TaskState.InProgress)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.State, TaskState.Waiting)
                .SetProperty(t => t.AssignedUserId, (long?)null),
                cancellationToken: token);
    }

    public Task<bool> TaskExistsByTitleAsync(string title, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return db.Tasks.AnyAsync(u => u.Title.ToLower() == title.ToLower(), token);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken token = default)
    {
        return db.Database.BeginTransactionAsync(token);
    }

    public Task SaveChangesAsync(CancellationToken token = default)
    {
        return db.SaveChangesAsync(token);
    }
}