using GestionProyectos.Application.Columns;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Columns.Commands.UpdateColumn;

public class UpdateColumnCommandHandler : IRequestHandler<UpdateColumnCommand, Result<ColumnResponseDto>>
{
    public const string ColumnNotFound = "Columna no encontrada.";

    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateColumnCommandHandler(IColumnRepository columnRepository, IUnitOfWork unitOfWork)
    {
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ColumnResponseDto>> Handle(UpdateColumnCommand request, CancellationToken cancellationToken)
    {
        var column = await _columnRepository.GetByIdAsync(request.Id, cancellationToken);

        if (column is null)
            return Result<ColumnResponseDto>.Failure(ColumnNotFound);

        column.Rename(request.Name);
        column.MoveTo(request.Order);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ColumnResponseDto>.Success(column.ToDto());
    }
}
