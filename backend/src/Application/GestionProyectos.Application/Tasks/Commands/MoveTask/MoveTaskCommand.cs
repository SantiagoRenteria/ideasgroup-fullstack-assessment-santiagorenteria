using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Tasks.Commands.MoveTask;

// Separado de UpdateTaskCommand (ADR §14.1): drag&drop, no edicion de negocio. TargetIndex
// no cuenta la propia tarea (0 = inicio de columna). ConnectionId: ver ADR §15.3.
public record MoveTaskCommand(Guid Id, Guid TargetColumnId, int TargetIndex, string? ConnectionId = null) : ICommand<Result<TaskResponseDto>>;
