using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Tasks.Commands.MoveTask;

public class MoveTaskCommandHandler : IRequestHandler<MoveTaskCommand, Result<TaskResponseDto>>
{
    public const string TaskNotFound = "Tarea no encontrada.";
    public const string TargetColumnNotFound = "Columna destino no encontrada.";
    public const string TargetIndexOutOfRange = "La posicion destino esta fuera del rango de la columna.";

    private readonly ITaskRepository _taskRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MoveTaskCommandHandler(ITaskRepository taskRepository, IColumnRepository columnRepository, IUnitOfWork unitOfWork)
    {
        _taskRepository = taskRepository;
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TaskResponseDto>> Handle(MoveTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetByIdAsync(request.Id, cancellationToken);

        if (task is null)
            return Result<TaskResponseDto>.Failure(TaskNotFound);

        var targetColumn = await _columnRepository.GetByIdAsync(request.TargetColumnId, cancellationToken);

        if (targetColumn is null)
            return Result<TaskResponseDto>.Failure(TargetColumnNotFound);

        var targetColumnTasks = (await _taskRepository.ListByColumnAsync(request.TargetColumnId, cancellationToken))
            .Where(t => t.Id != task.Id)
            .ToList();

        if (request.TargetIndex > targetColumnTasks.Count)
            return Result<TaskResponseDto>.Failure(TargetIndexOutOfRange);

        var order = TaskOrderingHelper.GetOrderForTargetIndex(targetColumnTasks, request.TargetIndex);

        task.Move(request.TargetColumnId, order);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TaskResponseDto>.Success(task.ToDto());
    }
}
