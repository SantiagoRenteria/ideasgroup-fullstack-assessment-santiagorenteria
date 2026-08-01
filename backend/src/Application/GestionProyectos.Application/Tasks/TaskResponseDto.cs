using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Tasks;

public record TaskResponseDto(
    Guid Id,
    Guid ColumnId,
    string Title,
    string Description,
    TaskPriority Priority,
    Guid? AssigneeId,
    string Order,
    DateTime CreatedAt);
