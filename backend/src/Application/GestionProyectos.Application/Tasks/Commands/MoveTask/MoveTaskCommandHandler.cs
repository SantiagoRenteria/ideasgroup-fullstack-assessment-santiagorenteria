using GestionProyectos.Application.Common.Exceptions;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Common.Outbox;
using GestionProyectos.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Tasks.Commands.MoveTask;

public class MoveTaskCommandHandler : IRequestHandler<MoveTaskCommand, Result<TaskResponseDto>>
{
    public const string TaskNotFound = "Tarea no encontrada.";
    public const string TargetColumnNotFound = "Columna destino no encontrada.";
    public const string TargetIndexOutOfRange = "La posicion destino esta fuera del rango de la columna.";
    public const string ConcurrencyConflict = "La tarea fue modificada por otra sesión. Actualiza la vista e intenta de nuevo.";

    private readonly ITaskRepository _taskRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IOutboxSignal _outboxSignal;
    private readonly ILogger<MoveTaskCommandHandler> _logger;

    public MoveTaskCommandHandler(
        ITaskRepository taskRepository,
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork,
        IOutboxWriter outboxWriter,
        IOutboxSignal outboxSignal,
        ILogger<MoveTaskCommandHandler> logger)
    {
        _taskRepository = taskRepository;
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
        _outboxWriter = outboxWriter;
        _outboxSignal = outboxSignal;
        _logger = logger;
    }

    public async Task<Result<TaskResponseDto>> Handle(MoveTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.Id, cancellationToken);

        if (task is null)
        {
            _logger.LogWarning("Intento de mover la tarea inexistente {TaskId}", request.Id);
            return Result<TaskResponseDto>.Failure(TaskNotFound, ErrorType.NotFound);
        }

        var targetColumn = await _columnRepository.GetByIdAsync(request.TargetColumnId, cancellationToken);

        if (targetColumn is null)
        {
            _logger.LogWarning("Intento de mover la tarea {TaskId} a la columna inexistente {TargetColumnId}", request.Id, request.TargetColumnId);
            return Result<TaskResponseDto>.Failure(TargetColumnNotFound, ErrorType.NotFound);
        }

        var targetColumnTasks = (await _taskRepository.ListByColumnAsync(request.TargetColumnId, cancellationToken))
            .Where(t => t.Id != task.Id)
            .ToList();

        if (request.TargetIndex > targetColumnTasks.Count)
        {
            _logger.LogWarning(
                "Intento de mover la tarea {TaskId} al indice {TargetIndex}, fuera de rango para la columna {TargetColumnId} ({Count} tareas)",
                request.Id, request.TargetIndex, request.TargetColumnId, targetColumnTasks.Count);
            return Result<TaskResponseDto>.Failure(TargetIndexOutOfRange, ErrorType.Validation);
        }

        var order = TaskOrderingHelper.GetOrderForTargetIndex(targetColumnTasks, request.TargetIndex);

        task.Move(request.TargetColumnId, order);

        var dto = task.ToDto();
        var payload = new TaskMovedOutboxPayload(dto, request.TargetIndex);
        _outboxWriter.Enqueue(OutboxEventTypes.TaskMoved, targetColumn.ProjectId, payload, request.ConnectionId);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Conflicto de concurrencia al mover la tarea {TaskId}: otra sesion la modifico primero", request.Id);
            return Result<TaskResponseDto>.Failure(ConcurrencyConflict, ErrorType.Conflict);
        }

        _outboxSignal.Signal();

        return Result<TaskResponseDto>.Success(dto);
    }
}
