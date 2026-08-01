using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Common.Interfaces;

public interface ITaskRepository
{
    Task<TaskEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Tracked (sin AsNoTracking): a diferencia de los demas listados de solo lectura del
    // proyecto, este se usa tambien para el rebalanceo de LexoRank (TaskOrderingHelper),
    // que muta el Order de cada tarea de la columna y depende del change tracker de EF.
    Task<IReadOnlyList<TaskEntity>> ListByColumnAsync(Guid columnId, CancellationToken cancellationToken);

    // Solo lectura: alimenta el tablero agregado (GetProjectBoardQuery), agrupado por
    // columna en el handler -- no necesita tracking.
    Task<IReadOnlyList<TaskEntity>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken);

    Task AddAsync(TaskEntity task, CancellationToken cancellationToken);
}
