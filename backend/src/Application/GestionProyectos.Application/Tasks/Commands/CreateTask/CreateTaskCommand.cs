using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Tasks.Commands.CreateTask;

public record CreateTaskCommand(
    Guid ColumnId,
    string Title,
    string Description,
    TaskPriority Priority,
    Guid? AssigneeId) : ICommand<Result<TaskResponseDto>>;
