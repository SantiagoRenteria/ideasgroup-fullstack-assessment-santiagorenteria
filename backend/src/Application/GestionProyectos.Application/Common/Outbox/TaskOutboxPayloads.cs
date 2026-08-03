using GestionProyectos.Application.Tasks;

namespace GestionProyectos.Application.Common.Outbox;

// Payloads propios del outbox para los eventos que IBoardNotifier no modela con un DTO
// existente (TaskCreated/TaskUpdated ya reutilizan TaskResponseDto directamente).
public record TaskDeletedOutboxPayload(Guid TaskId, Guid ColumnId);

public record TaskMovedOutboxPayload(TaskResponseDto Task, int TargetIndex);
