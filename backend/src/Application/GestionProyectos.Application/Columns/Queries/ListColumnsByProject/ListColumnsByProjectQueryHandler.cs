using GestionProyectos.Application.Columns;
using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Columns.Queries.ListColumnsByProject;

public class ListColumnsByProjectQueryHandler : IRequestHandler<ListColumnsByProjectQuery, Result<IReadOnlyList<ColumnResponseDto>>>
{
    public const string ProjectNotFound = "Proyecto no encontrado.";

    private readonly IProjectRepository _projectRepository;
    private readonly IColumnRepository _columnRepository;

    public ListColumnsByProjectQueryHandler(IProjectRepository projectRepository, IColumnRepository columnRepository)
    {
        _projectRepository = projectRepository;
        _columnRepository = columnRepository;
    }

    public async Task<Result<IReadOnlyList<ColumnResponseDto>>> Handle(ListColumnsByProjectQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);

        if (project is null)
            return Result<IReadOnlyList<ColumnResponseDto>>.Failure(ProjectNotFound);

        var columns = await _columnRepository.ListByProjectAsync(request.ProjectId, cancellationToken);

        return Result<IReadOnlyList<ColumnResponseDto>>.Success(
            columns.OrderBy(c => c.Order).Select(c => c.ToDto()).ToList());
    }
}
