using GestionProyectos.Application.Common.Exceptions;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Tasks.Commands.DeleteTask;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Result>
{
    public const string TaskNotFound = "Tarea no encontrada.";
    public const string ConcurrencyConflict = "La tarea fue modificada por otra sesión. Actualiza la vista e intenta de nuevo.";

    private readonly ITaskRepository _taskRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBoardNotifier _boardNotifier;

    public DeleteTaskCommandHandler(
        ITaskRepository taskRepository,
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork,
        IBoardNotifier boardNotifier)
    {
        _taskRepository = taskRepository;
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
        _boardNotifier = boardNotifier;
    }

    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.Id, cancellationToken);

        if (task is null)
            return Result.Failure(TaskNotFound);

        var columnId = task.ColumnId;
        task.Delete();

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result.Failure(ConcurrencyConflict);
        }

        var column = await _columnRepository.GetByIdAsync(columnId, cancellationToken);
        if (column is not null)
            await _boardNotifier.TaskDeletedAsync(column.ProjectId, task.Id, columnId, request.ConnectionId, cancellationToken);

        return Result.Success();
    }
}
