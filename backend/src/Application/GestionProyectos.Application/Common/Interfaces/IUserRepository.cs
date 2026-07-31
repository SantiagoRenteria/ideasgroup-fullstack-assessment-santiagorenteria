using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
}
