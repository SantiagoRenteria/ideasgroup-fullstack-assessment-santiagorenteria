using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Common.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Project> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? name,
        ProjectStatus? status,
        CancellationToken cancellationToken);

    Task AddAsync(Project project, CancellationToken cancellationToken);

    void Remove(Project project);
}
