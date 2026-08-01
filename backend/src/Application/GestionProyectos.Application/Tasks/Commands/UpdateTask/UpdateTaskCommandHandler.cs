using GestionProyectos.Application.Common.Exceptions;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, Result<TaskResponseDto>>
{
    public const string TaskNotFound = "Tarea no encontrada.";
    public const string ConcurrencyConflict = "La tarea fue modificada por otra sesión. Actualiza la vista e intenta de nuevo.";

    private readonly ITaskRepository _taskRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBoardNotifier _boardNotifier;

    public UpdateTaskCommandHandler(
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

    public async Task<Result<TaskResponseDto>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.Id, cancellationToken);

        if (task is null)
            return Result<TaskResponseDto>.Failure(TaskNotFound);

        task.Update(request.Title, request.Description, request.Priority, request.AssigneeId);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result<TaskResponseDto>.Failure(ConcurrencyConflict);
        }

        var dto = task.ToDto();
        var column = await _columnRepository.GetByIdAsync(task.ColumnId, cancellationToken);
        if (column is not null)
            await _boardNotifier.TaskUpdatedAsync(column.ProjectId, dto, request.ConnectionId, cancellationToken);

        return Result<TaskResponseDto>.Success(dto);
    }
}
