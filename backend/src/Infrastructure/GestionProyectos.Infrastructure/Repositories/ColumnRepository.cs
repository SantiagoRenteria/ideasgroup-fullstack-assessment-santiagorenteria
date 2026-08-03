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

    // ThenBy(Id) como desempate: el Order de una columna lo elige el cliente
    // (CreateColumnRequest) y nada impide que dos columnas del mismo proyecto compartan
    // valor. Sin un segundo criterio, PostgreSQL no garantiza orden estable entre las
    // empatadas y el tablero podia intercambiarlas de sitio entre recargas sin que nadie
    // tocara nada. El Id es arbitrario pero estable, que es lo unico que importa en un
    // empate -- ver docs/decisions/arquitectura-decisiones.md §27.1.
    public async Task<IReadOnlyList<Column>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        await _dbContext.Columns
            .AsNoTracking()
            .Where(c => c.ProjectId == projectId)
            .OrderBy(c => c.Order)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

    public Task<bool> HasTasksAsync(Guid columnId, CancellationToken cancellationToken) =>
        _dbContext.Tasks.AsNoTracking().AnyAsync(t => t.ColumnId == columnId, cancellationToken);

    public Task<bool> ProjectHasTasksAsync(Guid projectId, CancellationToken cancellationToken) =>
        _dbContext.Tasks
            .AsNoTracking()
            .AnyAsync(t => _dbContext.Columns.Any(c => c.Id == t.ColumnId && c.ProjectId == projectId), cancellationToken);

    public async Task AddAsync(Column column, CancellationToken cancellationToken) =>
        await _dbContext.Columns.AddAsync(column, cancellationToken);

    public async Task SoftDeleteByProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        await _dbContext.Columns
            .Where(c => c.ProjectId == projectId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.IsDeleted, true)
                .SetProperty(c => c.DeletedAt, DateTime.UtcNow), cancellationToken);
}
