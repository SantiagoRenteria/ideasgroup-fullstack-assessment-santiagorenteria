using GestionProyectos.Application.Common.Messaging;
using GestionProyectos.Domain.Common;

namespace GestionProyectos.Application.Tasks.Commands.MoveTask;

// Separado de UpdateTaskCommand a proposito (ver docs/decisions/arquitectura-decisiones.md
// §14.1): representa el traslado por drag&drop, no la edicion de datos de negocio.
// TargetIndex es la posicion deseada dentro de la columna destino, sin contar la propia
// tarea que se esta moviendo (0 = inicio de columna).
public record MoveTaskCommand(Guid Id, Guid TargetColumnId, int TargetIndex) : ICommand<Result<TaskResponseDto>>;
