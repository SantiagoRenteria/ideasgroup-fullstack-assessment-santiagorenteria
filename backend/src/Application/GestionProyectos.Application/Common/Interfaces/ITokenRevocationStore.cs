namespace GestionProyectos.Application.Common.Interfaces;

// Puerto de la blocklist de JWT (ver docs/decisions/arquitectura-decisiones.md §16).
// Solo trabaja con primitivos (jti como string): Application no necesita conocer como o
// donde se persiste la revocacion.
public interface ITokenRevocationStore
{
    Task RevokeAsync(string jti, DateTime expiresAtUtc, CancellationToken cancellationToken);

    Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken);
}
