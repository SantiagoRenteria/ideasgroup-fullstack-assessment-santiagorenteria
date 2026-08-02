using GestionProyectos.Application.Columns;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Columns.Commands.UpdateColumn;

public class UpdateColumnCommandHandler : IRequestHandler<UpdateColumnCommand, Result<ColumnResponseDto>>
{
    public const string ColumnNotFound = "Columna no encontrada.";

    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateColumnCommandHandler> _logger;

    public UpdateColumnCommandHandler(
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateColumnCommandHandler> logger)
    {
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ColumnResponseDto>> Handle(UpdateColumnCommand request, CancellationToken cancellationToken)
    {
        var column = await _columnRepository.GetByIdAsync(request.Id, cancellationToken);

        if (column is null)
        {
            _logger.LogWarning("Intento de actualizar la columna inexistente {ColumnId}", request.Id);
            return Result<ColumnResponseDto>.Failure(ColumnNotFound, ErrorType.NotFound);
        }

        column.Rename(request.Name);
        column.MoveTo(request.Order);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ColumnResponseDto>.Success(column.ToDto());
    }
}
