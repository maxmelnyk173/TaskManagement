public class TaskReassignmentOptions
{
    public const string SectionName = "TaskReassignment";

    public int IntervalSeconds { get; set; } = 120;
    public int PageSize { get; set; } = 500;
    public int MaxAssignmentHistoryCheck { get; set; } = 2;
}