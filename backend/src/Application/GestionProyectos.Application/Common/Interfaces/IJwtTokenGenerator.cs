using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Common.Interfaces;

public record JwtToken(string Value, DateTime ExpiresAtUtc);

public interface IJwtTokenGenerator
{
    JwtToken Generate(Usuario usuario);
}
