using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskManagement.Features.Tasks.Entities;
using TaskManagement.Features.Tasks.Models;
using TaskManagement.Features.Tasks.Repositories;
using TaskManagement.Features.Tasks.Services;
using TaskManagement.Features.Users.Services;

namespace TaskManagement.Test;

public class TaskAssignmentServiceTests
{
    private readonly Mock<ITaskRepository> _mockRepository;
    private readonly Mock<IUserCacheService> _mockUserCache;
    private readonly Mock<ILogger<TaskAssignmentService>> _mockLogger;
    private readonly Mock<IOptions<TaskReassignmentOptions>> _mockOptions;
    private readonly TaskAssignmentService _assignmentService;

    public TaskAssignmentServiceTests()
    {
        _mockRepository = new Mock<ITaskRepository>();
        _mockUserCache = new Mock<IUserCacheService>();
        _mockLogger = new Mock<ILogger<TaskAssignmentService>>();
        _mockOptions = new Mock<IOptions<TaskReassignmentOptions>>();

        _mockOptions.Setup(o => o.Value).Returns(new TaskReassignmentOptions
        {
            PageSize = 10,
            MaxAssignmentHistoryCheck = 2
        });

        _assignmentService = new TaskAssignmentService(
            _mockRepository.Object,
            _mockUserCache.Object,
            _mockLogger.Object,
            _mockOptions.Object
        );
    }

    [Fact]
    public async Task AssignInitialTaskAsync_WithAvailableUsers_AssignsRandomUser()
    {
        var task = new TaskItem { Id = 1, Title = "Test Task" };
        var userIds = new HashSet<long> { 1, 2, 3 };

        _mockUserCache.Setup(c => c.GetAvailableUserIdsAsync(default)).ReturnsAsync(userIds);


        await _assignmentService.AssignInitialTaskAsync(task, default);


        Assert.Equal(TaskState.InProgress, task.State);
        Assert.NotNull(task.AssignedUserId);
        Assert.Contains(task.AssignedUserId.Value, userIds);
        _mockRepository.Verify(r => r.CreateTaskAssignmentAsync(It.IsAny<TaskAssignment>(), default), Times.Once);
    }

    [Fact]
    public async Task AssignInitialTaskAsync_WithNoUsers_SetsTaskToWaiting()
    {
        var task = new TaskItem { Id = 1, Title = "Test Task" };
        var userIds = new HashSet<long>();

        _mockUserCache.Setup(c => c.GetAvailableUserIdsAsync(default)).ReturnsAsync(userIds);

        await _assignmentService.AssignInitialTaskAsync(task, default);

        Assert.Equal(TaskState.Waiting, task.State);
        Assert.Null(task.AssignedUserId);
        _mockRepository.Verify(r => r.CreateTaskAssignmentAsync(It.IsAny<TaskAssignment>(), default), Times.Never);
    }

    [Fact]
    public async Task ReassignTasksAsync_WithNoUsers_SetsAllTasksToWaiting()
    {
        var userIds = new HashSet<long>();

        _mockUserCache.Setup(c => c.GetAvailableUserIdsAsync(default)).ReturnsAsync(userIds);

        await _assignmentService.ReassignTasksAsync(default);

        _mockRepository.Verify(r => r.SetTasksToWaitingAsync(default), Times.Once);
    }

    [Fact]
    public async Task ReassignTasksAsync_WithUsers_ProcessesTasksAndCreatesAssignments()
    {
        var userIds = new HashSet<long> { 1, 2, 3 };
        var mockTransaction = new Mock<IDbContextTransaction>();
        var tasksToProcess = new[]
        {
            new TaskReassignmentInfo(new TaskItem { Id = 1, AssignedUserId = 1 }, [1])
        };

        _mockUserCache.Setup(c => c.GetAvailableUserIdsAsync(default)).ReturnsAsync(userIds);
        _mockRepository.Setup(r => r.BeginTransactionAsync(default)).ReturnsAsync(mockTransaction.Object);
        _mockRepository.Setup(r => r.MarkTasksAsCompletedAsync(userIds, default)).ReturnsAsync(0);
        _mockRepository.Setup(r => r.GetTasksForReassignmentAsync(0, 10, 2, default)).ReturnsAsync(tasksToProcess);
        _mockRepository.Setup(r => r.GetTasksForReassignmentAsync(10, 10, 2, default)).ReturnsAsync(Array.Empty<TaskReassignmentInfo>());

        await _assignmentService.ReassignTasksAsync(default);

        _mockRepository.Verify(r => r.CreateTaskAssignmentsAsync(It.IsAny<List<TaskAssignment>>(), default), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(default), Times.Once);
        mockTransaction.Verify(t => t.CommitAsync(default), Times.Once);
    }
}