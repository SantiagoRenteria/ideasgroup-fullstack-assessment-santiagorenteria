using GestionProyectos.Application.Common.Exceptions;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Common.Outbox;
using GestionProyectos.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Tasks.Commands.DeleteTask;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Result>
{
    public const string TaskNotFound = "Tarea no encontrada.";
    public const string ConcurrencyConflict = "La tarea fue modificada por otra sesión. Actualiza la vista e intenta de nuevo.";

    private readonly ITaskRepository _taskRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IOutboxSignal _outboxSignal;
    private readonly ILogger<DeleteTaskCommandHandler> _logger;

    public DeleteTaskCommandHandler(
        ITaskRepository taskRepository,
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork,
        IOutboxWriter outboxWriter,
        IOutboxSignal outboxSignal,
        ILogger<DeleteTaskCommandHandler> logger)
    {
        _taskRepository = taskRepository;
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
        _outboxWriter = outboxWriter;
        _outboxSignal = outboxSignal;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.Id, cancellationToken);

        if (task is null)
        {
            _logger.LogWarning("Intento de eliminar la tarea inexistente {TaskId}", request.Id);
            return Result.Failure(TaskNotFound, ErrorType.NotFound);
        }

        var columnId = task.ColumnId;
        task.Delete();

        var column = await _columnRepository.GetByIdAsync(columnId, cancellationToken);
        if (column is not null)
        {
            var payload = new TaskDeletedOutboxPayload(task.Id, columnId);
            _outboxWriter.Enqueue(OutboxEventTypes.TaskDeleted, column.ProjectId, payload, request.ConnectionId);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Conflicto de concurrencia al eliminar la tarea {TaskId}: otra sesion la modifico primero", request.Id);
            return Result.Failure(ConcurrencyConflict, ErrorType.Conflict);
        }

        _outboxSignal.Signal();

        return Result.Success();
    }
}
