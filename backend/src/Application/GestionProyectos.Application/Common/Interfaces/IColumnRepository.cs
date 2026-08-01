using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Common.Interfaces;

public interface IColumnRepository
{
    Task<Column?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Column>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken);

    Task<bool> HasTasksAsync(Guid columnId, CancellationToken cancellationToken);

    Task<bool> ProjectHasTasksAsync(Guid projectId, CancellationToken cancellationToken);

    Task AddAsync(Column column, CancellationToken cancellationToken);

    // Cascada logica al eliminar un Project: actualizacion en bloque (no carga cada
    // Column en memoria) ya que aqui no aplica ninguna regla de negocio por columna --
    // DeleteProjectCommandHandler ya garantizo que ninguna tiene tareas antes de llamar esto.
    Task SoftDeleteByProjectAsync(Guid projectId, CancellationToken cancellationToken);
}
