namespace GestionProyectos.Application.Common.Outbox;

// Despierta al dispatcher del outbox inmediatamente en vez de esperar el proximo ciclo de
// polling (ADR §24: polling + senal in-process, sin bus de mensajeria).
public interface IOutboxSignal
{
    void Signal();
}
