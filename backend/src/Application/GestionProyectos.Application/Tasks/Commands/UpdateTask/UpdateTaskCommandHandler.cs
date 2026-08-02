using GestionProyectos.Application.Common.Exceptions;
using GestionProyectos.Application.Common.Interfaces;
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
    private readonly IBoardNotifier _boardNotifier;
    private readonly ILogger<UpdateTaskCommandHandler> _logger;

    public UpdateTaskCommandHandler(
        ITaskRepository taskRepository,
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork,
        IBoardNotifier boardNotifier,
        ILogger<UpdateTaskCommandHandler> logger)
    {
        _taskRepository = taskRepository;
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
        _boardNotifier = boardNotifier;
        _logger = logger;
    }

    public async Task<Result<TaskResponseDto>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.Id, cancellationToken);

        if (task is null)
        {
            _logger.LogWarning("Intento de actualizar la tarea inexistente {TaskId}", request.Id);
            return Result<TaskResponseDto>.Failure(TaskNotFound);
        }

        task.Update(request.Title, request.Description, request.Priority, request.AssigneeId);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Conflicto de concurrencia al actualizar la tarea {TaskId}: otra sesion la modifico primero", request.Id);
            return Result<TaskResponseDto>.Failure(ConcurrencyConflict);
        }

        var dto = task.ToDto();
        var column = await _columnRepository.GetByIdAsync(task.ColumnId, cancellationToken);
        if (column is not null)
            await _boardNotifier.TaskUpdatedAsync(column.ProjectId, dto, request.ConnectionId, cancellationToken);

        return Result<TaskResponseDto>.Success(dto);
    }
}
