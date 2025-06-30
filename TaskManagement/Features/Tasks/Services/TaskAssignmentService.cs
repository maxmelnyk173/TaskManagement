using Microsoft.Extensions.Options;
using TaskManagement.Features.Tasks.Entities;
using TaskManagement.Features.Tasks.Models;
using TaskManagement.Features.Tasks.Repositories;
using TaskManagement.Features.Users.Services;

namespace TaskManagement.Features.Tasks.Services;

public class TaskAssignmentService(
    ITaskRepository repository,
    IUserCacheService userCache,
    ILogger<TaskAssignmentService> logger,
    IOptions<TaskReassignmentOptions> options
) : ITaskAssignmentService
{
    private static readonly Random _random = Random.Shared;
    private readonly TaskReassignmentOptions _options = options.Value;

    public async Task AssignInitialTaskAsync(TaskItem task, CancellationToken token)
    {
        var userIds = await userCache.GetAvailableUserIdsAsync(token);
        if (userIds.Count == 0)
        {
            task.State = TaskState.Waiting;
            task.AssignedUserId = null;
            return;
        }

        var randomUserId = userIds.ElementAt(_random.Next(userIds.Count));

        task.AssignedUserId = randomUserId;
        task.State = TaskState.InProgress;

        await repository.CreateTaskAssignmentAsync(new TaskAssignment
        {
            TaskId = task.Id,
            UserId = randomUserId,
            CreatedAt = DateTime.UtcNow
        }, token);
    }

    public async Task ReassignTasksAsync(CancellationToken token)
    {
        logger.LogInformation("Starting task reassignment job.");

        var userIds = await userCache.GetAvailableUserIdsAsync(token);
        if (userIds.Count == 0)
        {
            logger.LogWarning("No users found. Setting all InProgress tasks to Waiting");
            await repository.SetTasksToWaitingAsync(token);
            return;
        }

        await using var transaction = await repository.BeginTransactionAsync(token);

        try
        {
            var completedCount = await repository.MarkTasksAsCompletedAsync(userIds, token);
            logger.LogInformation("Marked {Count} tasks as Completed", completedCount);

            var processedCount = await ProcessTaskReassignments(userIds, token);

            await transaction.CommitAsync(token);
            logger.LogInformation("Task reassignment completed successfully. Processed {Count} tasks", processedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during task reassignment. Rolling back transaction.");
            await transaction.RollbackAsync(token);
            throw;
        }
    }

    private async Task<int> ProcessTaskReassignments(HashSet<long> allUserIds, CancellationToken token)
    {
        var totalProcessed = 0;
        var currentPage = 0;

        while (!token.IsCancellationRequested)
        {
            var tasksToProcess = await repository.GetTasksForReassignmentAsync(
                currentPage * _options.PageSize,
                _options.PageSize,
                _options.MaxAssignmentHistoryCheck,
                token
            );
            if (tasksToProcess.Length == 0) break;

            logger.LogDebug("Processing page {Page} with {Count} tasks", currentPage, tasksToProcess.Length);

            var newAssignments = ProcessTaskPage(tasksToProcess, allUserIds);

            if (newAssignments.Count > 0)
            {
                await repository.CreateTaskAssignmentsAsync(newAssignments, token);
            }

            await repository.SaveChangesAsync(token);

            totalProcessed += tasksToProcess.Length;
            currentPage++;

            if (tasksToProcess.Length < _options.PageSize) break;
        }

        return totalProcessed;
    }

    private List<TaskAssignment> ProcessTaskPage(
        TaskReassignmentInfo[] tasksToProcess,
        HashSet<long> allUserIds)
    {
        var newAssignments = new List<TaskAssignment>();

        foreach (var item in tasksToProcess)
        {
            var eligibleUserId = FindEligibleUser(allUserIds, item);
            if (eligibleUserId.HasValue)
            {
                item.Task.AssignedUserId = eligibleUserId;

                newAssignments.Add(new TaskAssignment
                {
                    TaskId = item.Task.Id,
                    UserId = eligibleUserId.Value,
                    CreatedAt = DateTime.UtcNow
                });
                logger.LogDebug("Reassigning Task {TaskId} to User {NewUserId}", item.Task.Id, eligibleUserId);
            }
            else
            {
                item.Task.State = TaskState.Waiting;
                item.Task.AssignedUserId = null;
                logger.LogWarning("No eligible users for Task {TaskId}. Setting to Waiting.", item.Task.Id);
            }
        }

        return newAssignments;
    }

    private static long? FindEligibleUser(HashSet<long> allUserIds, TaskReassignmentInfo taskInfo)
    {
        var excludedUserIds = new HashSet<long>();

        if (taskInfo.Task.AssignedUserId.HasValue)
            excludedUserIds.Add(taskInfo.Task.AssignedUserId.Value);

        foreach (var userId in taskInfo.RecentAssignedUserIds)
            excludedUserIds.Add(userId);

        var eligibleUsers = allUserIds.Except(excludedUserIds).ToArray();

        return eligibleUsers.Length > 0
            ? eligibleUsers[_random.Next(eligibleUsers.Length)]
            : null;
    }
}