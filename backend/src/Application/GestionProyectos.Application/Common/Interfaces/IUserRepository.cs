using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    // Alimenta el selector de "responsable" del formulario de tareas (seccion 6.5). No
    // hay CRUD de usuarios en el alcance del enunciado, solo lectura de los precargados.
    Task<IReadOnlyList<User>> ListAllAsync(CancellationToken cancellationToken);
}
