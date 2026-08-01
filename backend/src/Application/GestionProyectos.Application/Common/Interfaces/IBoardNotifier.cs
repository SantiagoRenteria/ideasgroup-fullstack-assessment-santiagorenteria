using GestionProyectos.Application.Tasks;

namespace GestionProyectos.Application.Common.Interfaces;

// Puerto de tiempo real (seccion 6.7): los Handlers de Tasks dependen solo de esta
// interfaz, nunca de SignalR directamente -- ver docs/decisions/arquitectura-decisiones.md
// §15.1. excludeConnectionId es la conexion del propio emisor (si tiene el canal abierto)
// para no reenviarle su propio cambio (§15.3): ya lo aplico de forma optimista con la
// respuesta HTTP.
public interface IBoardNotifier
{
    Task TaskCreatedAsync(Guid projectId, TaskResponseDto task, string? excludeConnectionId, CancellationToken cancellationToken);

    Task TaskUpdatedAsync(Guid projectId, TaskResponseDto task, string? excludeConnectionId, CancellationToken cancellationToken);

    Task TaskDeletedAsync(Guid projectId, Guid taskId, Guid columnId, string? excludeConnectionId, CancellationToken cancellationToken);

    // TargetIndex (no solo el Order de LexoRank) para que las demas sesiones apliquen el
    // mismo moveItemInArray/transferArrayItem que ya usa BoardComponent.onDrop (§15.5).
    Task TaskMovedAsync(Guid projectId, TaskResponseDto task, int targetIndex, string? excludeConnectionId, CancellationToken cancellationToken);
}
