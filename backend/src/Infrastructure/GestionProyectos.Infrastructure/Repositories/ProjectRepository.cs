using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.Enums;
using GestionProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionProyectos.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _dbContext;

    public ProjectRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, Guid? excludeProjectId, CancellationToken cancellationToken) =>
        _dbContext.Projects
            .AsNoTracking()
            .Where(p => excludeProjectId == null || p.Id != excludeProjectId)
            .AnyAsync(p => EF.Functions.ILike(p.Name, name), cancellationToken);

    public async Task<(IReadOnlyList<Project> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        string? name,
        ProjectStatus? status,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Projects.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{name}%"));

        if (status is not null)
            query = query.Where(p => p.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Project project, CancellationToken cancellationToken) =>
        await _dbContext.Projects.AddAsync(project, cancellationToken);
}
