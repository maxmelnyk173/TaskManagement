using TaskManagement.Features.Users.Entities;

namespace TaskManagement.Features.Tasks.Entities;

public class TaskItem
{
    public long Id { get; set; }

    public string Title { get; set; }

    public TaskState State { get; set; }

    public long? AssignedUserId { get; set; }

    public virtual User AssignedUser { get; set; }

    public virtual ICollection<TaskAssignment> Assignments { get; set; } = [];
}