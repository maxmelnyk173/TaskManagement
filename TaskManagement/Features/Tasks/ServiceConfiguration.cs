using TaskManagement.Features.Tasks.Repositories;
using TaskManagement.Features.Tasks.Services;

namespace TaskManagement.Features.Tasks;

public static class ServiceConfiguration
{
    public static void AddTasks(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TaskReassignmentOptions>(configuration.GetSection(TaskReassignmentOptions.SectionName));

        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITaskAssignmentService, TaskAssignmentService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddHostedService<TaskReassignmentWorker>();
    }
}