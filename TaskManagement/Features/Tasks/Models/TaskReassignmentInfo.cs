using TaskManagement.Features.Tasks.Entities;

namespace TaskManagement.Features.Tasks.Models;

public record TaskReassignmentInfo(TaskItem Task, long[] RecentAssignedUserIds);