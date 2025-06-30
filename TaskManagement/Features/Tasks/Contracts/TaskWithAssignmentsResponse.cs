namespace TaskManagement.Features.Tasks.Contracts;

public record TaskWithAssignmentsResponse(
    long Id,
    string Title,
    string State,
    List<TaskUserAssignmentResponse> Assignments
);

public record TaskUserAssignmentResponse(
    long UserId,
    string UserName
);