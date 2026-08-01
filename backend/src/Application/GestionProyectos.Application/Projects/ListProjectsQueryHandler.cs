using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Common.Models;
using GestionProyectos.Domain.Common;
using MediatR;

namespace GestionProyectos.Application.Projects;

public class ListProjectsQueryHandler : IRequestHandler<ListProjectsQuery, Result<PagedResult<ProjectResponseDto>>>
{
    private readonly IProjectRepository _projectRepository;

    public ListProjectsQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result<PagedResult<ProjectResponseDto>>> Handle(ListProjectsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _projectRepository.ListAsync(
            request.Page, request.PageSize, request.Name, request.Status, cancellationToken);

        var dtos = items.Select(p => p.ToDto()).ToList();

        return Result<PagedResult<ProjectResponseDto>>.Success(
            new PagedResult<ProjectResponseDto>(dtos, request.Page, request.PageSize, totalCount));
    }
}
