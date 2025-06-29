using TaskManagement.Features.Tasks.Entities;

namespace TaskManagement.Features.Users.Entities;

public class User
{
    public long Id { get; set; }

    public string Name { get; set; }

    public virtual ICollection<TaskItem> Tasks { get; set; } = [];
}