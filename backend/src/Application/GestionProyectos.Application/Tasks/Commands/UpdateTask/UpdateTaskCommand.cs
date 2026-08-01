using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Tasks.Commands.UpdateTask;

public record UpdateTaskCommand(
    Guid Id,
    string Title,
    string Description,
    TaskPriority Priority,
    Guid? AssigneeId) : ICommand<Result<TaskResponseDto>>;
