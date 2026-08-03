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

    // Purga oportunista antes de insertar: un JWT ya expirado lo rechaza la validacion de
    // firma, asi que su fila en la blocklist no protege de nada y solo hace crecer la tabla
    // (una fila por logout, para siempre). Se limpia aqui, en la operacion menos frecuente
    // del sistema, en vez de con un BackgroundService dedicado -- misma logica de no
    // introducir infraestructura para un problema que cabe en una consulta. Corre fuera del
    // SaveChanges del Handler: si este falla, lo unico perdido son filas ya inutiles.
    public async Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        await _dbContext.RevokedTokens
            .Where(t => t.ExpiresAtUtc < DateTime.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);

        _dbContext.RevokedTokens.Add(new RevokedToken(jti, expiresAtUtc));
    }

    public Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken) =>
        _dbContext.RevokedTokens.AsNoTracking().AnyAsync(t => t.Jti == jti, cancellationToken);
}
