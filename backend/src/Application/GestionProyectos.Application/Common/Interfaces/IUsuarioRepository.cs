using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Common.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByCorreoAsync(string correo, CancellationToken cancellationToken);
}
