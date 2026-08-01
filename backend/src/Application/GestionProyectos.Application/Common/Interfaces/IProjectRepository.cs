using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;

namespace GestionProyectos.Application.Common.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    // Comparacion case-insensitive (misma semantica que el filtro parcial de ListAsync,
    // via ILIKE): "Migracion ERP" y "migracion erp" se consideran el mismo nombre.
    // excludeProjectId permite que Update no choque contra el propio nombre del proyecto.
    Task<bool> ExistsByNameAsync(string name, Guid? excludeProjectId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Project> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? name,
        ProjectStatus? status,
        CancellationToken cancellationToken);

    Task AddAsync(Project project, CancellationToken cancellationToken);
}
