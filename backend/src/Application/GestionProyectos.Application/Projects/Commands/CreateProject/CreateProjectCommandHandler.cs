using GestionProyectos.Application.Common.Interfaces;
using GestionProyectos.Application.Projects;
using GestionProyectos.Domain.Common;
using GestionProyectos.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionProyectos.Application.Projects.Commands.CreateProject;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Result<ProjectResponseDto>>
{
    public const string DuplicateName = "Ya existe un proyecto con este nombre.";

    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateProjectCommandHandler> _logger;

    public CreateProjectCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ProjectResponseDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (await _projectRepository.ExistsByNameAsync(request.Name, excludeProjectId: null, cancellationToken))
        {
            _logger.LogWarning("Intento de crear un proyecto con nombre duplicado {Name}", request.Name);
            return Result<ProjectResponseDto>.Failure(DuplicateName);
        }

        var project = new Project(
            Guid.NewGuid(),
            request.Name,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.Status);

        await _projectRepository.AddAsync(project, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProjectResponseDto>.Success(project.ToDto());
    }
}
