using TaskManagement.Features.Users.Contracts;

namespace TaskManagement.Features.Tasks.Contracts;

public record TaskResponse(
    long Id,
    string Title,
    string State,
    UserResponse AssignedUser
);