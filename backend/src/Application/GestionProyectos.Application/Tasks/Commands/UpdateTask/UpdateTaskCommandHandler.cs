using GestionProyectos.Application.Common.Exceptions;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Common.Outbox;
using GestionProyectos.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, Result<TaskResponseDto>>
{
    public const string TaskNotFound = "Tarea no encontrada.";
    public const string ConcurrencyConflict = "La tarea fue modificada por otra sesión. Actualiza la vista e intenta de nuevo.";

    private readonly ITaskRepository _taskRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IOutboxSignal _outboxSignal;
    private readonly ILogger<UpdateTaskCommandHandler> _logger;

    public UpdateTaskCommandHandler(
        ITaskRepository taskRepository,
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork,
        IOutboxWriter outboxWriter,
        IOutboxSignal outboxSignal,
        ILogger<UpdateTaskCommandHandler> logger)
    {
        _taskRepository = taskRepository;
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
        _outboxWriter = outboxWriter;
        _outboxSignal = outboxSignal;
        _logger = logger;
    }

    public async Task<Result<TaskResponseDto>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.Id, cancellationToken);

        if (task is null)
        {
            _logger.LogWarning("Intento de actualizar la tarea inexistente {TaskId}", request.Id);
            return Result<TaskResponseDto>.Failure(TaskNotFound, ErrorType.NotFound);
        }

        task.Update(request.Title, request.Description, request.Priority, request.AssigneeId);

        var dto = task.ToDto();
        var column = await _columnRepository.GetByIdAsync(task.ColumnId, cancellationToken);
        if (column is not null)
            _outboxWriter.Enqueue(OutboxEventTypes.TaskUpdated, column.ProjectId, dto, request.ConnectionId);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Conflicto de concurrencia al actualizar la tarea {TaskId}: otra sesion la modifico primero", request.Id);
            return Result<TaskResponseDto>.Failure(ConcurrencyConflict, ErrorType.Conflict);
        }

        _outboxSignal.Signal();

        return Result<TaskResponseDto>.Success(dto);
    }
}
