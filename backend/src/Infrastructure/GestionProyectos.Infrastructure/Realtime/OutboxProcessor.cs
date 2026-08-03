using System.Text.Json;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Common.Outbox;
using GestionProyectos.Application.Tasks;
using GestionProyectos.Infrastructure.Persistence;
using GestionProyectos.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Infrastructure.Realtime;

// El "que hace un ciclo" del Outbox Pattern (ADR §24), separado de OutboxDispatcher (el
// "cuando corre un ciclo" -- BackgroundService + polling) para que esto sea testeable
// directamente contra Postgres real sin levantar todo el hosted service.
public class OutboxProcessor
{
    private const int BatchSize = 20;
    private static readonly JsonSerializerOptions SerializerOptions = new();

    private readonly AppDbContext _dbContext;
    private readonly IBoardNotifier _boardNotifier;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(AppDbContext dbContext, IBoardNotifier boardNotifier, ILogger<OutboxProcessor> logger)
    {
        _dbContext = dbContext;
        _boardNotifier = boardNotifier;
        _logger = logger;
    }

    // Reclama un lote de mensajes pendientes y los despacha. Devuelve cuantos proceso,
    // para que un test de integracion pueda afirmar sobre el resultado sin depender de
    // detalles internos.
    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var claimed = await ClaimBatchAsync(cancellationToken);

        foreach (var message in claimed)
        {
            try
            {
                await DispatchAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                // El mensaje ya quedo marcado procesado al reclamarlo (ver ClaimBatchAsync):
                // un fallo aqui no lo reintenta, se loguea para diagnostico. Trade-off
                // aceptado y documentado en el ADR §24 -- ventana mucho mas angosta que el
                // bug original (afectaba practicamente cualquier request).
                _logger.LogError(ex, "Fallo notificando el evento de outbox {OutboxMessageId} ({Type})", message.Id, message.Type);
            }
        }

        return claimed.Count;
    }

    // FOR UPDATE SKIP LOCKED: insurance barata para si algun dia se escala horizontalmente
    // -- sin esto, dos instancias de la API podrian reclamar y notificar el mismo evento
    // duplicado. Se marca procesado dentro de la misma transaccion corta que el claim, no
    // despues del dispatch, para no mantener el lock de fila abierto durante la llamada de
    // red a SignalR.
    private async Task<List<OutboxMessage>> ClaimBatchAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var claimed = await _dbContext.OutboxMessages
            .FromSqlInterpolated($@"
                SELECT * FROM outbox_messages
                WHERE processed_at_utc IS NULL
                ORDER BY created_at_utc
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED")
            .ToListAsync(cancellationToken);

        var processedAt = DateTime.UtcNow;
        foreach (var message in claimed)
            message.MarkProcessed(processedAt);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return claimed;
    }

    private Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken) =>
        message.Type switch
        {
            OutboxEventTypes.TaskCreated => _boardNotifier.TaskCreatedAsync(
                message.ProjectId,
                Deserialize<TaskResponseDto>(message),
                message.ExcludeConnectionId,
                cancellationToken),

            OutboxEventTypes.TaskUpdated => _boardNotifier.TaskUpdatedAsync(
                message.ProjectId,
                Deserialize<TaskResponseDto>(message),
                message.ExcludeConnectionId,
                cancellationToken),

            OutboxEventTypes.TaskDeleted => DispatchTaskDeletedAsync(message, cancellationToken),

            OutboxEventTypes.TaskMoved => DispatchTaskMovedAsync(message, cancellationToken),

            _ => throw new InvalidOperationException($"Tipo de evento de outbox no reconocido: {message.Type}")
        };

    private Task DispatchTaskDeletedAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = Deserialize<TaskDeletedOutboxPayload>(message);
        return _boardNotifier.TaskDeletedAsync(message.ProjectId, payload.TaskId, payload.ColumnId, message.ExcludeConnectionId, cancellationToken);
    }

    private Task DispatchTaskMovedAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = Deserialize<TaskMovedOutboxPayload>(message);
        return _boardNotifier.TaskMovedAsync(message.ProjectId, payload.Task, payload.TargetIndex, message.ExcludeConnectionId, cancellationToken);
    }

    private static T Deserialize<T>(OutboxMessage message) =>
        JsonSerializer.Deserialize<T>(message.Payload, SerializerOptions)
        ?? throw new InvalidOperationException($"Payload de outbox nulo tras deserializar ({message.Type}, {message.Id}).");
}
