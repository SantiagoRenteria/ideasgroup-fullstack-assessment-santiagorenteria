using GestionProyectos.Application.Tasks;

namespace GestionProyectos.Application.Common.Interfaces;

// Puerto de tiempo real (sección 6.7, ADR §15.1): los Handlers nunca dependen de SignalR
// directo. excludeConnectionId evita reenviarle al emisor su propio cambio (§15.3).
public interface IBoardNotifier
{
    Task TaskCreatedAsync(Guid projectId, TaskResponseDto task, string? excludeConnectionId, CancellationToken cancellationToken);

    Task TaskUpdatedAsync(Guid projectId, TaskResponseDto task, string? excludeConnectionId, CancellationToken cancellationToken);

    Task TaskDeletedAsync(Guid projectId, Guid taskId, Guid columnId, string? excludeConnectionId, CancellationToken cancellationToken);

    // TargetIndex (no solo el Order de LexoRank) para que las demas sesiones apliquen el
    // mismo moveItemInArray/transferArrayItem que ya usa BoardComponent.onDrop (§15.5).
    Task TaskMovedAsync(Guid projectId, TaskResponseDto task, int targetIndex, string? excludeConnectionId, CancellationToken cancellationToken);
}
