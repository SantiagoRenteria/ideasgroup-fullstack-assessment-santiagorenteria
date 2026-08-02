using GestionProyectos.Domain.Entities;
using GestionProyectos.Domain.ValueObjects;

namespace GestionProyectos.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken);

    // Alimenta el selector de "responsable" del formulario de tareas (seccion 6.5). No
    // hay CRUD de usuarios en el alcance del enunciado, solo lectura de los precargados.
    Task<IReadOnlyList<User>> ListAllAsync(CancellationToken cancellationToken);
}
