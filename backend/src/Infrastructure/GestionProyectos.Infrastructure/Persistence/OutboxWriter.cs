using System.Text.Json;
using GestionProyectos.Application.Common.Outbox;
using GestionProyectos.Infrastructure.Persistence.Entities;

namespace GestionProyectos.Infrastructure.Persistence;

public class OutboxWriter : IOutboxWriter
{
    // Serializacion interna del outbox, nunca expuesta directamente a un cliente SignalR
    // (eso lo hace IBoardNotifier con su propio RealtimeJsonOptions) -- solo necesita ser
    // legible para depuracion y deserializable de vuelta a los mismos tipos.
    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly AppDbContext _dbContext;

    public OutboxWriter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // No llama a SaveChanges: el Handler que invoca esto ya va a guardar el cambio de
    // negocio en el mismo SaveChangesAsync -- ver arquitectura-decisiones.md §24.
    public void Enqueue(string eventType, Guid projectId, object payload, string? excludeConnectionId)
    {
        var message = new OutboxMessage(
            Guid.NewGuid(),
            eventType,
            projectId,
            JsonSerializer.Serialize(payload, SerializerOptions),
            excludeConnectionId,
            DateTime.UtcNow);

        _dbContext.OutboxMessages.Add(message);
    }
}
