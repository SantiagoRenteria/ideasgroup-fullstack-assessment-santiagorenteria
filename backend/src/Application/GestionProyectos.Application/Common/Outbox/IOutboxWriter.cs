namespace GestionProyectos.Application.Common.Outbox;

// Puerto del Outbox Pattern (ADR §24): encola el evento en la MISMA transaccion que la
// mutacion de negocio -- no llama a SaveChanges por si mismo, el Handler ya lo hace justo
// despues. Sin esto, un crash entre el commit y la notificacion por SignalR pierde el
// evento en silencio (el gap de consistencia que motivo este fix).
public interface IOutboxWriter
{
    void Enqueue(string eventType, Guid projectId, object payload, string? excludeConnectionId);
}
