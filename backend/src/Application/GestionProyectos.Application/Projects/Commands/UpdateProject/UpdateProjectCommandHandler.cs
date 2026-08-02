using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Projects;
using GestionProyectos.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Projects.Commands.UpdateProject;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, Result<ProjectResponseDto>>
{
    public const string ProjectNotFound = "Proyecto no encontrado.";
    public const string DuplicateName = "Ya existe un proyecto con este nombre.";

    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProjectCommandHandler> _logger;

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProjectResponseDto>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(request.Id, cancellationToken);

        if (project is null)
        {
            _logger.LogWarning("Intento de actualizar el proyecto inexistente {ProjectId}", request.Id);
            return Result<ProjectResponseDto>.Failure(ProjectNotFound);
        }

        if (await _projectRepository.ExistsByNameAsync(request.Name, excludeProjectId: project.Id, cancellationToken))
        {
            _logger.LogWarning("Intento de renombrar el proyecto {ProjectId} a un nombre duplicado {Name}", project.Id, request.Name);
            return Result<ProjectResponseDto>.Failure(DuplicateName);
        }

        project.Update(request.Name, request.Description, request.StartDate, request.EndDate, request.Status);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProjectResponseDto>.Success(project.ToDto());
    }
}
