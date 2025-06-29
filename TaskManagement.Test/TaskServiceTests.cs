using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using TaskManagement.Common;
using TaskManagement.Features.Tasks.Contracts;
using TaskManagement.Features.Tasks.Entities;
using TaskManagement.Features.Tasks.Repositories;
using TaskManagement.Features.Tasks.Services;
using TaskManagement.Features.Users.Entities;

namespace TaskManagement.Test;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _mockRepository;
    private readonly Mock<ITaskAssignmentService> _mockAssignmentService;
    private readonly TaskService _taskService;

    public TaskServiceTests()
    {
        _mockRepository = new Mock<ITaskRepository>();
        _mockAssignmentService = new Mock<ITaskAssignmentService>();
        _taskService = new TaskService(_mockRepository.Object, _mockAssignmentService.Object);
    }

    [Fact]
    public async Task GetTasksAsync_ReturnsMappedTaskResponses()
    {

        var tasks = new[]
        {
                new TaskItem
                {
                    Id = 1,
                    Title = "Task 1",
                    State = TaskState.InProgress,
                    AssignedUser = new User { Id = 1, Name = "John" }
                },
                new TaskItem
                {
                    Id = 2,
                    Title = "Task 2",
                    State = TaskState.Waiting,
                    AssignedUser = null
                }
        };

        _mockRepository.Setup(r => r.GetTasksAsync(0, 10, default)).ReturnsAsync(tasks);

        var result = await _taskService.GetTasksAsync(0, 10, default);

        Assert.Equal(2, result.Length);
        Assert.Equal("Task 1", result[0].Title);
        Assert.Equal("InProgress", result[0].State);
        Assert.NotNull(result[0].AssignedUser);
        Assert.Equal("John", result[0].AssignedUser.Name);
        Assert.Null(result[1].AssignedUser);
    }

    [Fact]
    public async Task GetTaskByIdAsync_WithExistingTask_ReturnsTaskWithAssignments()
    {
        var task = new TaskItem
        {
            Id = 1,
            Title = "Task 1",
            State = TaskState.InProgress,
            AssignedUser = new User { Id = 1, Name = "John" },
            Assignments =
            [
                new() { UserId = 1, User = new User { Id = 1, Name = "John" } },
                new() { UserId = 2, User = new User { Id = 2, Name = "Jane" } }
            ]
        };

        _mockRepository.Setup(r => r.GetTaskByIdAsync(1, default)).ReturnsAsync(task);


        var result = await _taskService.GetTaskByIdAsync(1, default);

        Assert.Equal(1, result.Id);
        Assert.Equal("Task 1", result.Title);
        Assert.Equal("InProgress", result.State);
        Assert.Equal("John", result.AssignedUser.Name);
        Assert.Equal(2, result.Assignments.Count);
        Assert.Contains(result.Assignments, a => a.UserName == "John");
        Assert.Contains(result.Assignments, a => a.UserName == "Jane");
    }

    [Fact]
    public async Task GetTaskByIdAsync_WithNonExistentTask_ThrowsNotFoundException()
    {
        _mockRepository.Setup(r => r.GetTaskByIdAsync(999, default)).ReturnsAsync(value: null);

        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _taskService.GetTaskByIdAsync(999, default));
        Assert.Contains("Task with ID 999 not found", exception.Message);
    }

    [Fact]
    public async Task CreateTaskAsync_WithValidRequest_ReturnsTaskResponse()
    {
        var request = new CreateTaskRequest("New Task");

        var mockTransaction = new Mock<IDbContextTransaction>();
        _mockRepository.Setup(r => r.TaskExistsByTitleAsync("New Task", default)).ReturnsAsync(false);
        _mockRepository.Setup(r => r.BeginTransactionAsync(default)).ReturnsAsync(mockTransaction.Object);
        _mockRepository.Setup(r => r.CreateTaskAsync(It.IsAny<TaskItem>(), default))
                      .Callback<TaskItem, CancellationToken>((task, _) => task.Id = 1)
                      .Returns(Task.CompletedTask);

        var result = await _taskService.CreateTaskAsync(request, default);

        Assert.Equal(1, result.Id);
        Assert.Equal("New Task", result.Title);
        _mockAssignmentService.Verify(a => a.AssignInitialTaskAsync(It.IsAny<TaskItem>(), default), Times.Once);
        mockTransaction.Verify(t => t.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_WithExistingTitle_ThrowsConflictException()
    {
        var request = new CreateTaskRequest("Existing Task");

        _mockRepository.Setup(r => r.TaskExistsByTitleAsync("Existing Task", default)).ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _taskService.CreateTaskAsync(request, default));
        Assert.Contains("Task with title 'Existing Task' already exists", exception.Message);
    }
}