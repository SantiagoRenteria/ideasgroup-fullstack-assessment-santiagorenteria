using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionProyectos.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _dbContext;

    public TaskRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TaskEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    // Tracked a proposito: TaskOrderingHelper puede mutar el Order de estas entidades al
    // rebalancear la columna (ver ITaskRepository.ListByColumnAsync).
    public async Task<IReadOnlyList<TaskEntity>> ListByColumnAsync(Guid columnId, CancellationToken cancellationToken) =>
        await _dbContext.Tasks
            .Where(t => t.ColumnId == columnId)
            .OrderBy(t => t.Order)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TaskEntity>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        await _dbContext.Tasks
            .AsNoTracking()
            .Where(t => _dbContext.Columns.Any(c => c.Id == t.ColumnId && c.ProjectId == projectId))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TaskEntity task, CancellationToken cancellationToken) =>
        await _dbContext.Tasks.AddAsync(task, cancellationToken);
}
