using GestionProyectos.Application.Columns;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Columns.Queries.ListColumnsByProject;

public class ListColumnsByProjectQueryHandler : IRequestHandler<ListColumnsByProjectQuery, Result<IReadOnlyList<ColumnResponseDto>>>
{
    public const string ProjectNotFound = "Proyecto no encontrado.";

    private readonly IProjectRepository _projectRepository;
    private readonly IColumnRepository _columnRepository;
    private readonly ILogger<ListColumnsByProjectQueryHandler> _logger;

    public ListColumnsByProjectQueryHandler(
        IProjectRepository projectRepository,
        IColumnRepository columnRepository,
        ILogger<ListColumnsByProjectQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _columnRepository = columnRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<ColumnResponseDto>>> Handle(ListColumnsByProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
        {
            _logger.LogWarning("Intento de listar columnas del proyecto inexistente {ProjectId}", request.ProjectId);
            return Result<IReadOnlyList<ColumnResponseDto>>.Failure(ProjectNotFound, ErrorType.NotFound);
        }

        var columns = await _columnRepository.ListByProjectAsync(request.ProjectId, cancellationToken);

        return Result<IReadOnlyList<ColumnResponseDto>>.Success(
            columns.OrderBy(c => c.Order).Select(c => c.ToDto()).ToList());
    }
}
