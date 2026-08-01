namespace GestionProyectos.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);

    // Envuelve operacion en una transaccion explicita: usado cuando un caso de uso
    // combina un SaveChanges con una operacion de escritura adicional (ej. la cascada
    // logica de DeleteProjectCommandHandler) y ambas deben persistir juntas o ninguna.
    Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken);
}
