using Microsoft.Extensions.Options;
using TaskManagement.Features.Tasks.Services;

namespace TaskManagement.Features.Tasks;

public class TaskReassignmentWorker(
    ILogger<TaskReassignmentWorker> logger,
    IServiceProvider serviceProvider,
    IOptions<TaskReassignmentOptions> options
) : BackgroundService
{
    private readonly TaskReassignmentOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Task Reassignment Worker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Task Reassignment Worker is running.");

            using (var scope = serviceProvider.CreateScope())
            {
                var assignmentService = scope.ServiceProvider.GetRequiredService<ITaskAssignmentService>();
                try
                {
                    await assignmentService.ReassignTasksAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "An error occurred during the scheduled task reassignment.");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Task Reassignment Worker is stopping.");
    }
}