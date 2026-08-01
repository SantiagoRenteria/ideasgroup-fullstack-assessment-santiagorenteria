using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Entities;
using GestionProyectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionProyectos.Infrastructure.Repositories;

public class ColumnRepository : IColumnRepository
{
    private readonly AppDbContext _dbContext;

    public ColumnRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Column?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Columns.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Column>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        await _dbContext.Columns
            .AsNoTracking()
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.Order)
            .ToListAsync(cancellationToken);

    public Task<bool> HasTasksAsync(Guid columnId, CancellationToken cancellationToken) =>
        _dbContext.Tasks.AsNoTracking().AnyAsync(t => t.ColumnId == columnId, cancellationToken);

    public async Task AddAsync(Column column, CancellationToken cancellationToken) =>
        await _dbContext.Columns.AddAsync(column, cancellationToken);

    public void Remove(Column column) => _dbContext.Columns.Remove(column);
}
