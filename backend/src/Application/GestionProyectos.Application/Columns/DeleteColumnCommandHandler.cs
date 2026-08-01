using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Columns;

public class DeleteColumnCommandHandler : IRequestHandler<DeleteColumnCommand, Result>
{
    public const string ColumnNotFound = "Columna no encontrada.";

    // Regla de negocio obligatoria (enunciado seccion 6.4): no se permite eliminar una
    // columna que contenga tareas. El endpoint distingue este mensaje del de "no encontrada"
    // para mapear 409 vs 404 -- ver docs/decisions/arquitectura-decisiones.md §3 (Result Pattern).
    public const string ColumnHasTasks = "No se puede eliminar una columna que contiene tareas.";

    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteColumnCommandHandler(IColumnRepository columnRepository, IUnitOfWork unitOfWork)
    {
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteColumnCommand request, CancellationToken cancellationToken)
    {
        var column = await _columnRepository.GetByIdAsync(request.Id, cancellationToken);

        if (column is null)
            return Result.Failure(ColumnNotFound);

        if (await _columnRepository.HasTasksAsync(column.Id, cancellationToken))
            return Result.Failure(ColumnHasTasks);

        _columnRepository.Remove(column);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
