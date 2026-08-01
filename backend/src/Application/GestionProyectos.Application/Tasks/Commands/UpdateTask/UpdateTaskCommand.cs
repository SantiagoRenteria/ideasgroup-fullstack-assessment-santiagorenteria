using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Tasks.Commands.UpdateTask;

// ConnectionId: ver CreateTaskCommand / ADR §15.3.
public record UpdateTaskCommand(
    Guid Id,
    string Title,
    string Description,
    TaskPriority Priority,
    Guid? AssigneeId,
    string? ConnectionId = null) : ICommand<Result<TaskResponseDto>>;
