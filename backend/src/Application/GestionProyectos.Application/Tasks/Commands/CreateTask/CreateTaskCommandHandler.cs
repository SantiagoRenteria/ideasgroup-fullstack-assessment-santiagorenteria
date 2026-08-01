using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using MediatR;

namespace GestionProyectos.Application.Tasks.Commands.CreateTask;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Result<TaskResponseDto>>
{
    public const string ColumnNotFound = "Columna no encontrada.";

    private readonly IColumnRepository _columnRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTaskCommandHandler(IColumnRepository columnRepository, ITaskRepository taskRepository, IUnitOfWork unitOfWork)
    {
        _columnRepository = columnRepository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TaskResponseDto>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var column = await _columnRepository.GetByIdAsync(request.ColumnId, cancellationToken);

        if (column is null)
            return Result<TaskResponseDto>.Failure(ColumnNotFound);

        // Alta desde el tablero (seccion 6.5): siempre se agrega al final de la columna,
        // no hay forma de elegir posicion en el propio formulario de creacion.
        var existingTasks = await _taskRepository.ListByColumnAsync(request.ColumnId, cancellationToken);
        var order = TaskOrderingHelper.GetOrderForTargetIndex(existingTasks, existingTasks.Count);

        var task = new TaskEntity(
            Guid.NewGuid(),
            request.ColumnId,
            request.Title,
            request.Description,
            request.Priority,
            request.AssigneeId,
            order,
            DateTime.UtcNow);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TaskResponseDto>.Success(task.ToDto());
    }
}
