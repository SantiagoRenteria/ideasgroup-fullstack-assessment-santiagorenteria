using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Common.Interfaces;

public interface IColumnRepository
{
    Task<Column?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Column>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken);

    Task<bool> HasTasksAsync(Guid columnId, CancellationToken cancellationToken);

    Task AddAsync(Column column, CancellationToken cancellationToken);

    void Remove(Column column);
}
