using GestionProyectos.Application.Columns;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Columns.Commands.CreateColumn;

public class CreateColumnCommandHandler : IRequestHandler<CreateColumnCommand, Result<ColumnResponseDto>>
{
    public const string ProjectNotFound = "Proyecto no encontrado.";

    private readonly IProjectRepository _projectRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateColumnCommandHandler> _logger;

    public CreateColumnCommandHandler(
        IProjectRepository projectRepository,
        IColumnRepository columnRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateColumnCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _columnRepository = columnRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ColumnResponseDto>> Handle(CreateColumnCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            _logger.LogWarning("Intento de crear una columna en el proyecto inexistente {ProjectId}", request.ProjectId);
            return Result<ColumnResponseDto>.Failure(ProjectNotFound);
        }

        var column = new Column(Guid.NewGuid(), request.ProjectId, request.Name, request.Order);

        await _columnRepository.AddAsync(column, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ColumnResponseDto>.Success(column.ToDto());
    }
}
