using GestionProyectos.Application.Common.Interfaces;

namespace GestionProyectos.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _dbContext;

    public UnitOfWork(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await operation();

        await transaction.CommitAsync(cancellationToken);
    }
}
