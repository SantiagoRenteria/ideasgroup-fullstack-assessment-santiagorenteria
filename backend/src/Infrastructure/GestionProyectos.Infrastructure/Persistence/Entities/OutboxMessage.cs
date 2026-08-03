namespace GestionProyectos.Infrastructure.Persistence.Entities;

// Registro tecnico del Outbox Pattern (ADR §24), no una entidad de Domain: no tiene
// invariantes de negocio propias, es un registro de "hay que notificar esto" -- mismo
// criterio que RevokedToken (§16).
public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;
    public Guid ProjectId { get; private set; }
    public string Payload { get; private set; } = null!;
    public string? ExcludeConnectionId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(Guid id, string type, Guid projectId, string payload, string? excludeConnectionId, DateTime createdAtUtc)
    {
        Id = id;
        Type = type;
        ProjectId = projectId;
        Payload = payload;
        ExcludeConnectionId = excludeConnectionId;
        CreatedAtUtc = createdAtUtc;
    }

    public void MarkProcessed(DateTime processedAtUtc) => ProcessedAtUtc = processedAtUtc;
}
