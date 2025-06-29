using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Features.Users.Contracts;

public record CreateUserRequest(
    [property: Required]
    [property: MinLength(1)]
    [property: StringLength(100)]
    string Name
);