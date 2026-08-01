using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Tasks;

// Punto unico donde CreateTaskCommandHandler y MoveTaskCommandHandler calculan el Order
// (LexoRank) de una tarea al insertarla o moverla. Centraliza tambien la reaccion al
// rebalanceo (ver docs/decisions/arquitectura-decisiones.md §4 y §14): si el hueco entre
// dos claves se agoto, se regeneran las claves de toda la columna antes de reintentar.
internal static class TaskOrderingHelper
{
    // `tasksInColumn` debe venir ordenado por Order y sin incluir la tarea que se esta
    // insertando/moviendo. `targetIndex` es la posicion final deseada dentro de esa lista
    // (0 = inicio de columna, tasksInColumn.Count = final de columna).
    public static string GetOrderForTargetIndex(IReadOnlyList<TaskEntity> tasksInColumn, int targetIndex)
    {
        var prevOrder = targetIndex > 0 ? tasksInColumn[targetIndex - 1].Order : null;
        var nextOrder = targetIndex < tasksInColumn.Count ? tasksInColumn[targetIndex].Order : null;

        try
        {
            return LexoRankService.GetKeyBetween(prevOrder, nextOrder);
        }
        catch (LexoRankRebalanceRequiredException)
        {
            return RebalanceAndRetry(tasksInColumn, targetIndex);
        }
    }

    // Reescribe el Order de cada tarea existente en la columna con claves cortas y
    // parejamente espaciadas (mismo algoritmo de punto medio, aplicado por biseccion),
    // y recalcula el punto de insercion sobre esas claves nuevas. EF Core detecta estos
    // cambios por change tracking -- las entidades de `tasksInColumn` deben venir
    // trackeadas (ver ITaskRepository.ListByColumnAsync).
    private static string RebalanceAndRetry(IReadOnlyList<TaskEntity> tasksInColumn, int targetIndex)
    {
        var rebalanced = LexoRankService.GenerateSequence(tasksInColumn.Count);

        for (var i = 0; i < tasksInColumn.Count; i++)
            tasksInColumn[i].Move(tasksInColumn[i].ColumnId, rebalanced[i]);

        var prevOrder = targetIndex > 0 ? rebalanced[targetIndex - 1] : null;
        var nextOrder = targetIndex < rebalanced.Count ? rebalanced[targetIndex] : null;

        return LexoRankService.GetKeyBetween(prevOrder, nextOrder);
    }
}
