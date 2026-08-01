using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Infrastructure.Persistence;
using GestionProyectos.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionProyectos.Infrastructure.Security;

public class TokenRevocationStore : ITokenRevocationStore
{
    private readonly AppDbContext _dbContext;

    public TokenRevocationStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        _dbContext.RevokedTokens.Add(new RevokedToken(jti, expiresAtUtc));
        return Task.CompletedTask;
    }

    public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken) =>
        _dbContext.RevokedTokens.AsNoTracking().AnyAsync(t => t.Jti == jti, cancellationToken);
}
