using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Columns.Commands.DeleteColumn;

public class DeleteColumnCommandHandler : IRequestHandler<DeleteColumnCommand, Result>
{
    public const string ColumnNotFound = "Columna no encontrada.";

    // Regla de negocio obligatoria (sección 6.4, ADR §3 Result Pattern): distingue este
    // mensaje del de "no encontrada" para mapear 409 vs 404.
    public const string ColumnHasTasks = "No se puede eliminar una columna que contiene tareas.";

    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteColumnCommandHandler> _logger;

    public DeleteColumnCommandHandler(
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteColumnCommandHandler> logger)
    {
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteColumnCommand request, CancellationToken cancellationToken)
    {
        var column = await _columnRepository.GetByIdAsync(request.Id, cancellationToken);

        if (column is null)
        {
            _logger.LogWarning("Intento de eliminar la columna inexistente {ColumnId}", request.Id);
            return Result.Failure(ColumnNotFound);
        }

        if (await _columnRepository.HasTasksAsync(column.Id, cancellationToken))
        {
            _logger.LogWarning("Intento de eliminar la columna {ColumnId} que contiene tareas", column.Id);
            return Result.Failure(ColumnHasTasks);
        }

        column.Delete();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
