using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Tasks.Commands.CreateTask;

// ConnectionId: conexion SignalR del emisor (si el cliente tiene el canal abierto), para
// que el Handler la excluya al notificar por tiempo real -- ver ADR §15.3.
public record CreateTaskCommand(
    Guid ColumnId,
    string Title,
    string Description,
    TaskPriority Priority,
    Guid? AssigneeId,
    string? ConnectionId = null) : ICommand<Result<TaskResponseDto>>;
