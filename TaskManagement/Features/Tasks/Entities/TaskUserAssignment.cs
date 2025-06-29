using TaskManagement.Features.Users.Entities;

namespace TaskManagement.Features.Tasks.Entities;

public class TaskAssignment
{
    public long Id { get; set; }

    public long UserId { get; set; }

    public long TaskId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; }

    public virtual TaskItem Task { get; set; }
}