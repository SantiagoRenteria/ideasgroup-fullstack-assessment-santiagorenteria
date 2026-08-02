using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;

namespace GestionProyectos.Application.Tasks;

// Punto unico donde Create/MoveTaskCommandHandler calculan el Order (LexoRank) y
// reaccionan al rebalanceo (ADR §4/§14) si el hueco entre claves se agoto.
internal static class TaskOrderingHelper
{
    // `tasksInColumn` debe venir ordenado por Order y sin incluir la tarea que se esta
    // insertando/moviendo. `targetIndex` es la posicion final deseada dentro de esa lista
    // (0 = inicio de columna, tasksInColumn.Count = final de columna).
    public static string GetOrderForTargetIndex(IReadOnlyList<TaskEntity> tasksInColumn, int targetIndex)
    {
        var prevOrder = targetIndex > 0 ? tasksInColumn[targetIndex - 1].Order.Value : null;
        var nextOrder = targetIndex < tasksInColumn.Count ? tasksInColumn[targetIndex].Order.Value : null;

        try
        {
            return LexoRankService.GetKeyBetween(prevOrder, nextOrder);
        }
        catch (LexoRankRebalanceRequiredException)
        {
            return RebalanceAndRetry(tasksInColumn, targetIndex);
        }
    }

    // Reescribe el Order de toda la columna con el mismo algoritmo de biseccion; las
    // entidades deben venir trackeadas por EF (ver ITaskRepository.ListByColumnAsync).
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
