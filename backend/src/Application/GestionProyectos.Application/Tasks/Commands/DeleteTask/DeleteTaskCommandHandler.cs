using GestionProyectos.Application.Common.Exceptions;
using GestionProyectos.Application.Common.Interfaces;
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
    private readonly IBoardNotifier _boardNotifier;
    private readonly ILogger<DeleteTaskCommandHandler> _logger;

    public DeleteTaskCommandHandler(
        ITaskRepository taskRepository,
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork,
        IBoardNotifier boardNotifier,
        ILogger<DeleteTaskCommandHandler> logger)
    {
        _taskRepository = taskRepository;
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
        _boardNotifier = boardNotifier;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.Id, cancellationToken);

        if (task is null)
        {
            _logger.LogWarning("Intento de eliminar la tarea inexistente {TaskId}", request.Id);
            return Result.Failure(TaskNotFound);
        }

        var columnId = task.ColumnId;
        task.Delete();

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Conflicto de concurrencia al eliminar la tarea {TaskId}: otra sesion la modifico primero", request.Id);
            return Result.Failure(ConcurrencyConflict);
        }

        var column = await _columnRepository.GetByIdAsync(columnId, cancellationToken);
        if (column is not null)
            await _boardNotifier.TaskDeletedAsync(column.ProjectId, task.Id, columnId, request.ConnectionId, cancellationToken);

        return Result.Success();
    }
}
