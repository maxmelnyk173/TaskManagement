using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Features.Tasks.Contracts;

public record CreateTaskRequest(
    [property: Required]
    [property: MinLength(1)]
    [property: StringLength(200)]
    string Title
);