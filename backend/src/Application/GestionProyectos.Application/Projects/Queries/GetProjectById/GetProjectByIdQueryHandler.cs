using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Projects;
using GestionProyectos.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Projects.Queries.GetProjectById;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, Result<ProjectResponseDto>>
{
    private const string ProjectNotFound = "Proyecto no encontrado.";

    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<GetProjectByIdQueryHandler> _logger;

    public GetProjectByIdQueryHandler(IProjectRepository projectRepository, ILogger<GetProjectByIdQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<Result<ProjectResponseDto>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);

        if (project is null)
        {
            _logger.LogWarning("Intento de consultar el proyecto inexistente {ProjectId}", request.Id);
            return Result<ProjectResponseDto>.Failure(ProjectNotFound);
        }

        return Result<ProjectResponseDto>.Success(project.ToDto());
    }
}
