namespace GestionProyectos.Application.Common.Outbox;

// Constantes compartidas entre quien encola (los Handlers de Tasks) y quien despacha
// (OutboxDispatcher, Infrastructure) -- evita strings magicos duplicados en ambos lados.
public static class OutboxEventTypes
{
    public const string TaskCreated = "TaskCreated";
    public const string TaskUpdated = "TaskUpdated";
    public const string TaskDeleted = "TaskDeleted";
    public const string TaskMoved = "TaskMoved";
}
